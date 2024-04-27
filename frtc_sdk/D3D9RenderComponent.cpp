#include "stdafx.h"
#include "D3D9RenderComponent.h"
#include "frtc_sdk_util.h"
#include <sstream>  
#include "frtccall_manager.h"
#include "FrtcMeetingWindow.h"
#include "log.h"

#include <memory>

#define D3D9_LOG(level, fmt, ...)  \
RTC::Log("RENDER", level, "[%s:%d] " fmt, __FILENAME__, __LINE__, ##__VA_ARGS__)

#define D3D9Error(fmt, ...)      D3D9_LOG(RTC::kError, fmt, ##__VA_ARGS__)
#define D3D9Warn(fmt, ...)       D3D9_LOG(RTC::kWarn, fmt, ##__VA_ARGS__)
#define D3D9Info(fmt, ...)       D3D9_LOG(RTC::kInfo, fmt, ##__VA_ARGS__)
#define D3D9Debug(fmt, ...)      D3D9_LOG(RTC::kDebug, fmt, ##__VA_ARGS__)

extern std::shared_ptr<FrtcManager> g_frtc_mgr;
extern FrtcMeetingWindow* g_frtc_meeting_wnd;

template <typename T>
void release_com_interface(T** ppT)
{
	if (*ppT)
	{
		(*ppT)->Release();
		*ppT = NULL;
	}
}

std::string d3d_fmt_to_string(D3DFORMAT fmt)
{
	if (fmt == D3DFMT_X8R8G8B8)
	{
		return std::string("D3DFMT_X8R8G8B8");
	}
	else if (fmt == D3DFMT_YV12)
	{
		return std::string("D3DFMT_YV12");
	}
	else if (fmt == D3DFMT_I420)
	{
		return std::string("D3DFMT_I420");
	}
	else
	{
		std::stringstream fmt_stream;
		fmt_stream << fmt;
		return fmt_stream.str();
	}
}

D3D9RenderComponent::D3D9RenderComponent(HWND hWnd,
	IDirect3DDevice9* device,
	D3DPRESENT_PARAMETERS device_param,
	D3DFORMAT offScreenFormat,
	UINT width,
	UINT height)
	: _border_in_last_frame(true),
	_wnd(hWnd),
	_width(width),
	_height(height),
	_swap_chain_ptr(nullptr),
	_plain_surface_ptr(nullptr),
	_device_ptr(device),
	_device_parameter(device_param),
	_offscreen_format(offScreenFormat)
{
}

D3D9RenderComponent::~D3D9RenderComponent()
{
	release_com_interface(&_swap_chain_ptr);
	release_com_interface(&_plain_surface_ptr);
}

HRESULT D3D9RenderComponent::create_render()
{
	if (!_device_ptr)
	{
		D3D9Error("create failed, device_ptr is NULL");
		return -1;
	}

	if (_swap_chain_ptr)
	{
		release_com_interface(&_swap_chain_ptr);
	}

	D3DPRESENT_PARAMETERS d3d_param = { 0 };
	d3d_param.BackBufferWidth = _width;
	d3d_param.BackBufferHeight = _height;
	d3d_param.Windowed = TRUE;
	d3d_param.SwapEffect = D3DSWAPEFFECT_FLIP;
	d3d_param.hDeviceWindow = _wnd;
	d3d_param.BackBufferFormat = _device_parameter.BackBufferFormat;
	d3d_param.Flags = D3DPRESENTFLAG_VIDEO;
	d3d_param.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
	d3d_param.BackBufferCount = 2;

	HRESULT ret = _device_ptr->CreateAdditionalSwapChain(&d3d_param,
		&_swap_chain_ptr);
	if (FAILED(ret))
	{
		D3D9Error("create swap chain failed, window=0x%x", _wnd);
		return ret;
	}

	D3D9Info("create swap chain success, window=0x%x, type=%s",
		_wnd, d3d_fmt_to_string(d3d_param.BackBufferFormat).c_str());

	int offscreen_width = 1920;
	int offscreen_height = 1080;
	ret = _device_ptr->CreateOffscreenPlainSurface(offscreen_width,
		offscreen_height,
		_offscreen_format,
		D3DPOOL_DEFAULT,
		&_plain_surface_ptr,
		NULL);
	if (SUCCEEDED(ret))
	{
		D3D9Info("create plain surface success, window=0x%x, type=%s",
			_wnd, d3d_fmt_to_string(_offscreen_format).c_str());
		return ret;
	}

	_offscreen_format = D3DFMT_YUY2;
	ret = _device_ptr->CreateOffscreenPlainSurface(offscreen_width,
		offscreen_height,
		_offscreen_format,
		D3DPOOL_DEFAULT,
		&_plain_surface_ptr,
		NULL);
	if (SUCCEEDED(ret))
	{
		D3D9Info("create plain surface success, window=0x%x, type=%s",
			_wnd, d3d_fmt_to_string(_offscreen_format).c_str());
		return ret;
	}

	_offscreen_format = D3DFMT_X8R8G8B8;
	ret = _device_ptr->CreateOffscreenPlainSurface(offscreen_width,
		offscreen_height,
		_offscreen_format,
		D3DPOOL_DEFAULT,
		&_plain_surface_ptr,
		NULL);
	if (SUCCEEDED(ret))
	{
		D3D9Info("create plain surface success, window=0x%x, type=%s",
			_wnd, d3d_fmt_to_string(_offscreen_format).c_str());
		return ret;
	}

	D3D9Error("create plain surface failed, window=0x%x, type=%s",
		_wnd, d3d_fmt_to_string(_offscreen_format).c_str());
	release_com_interface(&_swap_chain_ptr);
	return ret;
}

HRESULT D3D9RenderComponent::destroy_render()
{
	release_com_interface(&_plain_surface_ptr);
	release_com_interface(&_swap_chain_ptr);
	return S_OK;
}

HRESULT D3D9RenderComponent::recreate_render(IDirect3DDevice9* device,
	D3DPRESENT_PARAMETERS device_param)
{
	_device_ptr = device;
	_device_parameter = device_param;
	return create_render();
}

IDirect3DSurface9* D3D9RenderComponent::get_backbuffer()
{
	IDirect3DSurface9* surface = NULL;
	_swap_chain_ptr->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, &surface);
	return surface;
}

HRESULT D3D9RenderComponent::draw_frame(D3DFORMAT format,
	BYTE* image,
	int width,
	int height,
	bool has_border,
	int border_width,
	bool is_content,
	std::string watermark_str)
{
	if (width <= 0 || height <= 0 || width > 1920 || height > 1080)
	{
		return S_OK;
	}

	if (_swap_chain_ptr == NULL || image == NULL)
	{
		return S_OK;
	}

	int interval = time(0) - g_frtc_mgr->get_name_card_begin_time();

	D3DLOCKED_RECT lr;
	HRESULT ret = _plain_surface_ptr->LockRect(&lr, NULL, D3DLOCK_NOSYSLOCK);
	if (FAILED(ret))
	{
		D3D9Error("LockRect failed, ret=0x%x", ret);
		return ret;
	}

	int resize_w = width;
	int resize_h = height;
	BYTE* new_image = NULL;

	if (is_content)
	{
		resize_w = 1920;
		resize_h = 1080;

		if (new_image == NULL)
		{
			new_image = new BYTE[resize_w * resize_h * 3 / 2];
		}

		int rc = g_frtc_mgr->resize_yuv420(image,
			width,
			height,
			new_image,
			resize_w,
			resize_h);
		if (rc != 0)
		{
			delete[] new_image;
			return S_OK;
		}
	}
	else
	{
		new_image = image;
	}

	if (is_content && (watermark_str.length() > 0))
	{
		int new_width = 0;
		int w = resize_w;
		int h = resize_h;
		BYTE* watermark_img = g_frtc_mgr->get_watermark_yuv_data(watermark_str,
			w,
			h,
			new_width);
		if (new_width == 320)
		{
			FRTCSDK::FRTCSdkUtil::merge_yuv420(watermark_img,
				320,
				180,
				new_image,
				resize_w,
				resize_h);
		}
		else
		{
			FRTCSDK::FRTCSdkUtil::merge_yuv420(watermark_img,
				w,
				h,
				new_image,
				resize_w,
				resize_h);
		}
	}

	if (format == _offscreen_format && _offscreen_format == D3DFMT_YV12)
	{
		BYTE* dest = (BYTE*)lr.pBits;
		BYTE* src = new_image;
		int stride = lr.Pitch;
		copy_frame_to_d3d_rect_yv12(src, dest, resize_w, resize_h, stride);
	}
	else if (format == D3DFMT_YV12 && _offscreen_format == D3DFMT_X8R8G8B8)
	{
		FRTCSDK::FRTCSdkUtil::yuv420_to_argb(new_image,
			new_image + resize_w * resize_h,
			new_image + resize_w * resize_h * 5 / 4,
			(BYTE*)lr.pBits,
			resize_w,
			resize_h);
	}

	if (is_content && new_image)
	{
		delete[] new_image;
	}

	ret = _plain_surface_ptr->UnlockRect();
	if (FAILED(ret))
	{
		D3D9Error("UnlockRect failed, ret=0x%X", ret);
		return ret;
	}

	IDirect3DSurface9* surface = NULL;
	ret = _swap_chain_ptr->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, &surface);
	if (FAILED(ret))
	{
		D3D9Error("get_backbuffer failed, ret=0x%X", ret);
		return ret;
	}

	RECT src_rect, dst_rect;
	::SetRect(&src_rect, 0, 0, resize_w, resize_h);
	if (has_border)
	{
		ret = _device_ptr->ColorFill(surface, NULL, D3DCOLOR_ARGB(0, 60, 210, 60));
		::SetRect(&dst_rect,
			border_width,
			border_width,
			_width - border_width,
			_height - border_width);
	}
	else
	{
		if(_border_in_last_frame)
		{
			ret = _device_ptr->ColorFill(surface, NULL, D3DCOLOR_ARGB(0, 0, 0, 0));
		}
		::SetRect(&dst_rect, 0, 0, _width, _height);
		if (!_frame_width_logged && resize_w < _width)
		{
			WarnLog("resize_w < _width, resize_w=%d, resize_h=%d, _width=%d, _height=%d", resize_w, resize_h, _width, _height);
			_frame_width_logged = true;
		}
	}

	_border_in_last_frame = has_border;

	ret = _device_ptr->StretchRect(_plain_surface_ptr,
		&src_rect,
		surface,
		&dst_rect,
        D3DTEXF_POINT);
		// D3DTEXF_LINEAR);
	if (FAILED(ret))
	{
		D3D9Error("StretchRect failed, ret=0x%X", ret);
		release_com_interface(&surface);
		return ret;
	}

	ret = _swap_chain_ptr->Present(NULL, NULL, NULL, NULL, 0);
	release_com_interface(&surface);
	return ret;
}

void D3D9RenderComponent::copy_frame_to_d3d_rect_yv12(BYTE* src,
	BYTE* dest,
	int width,
	int height,
	int stride)
{
	if (width > 1920 || height > 1080)
	{
		return;
	}

	int buffer_height = 1080;
	for (int i = 0; i < height; i++)
	{
		memcpy(dest + i * stride, src + i * width, width);
	}

	for (int i = 0; i < height / 2; i++)
	{
		memcpy(dest + stride * buffer_height + i * stride / 2,
			src + width * height + width * height / 4 + i * width / 2,
			width / 2);
	}

	for (int i = 0; i < height / 2; i++)
	{
		memcpy(dest + stride * buffer_height + stride * buffer_height / 4 + i * stride / 2,
			src + width * height + i * width / 2,
			width / 2);
	}
}

D3D9RendererEngine::D3D9RendererEngine()
	: _wnd(NULL),
	_d3d9_ex(nullptr),
	_device_ptr(nullptr)
{
	ZeroMemory(&_d3d_param, sizeof(_d3d_param));
}

D3D9RendererEngine::~D3D9RendererEngine()
{
	destory_d3d9_device();
}

HRESULT D3D9RendererEngine::create_d3d9_device(HWND hwnd)
{
	if (_device_ptr)
	{
		return S_OK;
	}

	HRESULT ret = S_OK;
	if (_d3d9_ex == nullptr)
	{
		ret = Direct3DCreate9Ex(D3D_SDK_VERSION, &_d3d9_ex);
		if (_d3d9_ex == nullptr)
		{
			D3D9Error("Direct3DCreate9Ex failed, ret=0x%x", ret);
			return E_FAIL;
		}
	}

	D3DDISPLAYMODE mode = { 0 };
	ret = _d3d9_ex->GetAdapterDisplayMode(D3DADAPTER_DEFAULT, &mode);
	if (FAILED(ret))
	{
		D3D9Error("GetAdapterDisplayMode failed, ret=0x%x", ret);
		return ret;
	}

	D3DPRESENT_PARAMETERS d3d_param = { 0 };
	d3d_param.BackBufferFormat = mode.Format;
	d3d_param.SwapEffect = D3DSWAPEFFECT_COPY;
	d3d_param.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
	d3d_param.Windowed = TRUE;
	d3d_param.hDeviceWindow = hwnd;
	_d3d_param = d3d_param;

	ret = _d3d9_ex->CreateDevice(D3DADAPTER_DEFAULT,
		D3DDEVTYPE_HAL,
		hwnd,
		D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_FPU_PRESERVE,
		&d3d_param,
		&_device_ptr);
	if (FAILED(ret))
	{
		D3D9Error("CreateDevice failed, ret=0x%x", ret);
		return ret;
	}

	_wnd = hwnd;

	return ret;
}

HRESULT D3D9RendererEngine::reset_d3d9_device()
{
	HRESULT ret = S_OK;
	if (_device_ptr)
	{
		auto it = _render_list.begin();
		for (; it != _render_list.end(); it++)
		{
			it->second->destroy_render();
		}

		D3DPRESENT_PARAMETERS d3dpp = _d3d_param;
		ret = _device_ptr->Reset(&d3dpp);
		if (FAILED(ret))
		{
			D3D9Error("Reset failed, ret=0x%x", ret);

			release_com_interface(&_device_ptr);
			release_com_interface(&_d3d9_ex);
			_device_ptr = NULL;
			_d3d9_ex = NULL;

			ret = create_d3d9_device(_wnd);
			if (FAILED(ret))
			{
				D3D9Error("create_d3d9_device failed, ret=0x%x", ret);
				return ret;
			}
		}
	}
	else
	{
		ret = create_d3d9_device(_wnd);
		if (FAILED(ret))
		{
			D3D9Error("create_d3d9_device failed, ret=0x%x", ret);
			return ret;
		}
	}

	auto it = _render_list.begin();
	for (; it != _render_list.end(); it++)
	{
		it->second->recreate_render(_device_ptr, _d3d_param);
	}

	return S_OK;
}

void D3D9RendererEngine::destory_d3d9_device()
{
	auto it = _render_list.begin();
	for (; it != _render_list.end(); it++)
	{
		delete it->second;
	}

	_render_list.clear();
	release_com_interface(&_device_ptr);
	release_com_interface(&_d3d9_ex);
}

HRESULT D3D9RendererEngine::create_render_component(HWND hwnd,
	int width,
	int height,
	D3DFORMAT format)
{
	D3D9RenderComponent* render = new D3D9RenderComponent(hwnd,
		_device_ptr,
		_d3d_param,
		format,
		width,
		height);
	if (render == nullptr)
	{
		DWORD err = GetLastError();
		D3D9Error("create D3D9RenderComponent failed, error=0x%x", err);
		return -1;
	}

	_render_list[hwnd] = render;
	HRESULT ret = render->create_render();

	return ret;
}

HRESULT D3D9RendererEngine::draw_frame(HWND hwnd,
	D3DFORMAT format,
	BYTE* image,
	int width,
	int height,
	bool has_border,
	bool is_content,
	std::string watermark_str)
{
	if (!this)
	{
		return 0;
	}

	if (_device_ptr == NULL)
	{
		return S_OK;
	}

	HRESULT ret = test_cooperative_level();
	if (FAILED(ret))
	{
		D3D9Error("test_cooperative_level failed, ret=0x%x", ret);
		return ret;
	}

	D3D9RenderComponent* render = _render_list[hwnd];
	if (render == NULL)
	{
		D3D9Error("null render, return");
		return S_OK;
	}

	int border_width = 3;
	RECT win_rect;
	GetWindowRect(hwnd, &win_rect);

	if (win_rect.right - win_rect.left >= 1080)
	{
		border_width = 5;
	}
	/*lse if (win_rect.right - win_rect.left >= 720)
	{
		border_width = 5;
	}
	else if (win_rect.right - win_rect.left >= 360)
	{
		border_width = 8;
	}
	else if (win_rect.right - win_rect.left >= 240)
	{
		border_width = 10;
	}
	else
	{
		border_width = 12;
	}*/

	return render->draw_frame(format,
		image,
		width,
		height,
		has_border,
		border_width,
		is_content,
		watermark_str);
}

HRESULT D3D9RendererEngine::test_cooperative_level()
{
	if (_device_ptr == NULL)
	{
		D3D9Error("D3D device is null");
		return E_FAIL;
	}

	HRESULT ret = _device_ptr->TestCooperativeLevel();
	if (ret == D3DERR_DEVICELOST)
	{
		D3D9Error("TestCooperativeLevel failed. D3DERR_DEVICELOST");

		DWORD session_id = WTSGetActiveConsoleSessionId();
		WTSINFOEXW info_ex;
		DWORD result_bytes = 0;

		WTSQuerySessionInformationW(NULL,
			session_id,
			WTSSessionInfoEx,
			(LPWSTR*)&info_ex,
			&result_bytes);

		if (info_ex.Data.WTSInfoExLevel1.SessionFlags == 1)
		{
			ret = reset_d3d9_device();
			if (ret == D3DERR_DEVICENOTRESET)
			{
				D3D9Error("reset_d3d9_device, D3DERR_DEVICENOTRESET");
			}
			else
			{
				D3D9Info("reset_d3d9_device, ret=0x%x", ret);
			}
		}
		else
		{
			D3D9Debug("WTSQuerySessionInformationW, session flag=%d",
				info_ex.Data.WTSInfoExLevel1.SessionFlags);
		}
	}
	else if (ret == D3DERR_DEVICENOTRESET)
	{
		ret = reset_d3d9_device();
		if (ret == D3DERR_DEVICENOTRESET)
		{
			D3D9Error("reset_d3d9_device, D3DERR_DEVICENOTRESET");
		}
		else
		{
			D3D9Info("reset_d3d9_device, ret=0x%x", ret);
		}
	}

	return ret;
}


