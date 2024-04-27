#include "stdafx.h"
#include "FrtcMeetingWindow.h"
#include "frtccall_manager.h"
#include "frtc_sdk_util.h"
#include "log.h"

#include <Dbt.h>
#include <initguid.h>
#include <usbiodef.h>
#include <SetupAPI.h>
#include <memory>

extern FrtcMeetingWindow* g_frtc_meeting_wnd;
extern HINSTANCE g_hInstance;
extern std::shared_ptr<FrtcManager> g_frtc_mgr;

bool g_last_camera_status = false;
bool g_last_mic_status = false;

/* https://learn.microsoft.com/en-us/windows-hardware/drivers/install/guid-devinterface-monitor */
GUID GUID_DEVINTERFACE_MONITOR = { 0xE6F07B5F, 0xEE97, 0x4a90,
					  0xB0,0x76,0x33,0xF5,0x7B,0xF4,0xEA,0xA7 };

FrtcMeetingWindow::FrtcMeetingWindow(std::shared_ptr<FrtcManager> frtc_mgr) 
    : FRTCBaseWnd(true), 
      _is_video_started(false), 
      _is_audio_started(false), 
      _is_call_started(false),
      _is_sharing_content(false),
      _is_layout_enabled(true),
      _is_media_encrypted(false),
      _frtc_mgr(frtc_mgr),
      _click_start_time(0)
{	
}

FrtcMeetingWindow::~FrtcMeetingWindow()
{
}

void FrtcMeetingWindow::init_main_window(std::shared_ptr<FrtcManager> frtc_mgr)
{
	release_all();
	clear_params();

	_frtc_mgr = frtc_mgr;
	_is_sharing_content = false;
	_is_media_encrypted = false;
	_is_layout_enabled = true;
	_click_start_time = 0;
}

void FrtcMeetingWindow::toggle_full_screen(bool doFullScreen)
{
	HWND hwnd = (HWND)*g_frtc_meeting_wnd;
	RECT win_rect;
	::GetWindowRect(hwnd, &win_rect);

	RECT desktop_rect;
	HMONITOR monitor = MonitorFromWindow((HWND)*g_frtc_meeting_wnd, 
                                          MONITOR_DEFAULTTONULL);
	if (monitor)
	{
		MONITORINFOEX monitor_info;
		monitor_info.cbSize = sizeof(monitor_info);
		::GetMonitorInfo(monitor, (LPMONITORINFO)&monitor_info);

		desktop_rect.left = monitor_info.rcMonitor.left;
		desktop_rect.right = monitor_info.rcMonitor.right;
		desktop_rect.top = monitor_info.rcMonitor.top;
		desktop_rect.bottom = monitor_info.rcMonitor.bottom;
	}
	else
	{
		HWND desktop_win;
		desktop_win = ::GetDesktopWindow();
		::GetWindowRect(desktop_win, &desktop_rect);
	}

	if (!doFullScreen)
	{
		_frtc_mgr->enter_full_screen(false);
		::SetWindowLong(hwnd, GWL_STYLE, _wnd_style);
		::SetWindowLong(hwnd, GWL_EXSTYLE, _wnd_ex_style);
		::SetWindowPos(hwnd, 
                       NULL, 
                       _mvw_rc.left, 
                       _mvw_rc.top, 
                       _mvw_rc.right - _mvw_rc.left, 
                       _mvw_rc.bottom - _mvw_rc.top, 
                       SWP_SHOWWINDOW);
	}
	else
	{
		_frtc_mgr->enter_full_screen(true);
		_wnd_style = ::GetWindowLong(hwnd, GWL_STYLE);
		_wnd_ex_style = ::GetWindowLong(hwnd, GWL_EXSTYLE);
		::GetWindowRect(hwnd, &_mvw_rc);

		::SetWindowLong(hwnd, 
                        GWL_EXSTYLE, 
                        GetWindowLong(hwnd, GWL_EXSTYLE) & ~(WS_EX_WINDOWEDGE | WS_EX_DLGMODALFRAME));

		::SetWindowLong(hwnd, 
                        GWL_STYLE, 
                        GetWindowLong(hwnd, GWL_STYLE) & ~WS_CAPTION);

		::SetWindowPos(hwnd, 
                       NULL, 
                       desktop_rect.left, 
                       desktop_rect.top, 
                       desktop_rect.right - desktop_rect.left, 
                       desktop_rect.bottom - desktop_rect.top, 
                       SWP_SHOWWINDOW);
	}
}

void FrtcMeetingWindow::set_audio_only_mode(BOOL audioOnlyMode)
{
	_is_audio_only = audioOnlyMode;
}

bool FrtcMeetingWindow::get_content_state()
{
	return _is_sharing_content;
}

void FrtcMeetingWindow::update_creat_options(std::shared_ptr<CREATE_OPTIONS> create_options)
{
	this->_create_options = create_options;
}

void FrtcMeetingWindow::set_create_options(std::shared_ptr<CREATE_OPTIONS> create_options)
{
	this->_create_options = create_options;
	typedef PVOID(*RegisterSuspendResumeFunc)(HWND, DWORD);

	HMODULE user32_module = LoadLibrary(L"User32.dll");
	if (user32_module)
	{
		RegisterSuspendResumeFunc RegisterSuspendResume = 
            (RegisterSuspendResumeFunc)GetProcAddress(
                user32_module, "RegisterSuspendResumeNotification");
		if (RegisterSuspendResume)
		{
			RegisterSuspendResume((HWND)*this, DEVICE_NOTIFY_WINDOW_HANDLE);
		}
		else
		{
			WarnLog("RegisterSuspendResumeNotification function not found");
		}
	}
}

void FrtcMeetingWindow::set_switch_layout_enable(bool enable)
{
	_is_layout_enabled = enable;
}

bool FrtcMeetingWindow::is_switch_layout_enabled()
{
	return _is_layout_enabled;
}

void FrtcMeetingWindow::start_statictisc_query_timer()
{
	::KillTimer((HWND)*this, IDT_QUERY_STATISTICS);
	_frtc_mgr->get_network_intensity();
	::SetTimer((HWND)*this, IDT_QUERY_STATISTICS, 5000, NULL);
}

void FrtcMeetingWindow::on_encryption_state_changed(bool encrypted)
{
	_is_media_encrypted = encrypted;
}

void FrtcMeetingWindow::close_manually()
{
	_frtc_mgr->end_meeting(0);
}

void FrtcMeetingWindow::get_sharing_monitor_rect(RECT& rect)
{
	if (_sharing_monitor_handle)
	{
		InfoLog("sharing_monitor_handle exists, try get the monitor rect");

		MONITORINFOEX monitor_info;
		monitor_info.cbSize = sizeof(monitor_info);
		BOOL ret = ::GetMonitorInfo(_sharing_monitor_handle, (LPMONITORINFO)&monitor_info);

		InfoLog("got sharing_monitor_handle rect, L: %d, R: %d, T: %d, B: %d",
			     monitor_info.rcMonitor.left, monitor_info.rcMonitor.right, monitor_info.rcMonitor.top, 
                 monitor_info.rcMonitor.bottom);

		rect.left = monitor_info.rcMonitor.left;
		rect.right = monitor_info.rcMonitor.right;
		rect.top = monitor_info.rcMonitor.top;
		rect.bottom = monitor_info.rcMonitor.bottom;
	}
	else
	{
		InfoLog("sharing_monitor_handle not exists, try get current desktop rect");

		HWND desktop_win;
		desktop_win = ::GetDesktopWindow();
		::GetWindowRect(desktop_win, &rect);

		InfoLog("got current desktop rect, L: %d, R: %d, T: %d, B: %d",
			     rect.left, rect.right, rect.top, rect.bottom);
	}
}

void FrtcMeetingWindow::clear_params()
{
	memset(&_create_options, 0, sizeof(CREATE_OPTIONS));
	_is_sharing_content = false;
	_meeting_id = L"";
	_window_title = L"";
	_meeting_start_time = 0;

	_is_video_started = false;
	_is_audio_started = false;
	_is_call_started = false;

	_wnd_style = 0;
	_wnd_ex_style = 0;
	_sharing_monitor_handle = NULL;

	_roster_list.clear();

	_is_layout_enabled = true;
}

void FrtcMeetingWindow::set_meeting_id(std::wstring& meetingId)
{
	_meeting_id = meetingId;
	time(&_meeting_start_time);
}

void FrtcMeetingWindow::set_window_title(std::wstring& title)
{
	_window_title = title;
}

void FrtcMeetingWindow::set_subwindow_center_pos(HWND hWnd)
{
	HMONITOR _hMonitor = MonitorFromWindow((HWND)*g_frtc_meeting_wnd, 
                                           MONITOR_DEFAULTTONULL);

	RECT desktop_rect;
	if (_hMonitor)
	{
		MONITORINFOEX monitor_info;
		monitor_info.cbSize = sizeof(monitor_info);
		BOOL ret = ::GetMonitorInfo(_hMonitor, (LPMONITORINFO)&monitor_info);
		desktop_rect.left = monitor_info.rcWork.left;
		desktop_rect.right = monitor_info.rcWork.right;
		desktop_rect.top = monitor_info.rcWork.top;
		desktop_rect.bottom = monitor_info.rcWork.bottom;
	}
	else
	{
		HWND desktop_win;
		desktop_win = ::GetDesktopWindow();
		::GetWindowRect(desktop_win, &desktop_rect);
	}

	RECT win_rc;
	RECT win_pos;
	::GetWindowRect(hWnd, &win_rc);

	win_pos.left = desktop_rect.left + ((desktop_rect.right - desktop_rect.left) - 
                                        (win_rc.right - win_rc.left)) / 2;

	win_pos.top = desktop_rect.top + ((desktop_rect.bottom - desktop_rect.top) - 
                                      (win_rc.bottom - win_rc.top)) / 2;

	win_pos.right = win_pos.left + (win_rc.right - win_rc.left);
	win_pos.bottom = win_pos.top + (win_rc.bottom - win_rc.top);

	::SetWindowPos(hWnd, 
                   HWND_NOTOPMOST, 
                   win_pos.left, 
                   win_pos.top, 
                   win_pos.right - win_pos.left, 
                   win_pos.bottom - win_pos.top, 
                   SWP_NOOWNERZORDER);
}

BOOL FrtcMeetingWindow::RegisterDeviceNotification_ToHwnd(GUID if_class_guid, 
                                                          HWND hWnd, 
                                                          HDEVNOTIFY* device_notify)
{
	DEV_BROADCAST_DEVICEINTERFACE dev_interface;
	ZeroMemory(&dev_interface, sizeof(dev_interface));

	dev_interface.dbcc_size = sizeof(DEV_BROADCAST_DEVICEINTERFACE);
	dev_interface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
	dev_interface.dbcc_classguid = if_class_guid;

	*device_notify = RegisterDeviceNotification(hWnd, 
                                                &dev_interface, 
                                                DEVICE_NOTIFY_WINDOW_HANDLE);
	if (NULL == *device_notify)
	{
		ErrorLog("RegisterDeviceNotification failed.");
		return FALSE;
	}

	return TRUE;
}

BOOL FrtcMeetingWindow::TrackMouseEvent_Register()
{
	TRACKMOUSEEVENT mev{ 0 };
	mev.hwndTrack = *this;
	mev.dwFlags = TME_LEAVE;
	mev.dwHoverTime = HOVER_DEFAULT;
	mev.cbSize = sizeof(mev);
	BOOL ret = TrackMouseEvent(&mev);
	if (!ret)
	{
		ErrorLog("TrackMouseEvent failed, error is %d", GetLastError());
	}

	return ret;
}

void FrtcMeetingWindow::switch_layout(bool bGrid)
{
	_frtc_mgr->set_layout_grid_mode(bGrid);
}

void FrtcMeetingWindow::release_all()
{
}

LRESULT FrtcMeetingWindow::WndProc(HWND hWnd, 
                                   UINT message, 
                                   WPARAM wParam, 
                                   LPARAM lParam)
{
	static HDEVNOTIFY device_notify;
	static HDEVNOTIFY usb_device_notify;

	switch (message)
	{
	case WM_CREATE:
	{
		if (!RegisterDeviceNotification_ToHwnd(GUID_DEVINTERFACE_MONITOR, 
                                               hWnd, 
                                               &device_notify))
		{
			ErrorLog("RegisterDeviceNotification_ToHwnd failed, exit");
			ExitProcess(1);
		}

		WTSRegisterSessionNotification(hWnd, NOTIFY_FOR_THIS_SESSION);

		RegisterDeviceNotification_ToHwnd(GUID_CLASS_USB_DEVICE, 
                                          hWnd, 
                                          &usb_device_notify);
	}
	break;
	case WM_WTSSESSION_CHANGE:
	{
		switch (wParam)
		{
		case WTS_SESSION_LOCK:
		{
			DebugLog("screen lock");
			_frtc_mgr->stop_sending_content();
			g_last_camera_status = !_frtc_mgr->is_local_video_muted();
			g_last_mic_status = !_frtc_mgr->is_local_audio_muted();
			_frtc_mgr->toggle_local_video_mute(true);
			_frtc_mgr->toggle_local_audio_mute(true);
		}
		break;
		case WTS_SESSION_UNLOCK:
		{
			DebugLog("screen unlock");
			_frtc_mgr->toggle_local_video_mute(!g_last_camera_status);
			_frtc_mgr->toggle_local_audio_mute(!g_last_mic_status);
		}
		break;
		default:
			break;
		}
	}
    break;
	case WM_KEYDOWN:
    {
		switch (wParam)
		{
		case VK_ESCAPE:
		{
			RECT win_rect;
			::GetWindowRect(hWnd, &win_rect);
			RECT desktop_rect;
			HMONITOR monitor = MonitorFromWindow((HWND)*g_frtc_meeting_wnd, 
                                                  MONITOR_DEFAULTTONULL);
			if (monitor)
			{
				MONITORINFOEX monitor_info;
				monitor_info.cbSize = sizeof(monitor_info);
				BOOL ret = ::GetMonitorInfo(monitor, (LPMONITORINFO)&monitor_info);
				desktop_rect.left = monitor_info.rcMonitor.left;
				desktop_rect.right = monitor_info.rcMonitor.right;
				desktop_rect.top = monitor_info.rcMonitor.top;
				desktop_rect.bottom = monitor_info.rcMonitor.bottom;
			}
			else
			{
				HWND desktop_win;
				desktop_win = ::GetDesktopWindow();
				::GetWindowRect(desktop_win, &desktop_rect);
			}

			if (win_rect.right - win_rect.left == desktop_rect.right - desktop_rect.left)
			{
				_frtc_mgr->enter_full_screen(false);
				::SetWindowLong(hWnd, GWL_STYLE, _wnd_style);
				::SetWindowLong(hWnd, GWL_EXSTYLE, _wnd_ex_style);

				::SetWindowPos(hWnd, 
                               NULL, 
                               _mvw_rc.left, 
                               _mvw_rc.top, 
                               _mvw_rc.right - _mvw_rc.left, 
                               _mvw_rc.bottom - _mvw_rc.top, 
                               SWP_SHOWWINDOW);
			}

			_frtc_mgr->exit_full_screen();
		}
		break;
        default:
            break;
		}
    }
	break;
	case WM_SIZING:
	{
		LPRECT main_rc = (LPRECT)lParam;
		_frtc_mgr->on_main_window_size_changed(main_rc->left, 
                                               main_rc->top, 
                                               main_rc->right, 
                                               main_rc->bottom);
	}
	break;
	case WM_MOVE:
	{
	}
	break;
	case WM_DISPLAYCHANGE:
	{
		if (_is_sharing_content)
		{
			std::vector<FRTCSDK::DISPLAY_MONITOR_INFO> monitor_list;
			FRTCSDK::FRTCSdkUtil::get_monitor_list(monitor_list);
			
			bool sharing_monitor_exist = false;
            auto monitor_it = monitor_list.begin();
			while (monitor_it != monitor_list.end())
			{
				if ((*monitor_it).monitor_handle == _sharing_monitor_handle)
				{
					sharing_monitor_exist = true;
					break;
				}

				monitor_it++;
			}

			if (!sharing_monitor_exist)
			{
				_frtc_mgr->stop_sending_content();
				_sharing_monitor_handle = NULL;
				_frtc_mgr->on_monitor_list_changed();
			}
			else
			{
				RECT desktop_rect = { 0 };
				get_sharing_monitor_rect(desktop_rect);
			}
		}
	}
	break;
	case WM_SIZE:
	{
		RECT main_client_rect;
		::GetClientRect(hWnd, &main_client_rect);
		_frtc_mgr->on_main_window_size_changed(main_client_rect.left, 
                                               main_client_rect.top, 
                                               main_client_rect.right - main_client_rect.left, 
                                               main_client_rect.bottom - main_client_rect.top);
	}
	break;
	case WM_MOVING:
	{

	}
	break;
	case WM_CLOSE:
	{
		DebugLog("get WM_CLOSE");

		DebugLog("Un register notifications");
		if (!UnregisterDeviceNotification(device_notify))
		{
			DebugLog("UnregisterDeviceNotification");
		}

		UnregisterDeviceNotification((usb_device_notify));

		release_all();
		clear_params();

		_frtc_mgr->end_meeting(0);
	}
	break;
	case WM_DESTROY:
	{
		if (!UnregisterDeviceNotification(device_notify))
		{
			DebugLog("UnregisterDeviceNotification");
		}

		UnregisterDeviceNotification((usb_device_notify));

		release_all();
		clear_params();

		PostQuitMessage(0);

		delete g_frtc_meeting_wnd;
		g_frtc_meeting_wnd = NULL;
		break;
	}
	case WM_DEVICECHANGE:
	{
		PDEV_BROADCAST_DEVICEINTERFACE dev_if = (PDEV_BROADCAST_DEVICEINTERFACE)lParam;

		if (wParam == DBT_DEVICEREMOVECOMPLETE || wParam == DBT_DEVICEARRIVAL)
		{
			if (dev_if->dbcc_classguid.Data1 == GUID_DEVINTERFACE_MONITOR.Data1 && 
                dev_if->dbcc_classguid.Data2 == GUID_DEVINTERFACE_MONITOR.Data2 && 
                dev_if->dbcc_classguid.Data3 == GUID_DEVINTERFACE_MONITOR.Data3 && 
                0 == memcmp(dev_if->dbcc_classguid.Data4, GUID_DEVINTERFACE_MONITOR.Data4, 8))
			{
				if (_is_sharing_content)
				{
					_frtc_mgr->stop_sending_content();
					_sharing_monitor_handle = NULL;
					_sharing_monitor_handle = NULL;
				}

				_frtc_mgr->on_monitor_list_changed();
			}

			if (wParam == DBT_DEVICEREMOVECOMPLETE)
			{
				_frtc_mgr->on_media_device_removal();
			}
			else if (wParam == DBT_DEVICEARRIVAL)
			{
				_frtc_mgr->on_media_device_arrival();
			}
		}
	}
	break;

	case WM_LBUTTONDBLCLK:
	{
		_click_start_time = GetTickCount();
		::KillTimer(*this, IDT_DB_CLICK);
		g_frtc_mgr->mouse_event_callback(message);
	}
	break;
	case WM_LBUTTONUP:
	{
		DWORD now = GetTickCount();
		UINT double_click_time = GetDoubleClickTime();
		if (_click_start_time != 0 && 
           (now - _click_start_time) > double_click_time)
		{
			::SetTimer(*this, IDT_DB_CLICK, double_click_time, NULL);
		}
		else if (_click_start_time == 0)
		{
			::SetTimer(*this, IDT_DB_CLICK, double_click_time, NULL);
		}

		_click_start_time = GetTickCount();
	}
	break;
	case WM_TIMER:
	{
		switch (wParam)
		{
		case IDT_RESTART_CAMERA:
		{
			_frtc_mgr->restart_camera();
		}
		break;
		case IDT_UPDATE_DEVICE_SETTING:
		{
			_frtc_mgr->update_camera_devices();
			::KillTimer((HWND)*this, IDT_UPDATE_DEVICE_SETTING);
		}
		break;
		case IDT_UPDATE_AUDIO_DEVICE:
		{
			::KillTimer((HWND)*this, IDT_UPDATE_AUDIO_DEVICE);
			_frtc_mgr->update_audio_devices();
			_frtc_mgr->notify_audio_device_reset(FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_SPEAKER);
			_frtc_mgr->notify_audio_device_reset(FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_MIC);
		}
		break;
		case IDT_UPDATE_DEFAULT_AUDIO_DEVICE:
		{
			_frtc_mgr->sync_audio_device_with_os();
			::KillTimer((HWND)*this, IDT_UPDATE_DEFAULT_AUDIO_DEVICE);
		}
		break;
		case IDT_QUERY_STATISTICS:
		{
			std::string report = _frtc_mgr->get_statistics_jstr();
		}
		break;
		case IDT_DB_CLICK:
		{
			g_frtc_mgr->mouse_event_callback(WM_LBUTTONUP);

			::KillTimer(*this, IDT_DB_CLICK);
		}
		break;
		case IDT_MOUSEENTER:
		{
			g_frtc_mgr->mouse_event_callback(WM_MOUSEHOVER);

			::KillTimer(*this, IDT_MOUSEENTER);
		}
		break;
		case IDT_MOUSELEAVE:
		{
			g_frtc_mgr->mouse_event_callback(WM_MOUSELEAVE);

			::KillTimer(*this, IDT_MOUSELEAVE);
		}
		break;
		}
	}
	break;
	case WM_POWERBROADCAST:
	{
		DebugLog("Get WM_POWERBROADCAST, wparam=0x%x", wParam);
	}
	break;
	default:
		break;
	}

	if (message != WM_ERASEBKGND)
	{
		_frtc_mgr->process_sc_msg_queue_msg();
	}

	return DefWindowProc(hWnd, message, wParam, lParam);
}
