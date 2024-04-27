#include "frtc_content_capturer.h"
#include "log.h"

#include "win_util.h"
#include "json/json.h"

#include <string>


#pragma region gdi_desktop_capturer implement

gdi_desktop_capturer::gdi_desktop_capturer() :
	_auto_hdc(NULL),
	_auto_desktop_dc(NULL),
	_auto_bitmap_cache(NULL),
	_monitor_index(-1),
	_monitor_id(L""),
	_capture_buffer(NULL),
	_capture_buffer_len(0),
	_capture_monitor_w(0),
	_capture_monitor_h(0),
	_capture_callback(NULL)
{
	if ((HDC)_auto_hdc == NULL)
	{
		_auto_hdc.Attach(CreateCompatibleDC(NULL));
	}
}

gdi_desktop_capturer::~gdi_desktop_capturer()
{
	if (_capture_buffer)
	{
		DebugLog("gdi_desktop_capturer free _capture_buffer");
		free(_capture_buffer);
		_capture_buffer = NULL;
		_capture_buffer_len = 0;
	}
}

void gdi_desktop_capturer::init(int monitor_index, LPCWSTR monitor_id)
{
	_monitor_index = monitor_index;
	_monitor_id = monitor_id;
}

void gdi_desktop_capturer::set_capture_callback(interface_frtc_capture_content_callback* callback)
{
	_capture_callback = callback;
}


HRESULT gdi_desktop_capturer::capture_to_buf()
{
	HBITMAP hbitmap;
	if (S_OK != capture_to_bitmap(hbitmap))
	{
		ErrorLog("capture_to_bitmap failed, can't capture bitmap, Maybe covered");
		if (hbitmap)
		{
			_auto_bitmap_cache.Attach(hbitmap);
		}
		return E_FAIL;
	}

	SIZE bitmapSize;
	if (!GetBitmapDimensionEx(hbitmap, &bitmapSize))
	{
		ErrorLog("GetBitmapDimensionEx failed, error=0x%x", GetLastError());
		_auto_bitmap_cache.Attach(hbitmap);
		return E_FAIL;
	}

	LONG len = bitmapSize.cx * bitmapSize.cy * 4;
	if (_capture_buffer_len < len)
	{
		if (_capture_buffer) {
			free(_capture_buffer);
		}
		_capture_buffer = malloc(len);
		InfoLog("gdi_desktop_capturer::capture_to_buf: size =%dx%d", bitmapSize.cx, bitmapSize.cy);
		InfoLog("gdi_desktop_capturer::capture_to_buf: re-alloc buf from %d to %d, buf[0x%x]", _capture_buffer_len, len, _capture_buffer);
		if (!_capture_buffer)
		{
			ErrorLog("gdi_desktop_capturer::capture_to_buf: re-alloc buf failed");
			_auto_bitmap_cache.Attach(hbitmap);
			return E_FAIL;
		}
		_capture_buffer_len = len;
	}

	BITMAPINFO bmiCapture = { 0 };
	bmiCapture.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	bmiCapture.bmiHeader.biWidth = bitmapSize.cx;
	bmiCapture.bmiHeader.biHeight = -bitmapSize.cy;
	bmiCapture.bmiHeader.biPlanes = 1;
	bmiCapture.bmiHeader.biBitCount = 32;
	bmiCapture.bmiHeader.biCompression = BI_RGB;

	GetDIBits(_auto_hdc, hbitmap, 0, bitmapSize.cy, _capture_buffer, &bmiCapture, DIB_RGB_COLORS);

	_capture_monitor_w = bitmapSize.cx;
	_capture_monitor_h = bitmapSize.cy;

	_capture_callback->on_capture(_capture_buffer, _capture_monitor_w, _capture_monitor_h);

	_auto_bitmap_cache.Attach(hbitmap);

	return S_OK;
}

HRESULT gdi_desktop_capturer::capture_to_bitmap(HBITMAP& bitmap)
{
	HRESULT hr = S_FALSE;
	DEVMODE dev;
	dev.dmSize = sizeof(DEVMODE);

	BOOL bFound = false;

	if (_monitor_index == -1)
	{
		bFound = EnumDisplaySettings(NULL, ENUM_CURRENT_SETTINGS, &dev);
	}
	else
	{
		bFound = EnumDisplaySettings(_monitor_id.c_str(), ENUM_CURRENT_SETTINGS, &dev);
	}

	static bool enumFail = false;
	if (bFound)
	{
		enumFail = false;
	}
	else
	{
		ErrorLog("capture_to_bitmap: EnumDisplaySettings failed, monitorId %S", _monitor_id);
		if (!enumFail)
		{
			enumFail = true;
			for (int i = 0; ; i++)
			{
				if (!EnumDisplaySettings(_monitor_id.c_str(), i, &dev)) break;
				DebugLog("mode num %d: position %dx%d pels %dx%d", i, dev.dmPosition.x, dev.dmPosition.y, dev.dmPelsWidth, dev.dmPelsHeight);
			}
		}
		return E_FAIL;
	}

	const int capture_frame_w = 4;
	RECT rect = { dev.dmPosition.x + capture_frame_w, dev.dmPosition.y + capture_frame_w,
				 dev.dmPosition.x + dev.dmPelsWidth - (capture_frame_w * 2), dev.dmPosition.y + dev.dmPelsHeight - (capture_frame_w * 2) };

	hr = this->capture_rect_to_bitmap(bitmap, rect);
	if (FAILED(hr))
	{
		return hr;
	}

	return S_OK;
}

HRESULT gdi_desktop_capturer::capture_rect_to_bitmap(HBITMAP& bitmap, RECT rect)
{
	LONG CanWidth = rect.right - rect.left;
	LONG CanHeight = rect.bottom - rect.top;
	ADJUST_RESOLUTION_16x9(CanWidth, CanHeight);

	SIZE bitmapSize = { CanWidth, CanHeight };

	HBITMAP tempTargetBitmap = NULL;
	HRESULT res = this->allocate_bitmap(bitmapSize, _auto_hdc, tempTargetBitmap);
	AutoReleaseHGDI targetBitmap(tempTargetBitmap);

	AutoReleaseSelector selector((HDC)_auto_hdc, targetBitmap);


	AutoReleaseHGDI backgroundBrush(GetStockObject(BLACK_BRUSH));
	RECT fillrect = { 0,0,bitmapSize.cx,bitmapSize.cy };
	res = FillRect(_auto_hdc, &fillrect, (HBRUSH)(HGDIOBJ)backgroundBrush);

	AutoReleaseWinDC winDC(GetWindowDC(NULL), NULL);

	res = do_bit_blt(_auto_hdc, 0, 0, rect.right - rect.left, rect.bottom - rect.top, winDC, rect.left, rect.top);

	HRESULT hr = capture_cursor(_auto_hdc, rect);

	bitmap = (HBITMAP)targetBitmap.Detach();
	SetBitmapDimensionEx(bitmap, CanWidth, CanHeight, &bitmapSize);
	return S_OK;
}

HRESULT gdi_desktop_capturer::capture_cursor(HDC hDC, const RECT& rect)
{
	CURSORINFO ci;
	ci.cbSize = sizeof(CURSORINFO);
	ci.flags = 0;
	ci.hCursor = NULL;
	ci.ptScreenPos.x = 0;
	ci.ptScreenPos.y = 0;

	BOOL res = GetCursorInfo(&ci);

	RECT curRect{ ci.ptScreenPos.x, ci.ptScreenPos.y, ci.ptScreenPos.x + 10, ci.ptScreenPos.y + 10 };
	if (cursor_point_adapter(curRect))
	{
		ci.ptScreenPos.x = curRect.left;
		ci.ptScreenPos.y = curRect.top;
	}

	if (ci.flags != CURSOR_SHOWING)
	{
		return S_OK;
	}

	if ((ci.ptScreenPos.x + GetSystemMetrics(SM_CXCURSOR) / 2 < rect.left) || (ci.ptScreenPos.x - GetSystemMetrics(SM_CXCURSOR) > rect.right) ||
		(ci.ptScreenPos.y + GetSystemMetrics(SM_CYCURSOR) / 2 < rect.top) || (ci.ptScreenPos.y - GetSystemMetrics(SM_CYCURSOR) > rect.bottom))
	{
		return S_OK;
	}

	AutoReleaseHICON copiedIcon(CopyIcon((HICON)ci.hCursor));

	ICONINFO iconInfo;
	iconInfo.hbmColor = NULL;
	iconInfo.hbmMask = NULL;

	HRESULT hr = GetIconInfo((HICON)copiedIcon, &iconInfo);
	AutoReleaseHGDI maskBitmap(iconInfo.hbmMask);
	AutoReleaseHGDI colorBitmap(iconInfo.hbmColor);

	AutoReleaseHCURSOR autoDeleteCur((HCURSOR)CopyImage(ci.hCursor, IMAGE_CURSOR, 0, 0, LR_CREATEDIBSECTION));

	LONG x_align = 0;
	LONG y_align = 0;
	LONG recWidth = rect.right - rect.left;
	LONG recHeight = rect.bottom - rect.top;
	if (recHeight * 16 > recWidth * 9)
		x_align = (recHeight * 16 / 9 - recWidth) / 2;
	else
		y_align = (recWidth * 9 / 16 - recHeight) / 2;

	hr = DrawIconEx(hDC,
		ci.ptScreenPos.x - rect.left - iconInfo.xHotspot + x_align,
		ci.ptScreenPos.y - rect.top - iconInfo.yHotspot + y_align,
		(HICON)(HCURSOR)autoDeleteCur, 0, 0, 0, NULL, DI_NORMAL);
	return S_OK;
}

HRESULT gdi_desktop_capturer::allocate_bitmap(SIZE rectSize, HDC compatiableHDC, HBITMAP& hBitmap)
{
	if ((HGDIOBJ)_auto_bitmap_cache != NULL)
	{
		BITMAP bm;
		int res = GetObject((HBITMAP)(HGDIOBJ)_auto_bitmap_cache, sizeof(BITMAP), &bm);
		if (res != 0 &&
			bm.bmWidth == rectSize.cx && bm.bmHeight == rectSize.cy)
		{
			hBitmap = (HBITMAP)_auto_bitmap_cache.Detach();
			return S_OK;
		}

		_auto_bitmap_cache.Attach(NULL);
	}

	BITMAPINFO* pbmi = (BITMAPINFO*)new char[sizeof(BITMAPINFOHEADER) + sizeof(RGBQUAD) * 256];
	if (pbmi == NULL)
		return E_OUTOFMEMORY;

	memset(&pbmi->bmiHeader, 0, sizeof(pbmi->bmiHeader));
	pbmi->bmiHeader.biSize = sizeof(pbmi->bmiHeader);
	pbmi->bmiHeader.biWidth = rectSize.cx;
	pbmi->bmiHeader.biHeight = rectSize.cy;
	pbmi->bmiHeader.biPlanes = 1;
	pbmi->bmiHeader.biBitCount = GetDeviceCaps(compatiableHDC, BITSPIXEL);
	pbmi->bmiHeader.biCompression = BI_RGB;
	pbmi->bmiHeader.biSizeImage = rectSize.cx * rectSize.cy * 4; //to cover 8/16/24/32bit

	void* pBits;
	hBitmap = ::CreateDIBSection(NULL, pbmi, DIB_RGB_COLORS, &pBits, NULL, 0);

	delete[]pbmi;
	return S_OK;
}

BOOL gdi_desktop_capturer::do_bit_blt(HDC hdc, int destX, int destY, int width, int height, HDC src_dc, int srcX, int srcY)
{
	if (width * 9 > height * 16)
		destY += (width * 9 / 16 - height) / 2;
	else
		destX += (height * 16 / 9 - width) / 2;

	BOOL res = TRUE;
	if (GetLayout(src_dc) == LAYOUT_RTL)
	{
		res = StretchBlt(hdc, destX, destY, width, height, src_dc, srcX + width, srcY, width * -1, height, SRCCOPY);
	}
	else
	{
		res = BitBlt(hdc, destX, destY, width, height, src_dc, srcX, srcY, SRCCOPY);
	}
	return res;
}

BOOL gdi_desktop_capturer::cursor_point_adapter(RECT& deskRect)
{
	int LogicWidth, LogicHeight, PysicalWidth, PysicalHeight = 0;

	HMONITOR MonitorOfCurrentWindow = MonitorFromRect(&deskRect, MONITOR_DEFAULTTONEAREST);
	if (!MonitorOfCurrentWindow) return FALSE;

	MONITORINFOEXW mi;
	mi.cbSize = sizeof(mi);
	if (!GetMonitorInfoW(MonitorOfCurrentWindow, &mi)) return FALSE;

	DEVMODEW dev;
	std::wstring deviceName = mi.szDevice;
	if (!EnumDisplaySettingsW(deviceName.c_str(), ENUM_CURRENT_SETTINGS, &dev)) return FALSE;

	LogicWidth = mi.rcMonitor.right - mi.rcMonitor.left;
	LogicHeight = mi.rcMonitor.bottom - mi.rcMonitor.top;
	PysicalWidth = dev.dmPelsWidth;
	PysicalHeight = dev.dmPelsHeight;

	if (LogicHeight == 0 || LogicWidth == 0 || PysicalHeight == 0 || PysicalWidth == 0) return FALSE;

	deskRect.left = dev.dmPosition.x + (deskRect.left - mi.rcMonitor.left) * PysicalWidth / LogicWidth;
	deskRect.right = dev.dmPosition.x + (deskRect.right - mi.rcMonitor.left) * PysicalWidth / LogicWidth;
	deskRect.top = dev.dmPosition.y + (deskRect.top - mi.rcMonitor.top) * PysicalHeight / LogicHeight;
	deskRect.bottom = dev.dmPosition.y + (deskRect.bottom - mi.rcMonitor.top) * PysicalHeight / LogicHeight;

	return TRUE;
}


#pragma endregion

#pragma region frtc_content_capturer implement

frtc_content_capturer::frtc_content_capturer() :
	_callback(NULL),
	_webrtc_capture_callback(NULL),
	_type(frtc_capture_type::none),
	_source_id(-1)
{
	_webrtc_capturer.reset(new webrtc_desktop_capture());
	_webrtc_capturer->Init(webrtc_capture_type::window_capture);
}

frtc_content_capturer::~frtc_content_capturer()
{
	if (_monitor_capture_started)
		stop();
	_gdi_desktop_capturer.reset();
	_gdi_desktop_capturer = nullptr;
	_dxgi_desktop_capturer.reset();
	_dxgi_desktop_capturer = nullptr;

	_webrtc_capturer->Stop();
	_webrtc_capturer.reset();
	_webrtc_capturer = nullptr;
}

bool frtc_content_capturer::init(frtc_capture_type type, int content_source_id, const wchar_t* content_source_name)
{
	_type = type;
	_source_id = content_source_id;

	if (_type == frtc_capture_type::window)
	{
		//_webrtc_capturer->SetCaptureCallback(_webrtc_capture_callback);
	}
	else
	{
		if (!content_source_name)
			return false;
		_use_dxgi_desktop_capturer = IsWin8OrLater();
		if (_use_dxgi_desktop_capturer)
		{
			_dxgi_desktop_capturer.reset(new DXGI_DesktopDuplicator(content_source_id, content_source_name));
			if (!_dxgi_desktop_capturer->init())
			{
				HRESULT err = _dxgi_desktop_capturer->get_dxgi_error();
				_dxgi_desktop_capturer.reset(NULL);
				ErrorLog("frtc_content_capturer::init init _dxgi_desktop_capturer failed 0x%x", err);
				return false;
			}
		}
		else
		{
			_gdi_desktop_capturer.reset(new gdi_desktop_capturer());
			_gdi_desktop_capturer->init(content_source_id, content_source_name);
		}
	}
	return true;
}

void frtc_content_capturer::get_content_source_windows_list(std::string& list_json_str, HWND excluded_hwnd)
{
	webrtc::DesktopCapturer::SourceList windowList;
	windowList.clear();
	_webrtc_capturer->GetCaptureSource(windowList);

	Json::Value jsonWindowsArray(Json::arrayValue);

	for (const webrtc::DesktopCapturer::Source& s : windowList)
	{
		if ((HWND)s.id != excluded_hwnd)
		{
			Json::Value jsonWindow;
			jsonWindow["windowTitle"] = s.title;
			jsonWindow["hwnd"] = s.id;
			jsonWindowsArray.append(jsonWindow);
		}
	}

	Json::Value jsonWindows;
	jsonWindows["windows"] = jsonWindowsArray;

	Json::FastWriter writer;
	list_json_str = writer.write(jsonWindows);
}

void frtc_content_capturer::set_capture_callback(interface_frtc_capture_content_callback* callback)
{
	_callback = callback;
	if(_type == frtc_capture_type::monitor && !_use_dxgi_desktop_capturer && _gdi_desktop_capturer)
		_gdi_desktop_capturer->set_capture_callback(_callback);
}

void frtc_content_capturer::set_webrtc_capture_callback(IWebRTCCaptureCallback* webrtc_callback)
{
	_webrtc_capture_callback = webrtc_callback;
	_webrtc_capturer->SetCaptureCallback(_webrtc_capture_callback);
}


void frtc_content_capturer::start(bool use_minimize_rate)
{
	_use_minimize_rate = use_minimize_rate;
	if (_type == frtc_capture_type::window)
	{
		_webrtc_capturer->SetCaptureSource((HWND)_source_id);
		_webrtc_capturer->SetInterval(_use_minimize_rate ? 200 : 55);
		_webrtc_capturer->Start();
	}
	else
	{
		if (!_monitor_capture_started)
		{
			_stop_monitor_capture = false;
			_monitor_capture_timer_thread.reset();
			_monitor_capture_timer_thread = std::make_unique<std::thread>(monitor_capture_timer_proc, this);
			_monitor_capture_timer_thread->detach();
			_monitor_capture_started = true;
		}
	}
}

void frtc_content_capturer::stop()
{
	if (_type == frtc_capture_type::window)
	{
		_webrtc_capturer->Stop();
	}
	else
	{
		_stop_monitor_capture = true;
		std::this_thread::sleep_for(std::chrono::milliseconds(500));
		if (_monitor_capture_timer_thread && _monitor_capture_timer_thread->native_handle())
			TerminateThread(_monitor_capture_timer_thread->native_handle(), 0);
		_monitor_capture_started = false;
	}
}

void frtc_content_capturer::monitor_capture_timer_proc(frtc_content_capturer* capturer)
{
	std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	int dst_w = 0;
	int dst_h = 0;
	void* dxgi_capture_buffer = NULL;
	while (!capturer->_stop_monitor_capture)
	{
		if (capturer->_use_dxgi_desktop_capturer)
		{
			bool res = capturer->_dxgi_desktop_capturer->capture_screen(&dxgi_capture_buffer, &dst_w, &dst_h);		
			if (!res)
			{
				ErrorLog("dxgi_desktop_capturer capture_screen failed!");
			}
			else
			{
				capturer->_callback->on_capture(dxgi_capture_buffer, dst_w, dst_h);
			}
		}
		else
		{
			//_gdi_desktop_capturer call capture callback automatically
			HRESULT hRes = capturer->_gdi_desktop_capturer->capture_to_buf();
			if (FAILED(hRes))
			{
				ErrorLog("gdi_desktop_capturer capture_screen failed! 0x%x", hRes);
			}
		}
		std::this_thread::sleep_for(std::chrono::milliseconds(1000 / (capturer->_use_minimize_rate ? 200 : 55)));
	}
}

#pragma endregion

