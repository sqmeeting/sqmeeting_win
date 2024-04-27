// frtc_sdk_api.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "frtccall_manager.h"
#include "FrtcMeetingWindow.h"
#include "resource.h"
#include "json/json.h"
#include "log.h"

#include <memory>

std::shared_ptr<FrtcManager> g_frtc_mgr;
extern HINSTANCE g_hInstance;
FrtcMeetingWindow* g_frtc_meeting_wnd = NULL;

void SdkSwitchMediaDeviceInMeeting(_FRTC_MEDIA_DEVICE_TYPE deviceType, const char* deviceID);

std::shared_ptr<CREATE_OPTIONS> g_create_options;
FrtcLayout g_layout;

BOOL MakeFRTCCall(const FrtcCallParam& params);
UINT32 CreateUI(HWND parentHwnd);

std::shared_ptr<FrtcManager> frtc_mgr_instance()
{
	if (g_frtc_mgr == NULL)
	{
		FrtcManager* pMgr = new FrtcManager();
		g_frtc_mgr.reset(pMgr);
	}
	return g_frtc_mgr;
}


BOOL CreateMainWindow(std::shared_ptr <CREATE_OPTIONS> create_options)
{
	if (g_frtc_meeting_wnd == NULL) 
    {
		g_frtc_meeting_wnd = new FrtcMeetingWindow(frtc_mgr_instance());
	}
	else
	{
		SetParent((HWND)*g_frtc_meeting_wnd, create_options->ui_host_hwnd);
	}

	RECT rc;
	int width, height, smallHeight;
	if (create_options->ui_host_hwnd != NULL)
	{
		::GetClientRect(create_options->ui_host_hwnd, &rc);

		int wv = rc.right - rc.left;
		int hv = (rc.bottom - rc.top) / 6 * 5;
		if (wv * 9 > hv * 16)
		{
			height = hv;
			width = height * 16 / 9;
		}
		else
		{
			width = wv;
			height = width * 9 / 16;
		}

		smallHeight = height / 5;
	}
	else
	{
		HWND   hDesk;
		hDesk = ::GetDesktopWindow();
		::GetWindowRect(hDesk, &rc);

		width = 1280;
		height = 720;
		smallHeight = 144;
	}

	InfoLog("start to create main window");

	DWORD dwStyle = (WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS);
	RECT rcSelf;
	rcSelf.left = (rc.right - rc.left - width) / 2;
	rcSelf.right = rc.left + width;
	rcSelf.top = 0;
	rcSelf.bottom = height + smallHeight;

	::AdjustWindowRectEx(&rcSelf, dwStyle, FALSE, 0);

	BOOL bret = FALSE;

    bret = g_frtc_meeting_wnd->Create(0,
                                      NULL,
                                      dwStyle,
                                      0,
                                      0,
                                      width,
                                      height + smallHeight,
                                      create_options->ui_host_hwnd,
                                      NULL,
                                      g_hInstance,
                                      FALSE);

	g_frtc_meeting_wnd->set_create_options(create_options);

	return bret;
}


void frtc_sdk_init(FrtcCallInitParam param)
{
	InfoLog("Start FRTCSDK");

	frtc_mgr_instance()->set_call_state_change_ui_callback(param.funcCallStateChangedCB);
	frtc_mgr_instance()->set_password_request_ui_callback(param.funcCallPasswordProcessCB);
	frtc_mgr_instance()->set_meeting_control_msg_callback(param.funcMeetingControlMsgCB);
	frtc_mgr_instance()->set_content_sending_state_callback(param.funcContentShareStateCB);
	frtc_mgr_instance()->set_reminder_notify_ui_callback(param.funcReminderNotifyCB);
	frtc_mgr_instance()->set_video_wnd_mouse_event_callback(param.funcWndMouseEventCB);

	frtc_mgr_instance()->init(param.uuid);
	InfoLog("FRTCSDK version %s started!", frtc_mgr_instance()->get_rtc_version());
}

void SdkShutdown()
{
	DebugLog("Shutdown FRTCSDK");
	frtc_mgr_instance()->uninit();
}

void frtc_sdk_ext_config(FRTC_SDK_EXT_CONFIG_KEY key, char* value)
{
	if (key == CONFIG_ENABLE_NOISE_BLOCKER)
	{
		std::string enabled(value);
		std::transform(enabled.begin(), enabled.end(), enabled.begin(),
			[](unsigned char c) { return tolower(c); });
		frtc_mgr_instance()->toggle_noise_block(enabled == "true");
	}
	else if (key == CONFIG_CAMERA_QUALITY_PREFERENCE)
	{
		frtc_mgr_instance()->set_camera_quality_preference(std::string(value));
	}
	else if (key == CONFIG_USE_SOFTWARE_RENDER)
	{
		std::string enabled(value);
		std::transform(enabled.begin(), enabled.end(), enabled.begin(),
			[](unsigned char c) { return tolower(c); });
		frtc_mgr_instance()->set_use_gdi_render(enabled == "true");
	}
	else if (key == CONFIG_MIRROR_LOCAL_VIDEO)
	{
		std::string mirror(value);
		std::transform(mirror.begin(), mirror.end(), mirror.begin(),
			[](unsigned char c) { return tolower(c); });
		frtc_mgr_instance()->toggle_camera_mirroring(mirror == "true");
	}
	else if (key == CONFIG_MICROPHONE_SHAREMODE)
	{
		std::string shareMode(value);
		std::transform(shareMode.begin(), shareMode.end(), shareMode.begin(),
			[](unsigned char c) { return tolower(c); });
		frtc_mgr_instance()->set_mic_share_mode(shareMode == "true");
	}
}

UINT32 frtc_parent_hwnd_set(HWND parant)
{
	return CreateUI(parant);
}

//Call
BOOL frtc_call_join(FrtcCallParam params)
{
	return MakeFRTCCall(params);
}

BOOL MakeFRTCCall(const FrtcCallParam& params)
{
	FRTC_CALL_STATE currentState = frtc_mgr_instance()->get_call_state();
	if (currentState == FRTC_CALL_STATE::CALL_CONNECTING || currentState == FRTC_CALL_STATE::CALL_CONNECTED)
	{
		if (frtc_mgr_instance()->get_reconnect_state() == ReconnectState::RECONNECT_FAILED)//means auto re-connect failed
		{
			frtc_mgr_instance()->reconnect_current_meeting(*frtc_mgr_instance()->get_lastcall_param());
			return TRUE;
		}
		return FALSE;
	}

	Gdiplus::GdiplusStartupInput gdiplusStartupInput;
	ULONG_PTR gdiplusToken;
	Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

	if (g_frtc_meeting_wnd == NULL) {
		g_frtc_meeting_wnd = new FrtcMeetingWindow(frtc_mgr_instance());
	}
	else
	{
		g_frtc_meeting_wnd->init_main_window(frtc_mgr_instance());
	}

	if (!g_create_options)
	{
		g_create_options.reset(new CREATE_OPTIONS());
	}

	g_create_options->video_state = params.muteVideo;
	g_create_options->audio_state = params.muteAudio;
	g_create_options->ui_host_hwnd = NULL;
	g_layout = params.layout;
	g_frtc_meeting_wnd->update_creat_options(g_create_options);
	frtc_mgr_instance()->set_create_options(g_create_options);
	frtc_mgr_instance()->clear_main_window();
	frtc_mgr_instance()->join_meeting(params);

	if (g_layout == FrtcLayout::LAYOUT_GALLERY)
	{
		frtc_mgr_instance()->set_layout_grid_mode(true);
	}
	else
	{
		frtc_mgr_instance()->set_layout_grid_mode(false);
	}

	return TRUE;
}

UINT32 CreateUI(HWND parentHwnd)
{
	FRTC_CALL_STATE currentState = frtc_mgr_instance()->get_call_state();
	if (currentState == FRTC_CALL_STATE::CALL_CONNECTING)
	{
		return 0;
	}

	g_create_options->ui_host_hwnd = parentHwnd;
	CreateMainWindow(g_create_options);

	if (!frtc_mgr_instance()->initialized())
	{
		ErrorLog("SDK not initialized!!!");
		return 0;
	}
	else
	{
        DebugLog("CreateUI, reset main window");

		frtc_mgr_instance()->reset_main_window((HWND)*g_frtc_meeting_wnd, g_hInstance);
	}

	HWND hMain = (HWND)*g_frtc_meeting_wnd;

	return reinterpret_cast<UINT32>(hMain);
}

void frtc_call_leave()
{
	DebugLog("drop call from UI");

	if (frtc_mgr_instance()->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTED
		|| frtc_mgr_instance()->get_reconnect_state() == ReconnectState::RECONNECT_FAILED)
	{
		frtc_mgr_instance()->cancel_next_reconnect();
		frtc_mgr_instance()->OnMeetingStatusChange(RTC::MeetingStatus::kDisconnected,
			RTC::MeetingStatusChangeReason::kAborted, "");
	}
	else if (frtc_mgr_instance()->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTED
		&& frtc_mgr_instance()->get_reconnect_state() == ReconnectState::RECONNECT_TRYING)
	{
		frtc_mgr_instance()->cancel_next_reconnect();
		frtc_mgr_instance()->OnMeetingStatusChange(RTC::MeetingStatus::kDisconnected,
			RTC::MeetingStatusChangeReason::kAborted, "");
	}
	else
	{
		if (g_frtc_meeting_wnd != NULL)
		{
			g_frtc_meeting_wnd->close_manually();
		}
	}
}

const char* frtc_media_device_get(FRTC_MEDIA_DEVICE_TYPE deviceType)
{
	switch (deviceType)
	{
	case MEDIA_DEVICE_CAMERA:
		return frtc_mgr_instance()->get_camera_device_list_jstr().c_str();
		break;
	case MEDIA_DEVICE_MIC:
		return frtc_mgr_instance()->get_microphone_device_list_jstr().c_str();
		break;
	case MEDIA_DEVICE_SPEAKER:
		return frtc_mgr_instance()->get_speaker_device_list_jstr().c_str();
		break;
	default:
		break;
	}
	return "";
}

void frtc_media_device_set(FRTC_MEDIA_DEVICE_TYPE deviceType, const char* deviceID)
{
	DebugLog("set device type %d to new device %s", deviceType, deviceID);
	if (deviceType != FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_CAMERA && strcmp(deviceID, "os_default") == 0)
	{
		return frtc_mgr_instance()->set_audio_device_os_default(deviceType, true);
	}

	if (frtc_mgr_instance()->get_call_state() == FRTC_CALL_STATE::CALL_CONNECTED)
	{
		return SdkSwitchMediaDeviceInMeeting(deviceType, deviceID);
	}
	else if (deviceID && strcmp(deviceID, ""))
	{
		DebugLog("set device type %d to non-default device %s", deviceType, deviceID);
		std::wstring wstrDeviceID = FRTCSDK::FRTCSdkUtil::string_to_wstring(deviceID);
		switch (deviceType)
		{
		case MEDIA_DEVICE_CAMERA:
			frtc_mgr_instance()->set_camera_device(wstrDeviceID.c_str());
			break;
		case MEDIA_DEVICE_MIC:
			frtc_mgr_instance()->set_audio_device_os_default(deviceType, false);
			frtc_mgr_instance()->set_microphone_device(wstrDeviceID.c_str());
			break;
		case MEDIA_DEVICE_SPEAKER:
			frtc_mgr_instance()->set_audio_device_os_default(deviceType, false);
			frtc_mgr_instance()->set_speaker_device(wstrDeviceID.c_str());
			break;
		default:
			break;
		}
	}
}

void frtc_start_mic_test()
{
	frtc_mgr_instance()->start_mic_test();
}

void frtc_stop_mic_test()
{
	frtc_mgr_instance()->stop_mic_test();
}

float frtc_get_mic_peak_meter()
{
	return frtc_mgr_instance()->get_mic_peak_meter();
}

void frtc_media_device_get_current_used(_FRTC_MEDIA_DEVICE_TYPE deviceType, OUT char* deviceName, int* len)
{
	switch (deviceType)
	{
	case MEDIA_DEVICE_CAMERA:
	{
		const TCHAR* name = frtc_mgr_instance()->get_current_camera_device_name();
		if (!name)
		{
			name = frtc_mgr_instance()->get_preferred_camera_device_name();
		}
		std::string& camera = FRTCSDK::FRTCSdkUtil::wstring_to_string(name);
		if (deviceName && *len >= camera.length())
			::strcpy(deviceName, camera.c_str());
		*len = camera.length();
		break;
	}
	case MEDIA_DEVICE_MIC:
	{
		const TCHAR* name = frtc_mgr_instance()->get_current_mic_device_name();
		std::string strName = FRTCSDK::FRTCSdkUtil::wstring_to_string(name);
		if (deviceName && *len >= strName.length())
			::strcpy(deviceName, strName.c_str());
		*len = strName.length();
		break;
	}
	case MEDIA_DEVICE_SPEAKER:
	{
		const TCHAR* name = frtc_mgr_instance()->get_current_speaker_device_name();
		std::string strName = FRTCSDK::FRTCSdkUtil::wstring_to_string(name);
		if (deviceName && *len >= strName.length())
			::strcpy(deviceName, strName.c_str());
		*len = strName.length();
		break;
	}
	default:
		break;
	}
}

void SdkSwitchMediaDeviceInMeeting(_FRTC_MEDIA_DEVICE_TYPE deviceType, const char* deviceID)
{
	ErrorLog("Switch device type %d to new device %s", deviceType, deviceID);
	if (deviceID && strcmp(deviceID, ""))
	{
		size_t WLength = MultiByteToWideChar(CP_UTF8, 0, deviceID, -1, NULL, NULL);
		if (WLength == 1 * sizeof(WCHAR))
		{
			ErrorLog("Get device ID error, id is %s", deviceID);
		}
		LPWSTR pszW = (LPWSTR)_alloca((WLength + 1) * sizeof(WCHAR)); //do not need free
		MultiByteToWideChar(CP_UTF8, 0, deviceID, -1, pszW, WLength);
		switch (deviceType)
		{
		case MEDIA_DEVICE_CAMERA:
		{
			if (!g_frtc_mgr->get_current_camera_device() || 0 != wcscmp(g_frtc_mgr->get_current_camera_device(), pszW))
			{
				const video_device_list& cameras = frtc_mgr_instance()->get_camera_devices(TRUE);
				video_device_list::const_iterator it = cameras.begin();
				while (it != cameras.end())
				{
					if (wcscmp(pszW, (*it)->id) == 0)
					{
						g_frtc_mgr->set_camera_device((*it)->id);
						if (!g_frtc_mgr->is_local_video_muted())
						{
							g_frtc_mgr->reset_camera_device((*it));

							std::string localID = "__local_preview_msid__";
							g_frtc_mgr->get_local_preview_msid(localID);
							g_frtc_mgr->clear_receive_video(localID);

							ErrorLog("clearReceiveVideo ");
							g_frtc_mgr->set_video_data_zero_check_timer();
						}
						break;
					}
					++it;
				}
			}
			break;
		}
		case MEDIA_DEVICE_MIC:
		{
			frtc_mgr_instance()->set_audio_device_os_default(MEDIA_DEVICE_MIC, false);
			GUID guid{ 0 };
			CLSIDFromString(pszW, &guid);
			frtc_mgr_instance()->reset_mic_device(guid);
			break;
		}
		case MEDIA_DEVICE_SPEAKER:
		{
			frtc_mgr_instance()->set_audio_device_os_default(MEDIA_DEVICE_SPEAKER, false);
			GUID guid{ 0 };
			CLSIDFromString(pszW, &guid);
			frtc_mgr_instance()->reset_speaker_device(guid);
			break;
		}
		default:
			break;
		}
	}
}

//Misc
const char* frtc_sdk_version_get()
{
	return frtc_mgr_instance()->get_rtc_version();
}

const char* frtc_statistics_collect()
{
	return frtc_mgr_instance()->get_meeting_statistics();
}

const BOOL frtc_audio_mute_check()
{
	return frtc_mgr_instance()->is_local_audio_muted();
}

void frtc_window_content_share(int hwnd)
{
	frtc_mgr_instance()->start_send_window_content((HWND)hwnd);
}

static BOOL __stdcall EnumMonitor(HMONITOR hMonitor, HDC hDC, RECT* pRect, LPARAM pData);
static BOOL __stdcall EnumMonitor(HMONITOR hMonitor, HDC hDC, RECT* pRect, LPARAM pData)
{
	std::vector <std::string>* pszMonNameList = (std::vector <std::string>*)pData;
	MONITORINFOEXA mi;
	::memset(&mi, 0, sizeof(mi));

	mi.cbSize = sizeof(mi);
	BOOL res = GetMonitorInfoA(hMonitor, &mi);

	std::string monitorName(mi.szDevice);
	pszMonNameList->push_back(monitorName);
	return TRUE;
}

void frtc_desktop_share(int monitorIndex, bool withContentAudio)
{
	DebugLog("frtc_desktop_share  withContentAudio:%d monitorIndex:%d", withContentAudio, monitorIndex);
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		std::vector<FRTCSDK::DISPLAY_MONITOR_INFO> monitors;
		FRTCSDK::FRTCSdkUtil::get_monitor_list(monitors);
		std::vector<FRTCSDK::DISPLAY_MONITOR_INFO>::iterator monitor = monitors.begin();
		for (; monitor != monitors.end(); monitor++)
		{
			FRTCSDK::DISPLAY_MONITOR_INFO dis = *monitor;
			if (dis.idx == monitorIndex)
			{
				std::wstring name = dis.device_name;
				frtc_mgr_instance()->set_share_content_with_audio(withContentAudio);
				frtc_mgr_instance()->start_send_content(name, monitorIndex);
				break;
			}
		}
	}

	DebugLog("frtc_desktop_share  withContentAudio:%d monitorIndex:%d end", withContentAudio, monitorIndex);
}

void frtc_window_share(int hwnd, bool withContentAudio)
{

	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		frtc_mgr_instance()->set_share_content_with_audio(withContentAudio);
		frtc_mgr_instance()->start_send_content(hwnd);
	}
}

void frtc_content_stop()
{
    FRTC_CALL_STATE status = frtc_mgr_instance()->get_call_state(); 
	InfoLog("sdk frtc content stop, status=%d", status);

	if (FRTC_CALL_STATE::CALL_CONNECTED == status)
	{
		frtc_mgr_instance()->stop_sending_content();
	}
}

void frtc_local_audio_mute(bool mute)
{
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		frtc_mgr_instance()->toggle_local_audio_mute(mute);
	}
	else
	{
		frtc_mgr_instance()->set_mute_state(mute);
	}
}

void frtc_local_video_start()
{
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		frtc_mgr_instance()->toggle_local_video_mute(false);
	}
}

void frtc_local_video_stop()
{
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		frtc_mgr_instance()->toggle_local_video_mute(true);
	}
}

void frtc_full_screen_switch(bool fullScreen)
{
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		if (g_frtc_meeting_wnd != NULL)
		{
			g_frtc_meeting_wnd->toggle_full_screen(fullScreen);
		}
	}
}

void frtc_layout_config(FrtcLayout layout)
{
	if (frtc_mgr_instance()->get_call_state() == FRTC_CALL_STATE::CALL_CONNECTED)
	{
		switch (layout)
		{
		case FrtcLayout::LAYOUT_AUTO:
			frtc_mgr_instance()->set_layout_grid_mode(false);
			break;
		case FrtcLayout::LAYOUT_GALLERY:
			frtc_mgr_instance()->set_layout_grid_mode(true);
			break;
		default:
			break;
		}
	}
}

void frtc_local_preview_hide(bool hide)
{
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		frtc_mgr_instance()->toggle_local_video_wnd_hide(hide);
	}
}

const char* frtc_participants_collect()//JSON string
{
	if (FRTC_CALL_STATE::CALL_CONNECTED == frtc_mgr_instance()->get_call_state())
	{
		return frtc_mgr_instance()->get_participant_list_jstr().c_str();
	}
	else
	{
		return NULL;
	}
}

void SdksetMeetingControlMsgCallback(MeetingControlMsgCB callback)//JSON string
{
	frtc_mgr_instance()->set_meeting_control_msg_callback(callback);
}

void frtc_call_password_send(const char* passcode)
{
	frtc_mgr_instance()->verify_password(passcode);
}

FRTC_CALL_STATE frtc_call_state_get()
{
	return frtc_mgr_instance()->get_call_state();
}

bool frtc_content_state_get()
{
	return frtc_mgr_instance()->is_sending_content();
}

const char* frtc_monitors_collect()
{
	return frtc_mgr_instance()->get_monitor_list();
}

const char* frtc_windows_collect()
{
	return frtc_mgr_instance()->get_window_list();
}

const BOOL frtc_local_preview_hide_check()
{
	return frtc_mgr_instance()->is_local_video_wnd_hidden();
}

const BOOL frtc_layout_config_enabled_check()
{
	return frtc_mgr_instance()->is_switch_layout_enabled();
}

void frtc_name_card_switch(bool bVisible)
{
	return frtc_mgr_instance()->toggle_name_card_visible(bVisible);
}

long long frtc_logs_upload_start(const char* metaData, const char* fileName, int count, const char* coreDumpName)
{
	return frtc_mgr_instance()->start_upload_logs(std::string(metaData), std::string(fileName), count, coreDumpName);
}

int frtc_logs_upload_status_query(long long tractionId, int fileType, int* speed)
{
	return frtc_mgr_instance()->get_log_update_status(tractionId, fileType, speed);
}

void frtc_logs_upload_cancel(long long tractionId)
{
	frtc_mgr_instance()->cancel_log_update(tractionId);
}
