#include "stdafx.h"
#include <codecvt>
#include <future>

#include <SetupAPI.h>

#include "log.h"
#include "FrtcMeetingWindow.h"
#include "frtccall_manager.h"


#define WM_FRTC_CALLNOTIFICATION WM_USER + 1
#define WM_FRTC_SIGNINFUNCCALL WM_USER + 1000

extern HINSTANCE g_hInstance;
extern FrtcMeetingWindow* g_frtc_meeting_wnd;

const std::string kContentMSIDPrefixStr = "VCR";

FrtcManager::FrtcManager(void)
	: _rtc_event_observer(nullptr),
	_hMain(nullptr),
	_is_local_video_muted(false),
	_is_local_audio_muted(false),
	_is_sharing_content(false),
	_call_state(CALL_DISCONNECTED),
	_is_sending_audio(false),
	_is_sending_content(false),
	_uuid(""),
	_statistics_report_str(),
	_incall_window_title("SQMeeting_CE"),
	_signed_user_display_name(""),
	_is_guest_call(false),
	_noise_blocker_enabled(true),
	_media_convert_buffer(nullptr),
	_media_convert_buffer_size(0),
	_max_camera_height(0),
	_camera_resolution_list(),
	_msid_to_resolution_map(),
	_camera_resolution_height(0),
	_is_restart_camera_timer_set(false),
	_restart_camera_failed_count(0),
	_meeting_control_msg_callback(nullptr),
	_content_sending_state_callback(nullptr),
	_is_media_encrypted(false),
	_ui_reminder_notify_callback(nullptr),
	_ui_window_mouse_event_callback(nullptr),
	_show_name_card(true),
	_send_content_audio(false),
	_want_send_content_audio(false),
	_is_audio_muted_by_server(false),
	_allow_unmute_audio_by_self(true),
	_local_camera_muted_pic_data_ptr(nullptr),
	_share_window_paused_pic_data_ptr(nullptr),
	_content_paused(false),
	_content_audio_capture(nullptr),
	_using_os_default_mic_device(true),
	_using_os_default_speaker_device(true),
	_is_window_content(false),
	_use_gdi_render(false),
	_audio_render(std::make_unique<AudioRender>()),
	_default_system_speaker_guid({ 0 }),
	_video_wnd_mgr(std::make_unique<FRTCVideoWndMgr>()),
	_name_card_display_begin_time(0),
	_people_audio_monitor(),
	reconnectHelper_(std::make_unique<ReconnectHelper>()),
	_frtc_content_capturer(std::make_unique<frtc_content_capturer>())
{
	// Initialize MicCapture objects
	_people_audio_capture = new PeopleAudioCapture();

	init_content_audio_device();

	_status = S_CREATE;

	FRTCSDK::FRTCSdkUtil::init_lookup_table();
	FRTCSDK::FRTCSdkUtil::init_convert_table_yuv420_rgb();

	load_camera_muted_pic();
	load_share_window_paused_pic();
	clean_sc_msg_queue();

	_people_audio_monitor.SetAudioSink(this);
	HRESULT hr = _people_audio_monitor.StartMonitor();
	if (FAILED(hr))
	{
		WarnLog("Start audio device monitor failed 0x%x", hr);
	}
}

FrtcManager::~FrtcManager(void)
{
	_people_audio_monitor.StopMonitor();

	if (_content_audio_capture)
	{
		delete _content_audio_capture;
		_content_audio_capture = nullptr;
	}

	if (_people_audio_capture)
	{
		delete _people_audio_capture;
		_people_audio_capture = nullptr;
	}

	_default_system_speaker_guid = { 0 };

	if (_media_convert_buffer)
	{
		delete[] _media_convert_buffer;
		_media_convert_buffer = nullptr;
		_media_convert_buffer_size = 0;
	}

	_camera_resolution_list.clear();
	_msid_to_resolution_map.clear();

	if (_local_camera_muted_pic_data_ptr)
	{
		delete _local_camera_muted_pic_data_ptr;
		_local_camera_muted_pic_data_ptr = nullptr;
	}
	if (_share_window_paused_pic_data_ptr)
	{
		delete _share_window_paused_pic_data_ptr;
		_share_window_paused_pic_data_ptr = nullptr;
	}
}

BOOL FrtcManager::init(const std::string& uuid)
{
	if (_status == S_CREATE)
	{
		_status = S_INIT;
		_uuid = uuid;

		if (!_rtc_event_observer)
		{
			_rtc_event_observer = new RTCEventObserver(this);
		}

		RTC::RTCInitParam rtcInitParam;
		rtcInitParam.log_path = "frtc_sdk.log";
		rtcInitParam.uuid = _uuid;
		_rtcInstance = RTC::RTCInterface::InitRTC(_rtc_event_observer, rtcInitParam);

		InitLog(_rtcInstance);

		_rtcInstance->SetFeatureEnable(RTC::kAEC, true);

		set_cpu_level_to_dshow_capture();

		_is_processing_sc_msg = true;
		_video_wnd_mgr->set_handle_layout_change(_is_processing_sc_msg);
	}

	return TRUE;
}

BOOL FrtcManager::uninit()
{
	if (_status == S_STARTED)
	{
		stop_meeting_audio();
		stop_meeting_video();
	}

	_video_wnd_mgr->remove_main_window();
	delete _rtc_event_observer;
	_status = S_DESTROY;

	return TRUE;
}

const char* FrtcManager::get_rtc_version()
{
	_sdk_version_str = _rtcInstance->GetVersion();
	return _sdk_version_str.c_str();
}

void FrtcManager::join_meeting(const FrtcCallParam& param)
{
	_call_state = CALL_CONNECTING;
	_is_guest_call = false;
	_is_processing_sc_msg = true;
	_video_wnd_mgr->set_handle_layout_change(_is_processing_sc_msg);
	_audio_only_list.clear();
	_layout_items_list.clear();
	_is_sharing_content = false;
	_is_audio_muted_by_server = false;
	_allow_unmute_audio_by_self = true;

	if (g_frtc_meeting_wnd)
	{
		g_frtc_meeting_wnd->set_audio_only_mode(param.isAudioOnly);
	}
	_is_audio_only = param.isAudioOnly;

	std::string meeting_pwd = param._meeting_pwd ? param._meeting_pwd : "";
	int call_rate = param.isAudioOnly ? 64 : (param._call_rate ? atoi(param._call_rate) : 0);
	_is_local_audio_muted = param.muteAudio;
	_is_local_video_muted = param.muteVideo;
	_confrence_alias = std::string(param.callNumber, param.callNumber + strlen(param.callNumber));

	if (param._user_token && param._user_token[0] != '\0')
	{
		join_meeting_logged_in(param._server_address, param.callNumber, param._display_name, param._user_token, meeting_pwd, call_rate);
	}
	else
	{
		join_meeting_guest(param._server_address, param.callNumber, param._display_name, call_rate, meeting_pwd);
	}

	reconnectHelper_->SetLastCallParam(param);
}

void FrtcManager::join_meeting_guest(const std::string& _server_address, const std::string& _meeting_number, const std::string& _user_display_name, int _call_rate, const std::string& _meeting_pwd)
{
	_guest_call_param = { _server_address, _meeting_number, _user_display_name, _meeting_pwd, _call_rate };
	_signed_call_param._user_token = "";
	_is_guest_call = true;
	_guest_user_display_name = _user_display_name;

	std::async(std::launch::async, make_call_thread_proc, this);
	_status = S_STARTED;
}

void FrtcManager::join_meeting_logged_in(const std::string& _server_address, const std::string& conferenceAlias, const std::string& userName, const std::string& _user_token, const std::string& _meeting_pwd, int _call_rate)
{
	_signed_call_param = { _server_address, conferenceAlias, userName, _user_token, _meeting_pwd, _call_rate };
	_signed_user_display_name = userName;
	_is_guest_call = false;
	std::async(std::launch::async, make_call_thread_proc, this);

	_status = S_STARTED;
}

void FrtcManager::end_meeting(int callIndex)
{
	_is_processing_sc_msg = false;
	_video_wnd_mgr->set_handle_layout_change(_is_processing_sc_msg);

	if (_call_state != CALL_DISCONNECTED)
		_call_state = CALL_DISCONNECTING;

	stop_meeting_audio();
	stop_meeting_video();
	_video_wnd_mgr->remove_main_window();
	clear_params();
	clear_name_card();
	_rtcInstance->EndMeeting(callIndex);

	_camera_resolution_list.clear();
	_msid_to_resolution_map.clear();
}

void FrtcManager::reconnect_current_meeting(const FrtcCallParam& param)
{
	DebugLog("reconnect current meeting");
	if (_is_sharing_content)
	{
		stop_sending_content();
		FRTC_SDK_CALL_NOTIFICATION callNotify;
		callNotify._target_id = "stopSharing";
		callNotify._msg_name = "ContentStatusChanged";
		OnCallNotification(callNotify);
	}

	int call_rate = 0;
	_is_processing_sc_msg = true;
	_video_wnd_mgr->set_handle_layout_change(_is_processing_sc_msg);
	_is_audio_only = param.isAudioOnly;
	if (g_frtc_meeting_wnd)
		g_frtc_meeting_wnd->set_audio_only_mode(param.isAudioOnly);

	std::string meeting_pwd = param._meeting_pwd ? param._meeting_pwd : "";
	call_rate = param.isAudioOnly ? 64 : (param._call_rate ? atoi(param._call_rate) : 0);
	_confrence_alias = std::string(param.callNumber, param.callNumber + strlen(param.callNumber));

	if (param._user_token && param._user_token[0] != '\0')
	{
		join_meeting_logged_in(param._server_address, param.callNumber, param._display_name, param._user_token, meeting_pwd, call_rate);
	}
	else
	{
		join_meeting_guest(param._server_address, param.callNumber, param._display_name, call_rate, meeting_pwd);
	}
}

void FrtcManager::notify_meeting_reconnect_state(ReconnectState state)
{
	Json::Value jsonState;
	jsonState["state"] = (int)state;
	OnMeetingControlMsg("reconnect_state", jsonState);
}

void FrtcManager::verify_password(const char* passCode)
{
	reconnectHelper_->SetLastCallPwdForGuest(passCode);
	_rtcInstance->VerifyPasscode(std::string(passCode));
}

bool FrtcManager::is_audio_only()
{
	return _is_audio_only;
}

bool FrtcManager::is_local_video_muted()
{
	return _is_local_video_muted;
}

bool FrtcManager::is_local_audio_muted()
{
	return _is_local_audio_muted;
}

bool FrtcManager::is_local_only()
{
	return _video_wnd_mgr->is_local_only();
}

void FrtcManager::set_layout_grid_mode(bool gridMode)
{
	_is_grid_mode = gridMode;

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "SetGridLayoutMode";
	callNotify._is_grid_layout_mode = _is_grid_mode;
	OnCallNotification(callNotify);

	_rtcInstance->SetLayoutGridMode(_is_grid_mode);
}

void FrtcManager::set_mute_state(bool mute)
{
	InfoLog("set audio mute state %d", mute);
	_is_local_audio_muted = mute;
}

void FrtcManager::toggle_camera_mirroring(bool mirror)
{
	_mirror_local_video = mirror;
	_rtcInstance->SetCameraMirror(mirror);
}

void FrtcManager::toggle_local_audio_mute(bool mute)
{
	InfoLog("toggle loca audio mute %d", mute);
	_is_local_audio_muted = mute;
	_rtcInstance->MuteLocalAudio(mute);

	if (!mute)
		_is_audio_muted_by_server = false;

	_send_content_audio = _want_send_content_audio && !mute;

	if (mute && !_is_audio_muted_by_server)
	{
		_send_content_audio = false;
	}

	if (_is_sending_content && _want_send_content_audio)
	{
		bool isSameDev = is_same_speaker_device();
		toggle_content_auido(_send_content_audio, isSameDev);
	}

	report_mute_status();

	std::string strLocalSourceID;
	get_local_preview_msid(strLocalSourceID);
	_video_wnd_mgr->toggle_audio_state_icon_window_show(strLocalSourceID, _is_local_audio_muted);
}

void FrtcManager::toggle_local_video_mute(bool mute)
{
	_is_local_video_muted = mute;
	if (mute)
	{
		stop_camera(true);
	}
	else
	{
		start_camera();
	}
	_rtcInstance->MuteLocalVideo(mute);
	update_self_video_status();
}

void FrtcManager::toggle_local_video_wnd_hide(bool hide)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "SetLocalVideoHideState";
	callNotify._is_hide_local_video = hide;
	OnCallNotification(callNotify);
}

void FrtcManager::toggle_noise_block(bool enable)
{
	DebugLog("noise blocker enable=%s", enable ? "true" : "false");
	_noise_blocker_enabled = enable;
}

void FrtcManager::toggle_content_auido(bool enable, bool isSameDevice)
{
	DebugLog("enable content audio=%s, same device to speaker=%s",
		enable ? "true" : "false",
		isSameDevice ? "true" : "false");

	_rtcInstance->SetContentAudio(enable, isSameDevice);

	Json::Value jsonContentAudio;
	jsonContentAudio["sendContentAudio"] = enable;
	OnMeetingControlMsg("on_send_content_audio_changed", jsonContentAudio);
}

void FrtcManager::OnDetectAudioMute()
{
	set_reminder_type(kAudioMuteDetect);
}

void FrtcManager::OnCallNotification(FRTC_SDK_CALL_NOTIFICATION& callNotify)
{
	if ((_hMain && _call_state != FRTC_CALL_STATE::CALL_DISCONNECTED) ||
		callNotify._msg_name == "CallDisconnected")
	{
		AutoLock autoLock(_msg_queue_lock);

		while (!_sc_msg_queue_temp.empty())
		{
			FRTC_SDK_CALL_NOTIFICATION notify = _sc_msg_queue_temp.front();
			_sc_msg_queue.push(notify);

			DebugLog("pop sc msg from temp queue and push them to queue, msg=%s",
				notify._msg_name.c_str());

			_sc_msg_queue_temp.pop();
		}

		if (callNotify._msg_name != "SendQueue")
		{
			_sc_msg_queue.push(callNotify);
		}

		DebugLog("push sc msg to queue, msg=%s", callNotify._msg_name.c_str());

		if (callNotify._msg_name != "CallDisconnected")
		{
			PostMessage(_hMain, WM_FRTC_CALLNOTIFICATION, 0, NULL);
		}
	}
	else if (_hMain == NULL)
	{
		AutoLock autoLock(_msg_queue_lock);
		clean_sc_msg_queue();

		DebugLog("push sc msg to temp queue, msg=%s", callNotify._msg_name.c_str());

		_sc_msg_queue_temp.push(callNotify);
	}
}

void FrtcManager::OnContentTokenResponse(bool rejected)
{
	DebugLog("share content is %s by server.", rejected ? "rejected" : "accepted");

	if (rejected)
	{
		clear_content_sending_params();
		set_reminder_type(kContentShareNoPermission);
	}
}

void FrtcManager::OnContentFailForLowBandwidth()
{
	clear_content_sending_params();
	set_reminder_type(kUplinkBitRateLimit);
}

void FrtcManager::OnLayoutSetting(int max_cell_count, const std::vector<std::string>& lectures)
{
	_video_wnd_mgr->set_layout_cell_max_count(max_cell_count);
	_video_wnd_mgr->set_current_lecture(lectures.empty() ? "" : lectures.front());
}

void FrtcManager::OnMeetingControlMsg(const std::string& msgType, const Json::Value& jsonContext)
{
	if (_meeting_control_msg_callback)
	{
		Json::Value jsonMsg;
		jsonMsg["message_type"] = msgType;
		jsonMsg["message_context"] = jsonContext;
		Json::Value root;
		root["meeting_controll_msg"] = jsonMsg;

		Json::FastWriter writer;
		const std::string json_file = writer.write(root);

		_meeting_control_msg_callback(json_file.c_str());
	}
}

void FrtcManager::OnMeetingJoinFail(RTC::MeetingStatusChangeReason state)
{
	_is_guest_call = false;
	OnMeetingStatusChange(RTC::MeetingStatus::kDisconnected, (int)state, "");
}

void FrtcManager::OnMeetingJoinInfo(const std::shared_ptr<MeetingInfo>& meetingInfo)
{
	_meeting_id = meetingInfo->meeting_id;
	_meeting_name = meetingInfo->meeting_name;
	_meeting_owner_id = meetingInfo->owner_id;
	_meeting_owner_name = meetingInfo->owner_name;

	std::string strLocalSourceID;
	get_local_preview_msid(strLocalSourceID);
	_video_wnd_mgr->toggle_audio_state_icon_window_show(strLocalSourceID, _is_local_audio_muted);

	if (g_frtc_meeting_wnd != nullptr)
	{
		Json::Value jsonMeetingInfo;
		jsonMeetingInfo["meeting_name"] = _meeting_name;
		jsonMeetingInfo["meeting_id"] = _meeting_id;
		jsonMeetingInfo["display_name"] = meetingInfo->display_name;
		jsonMeetingInfo["meeting_owner_id"] = _meeting_owner_id;
		jsonMeetingInfo["meeting_owner_name"] = _meeting_owner_name;
		jsonMeetingInfo["meeting_url"] = meetingInfo->meeting_url;
		jsonMeetingInfo["group_meeting_url"] = meetingInfo->group_meeting_url;
		jsonMeetingInfo["schedule_start_time"] = meetingInfo->start_time;
		jsonMeetingInfo["schedule_end_time"] = meetingInfo->end_time;
		jsonMeetingInfo["is_guest"] = _is_guest_call;

		OnMeetingControlMsg("on_meeting_info", jsonMeetingInfo);
	}
}

void FrtcManager::OnMeetingSessionStatus(
	const std::string& watermark_msg,
	const std::string& recording_status,
	const std::string& streaming_status,
	const std::string& streaming_url,
	const std::string& streaming_pwd)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "RecordingStreamingStatusChanged";
	PFRTC_WATERMARK_PARAM waterMarkNotify = new FRTC_WATERMARK_PARAM();

	Json::Value jsonMsg;
	Json::Reader reader;
	reader.parse(watermark_msg, jsonMsg);
	bool enable = jsonMsg["enable"].asBool();

	std::string watermarkStr;
	if (!_is_guest_call)
	{
		watermarkStr = _signed_user_display_name;
	}
	else
	{
		watermarkStr = _guest_user_display_name;
	}

	int newWidth = 0;
	get_watermark_yuv_data(watermarkStr, 1920, 1080, newWidth);

	waterMarkNotify->_water_mark_str = watermarkStr;
	waterMarkNotify->_enable_water_mark = enable;
	waterMarkNotify->_recording_status_str = recording_status;
	waterMarkNotify->_live_status_str = streaming_status;
	waterMarkNotify->_live_meeting_url = streaming_url;
	waterMarkNotify->_live_meeting_pwd = streaming_pwd;
	callNotify._param = waterMarkNotify;
	OnCallNotification(callNotify);
}

void FrtcManager::OnMeetingStatusChange(RTC::MeetingStatus state, int reason, const std::string& callId)
{
	DebugLog("meeting status changed, status=%d, reason=%d, callId=%s",
		static_cast<int>(state), reason, callId.c_str());

	ReconnectState lastState = reconnectHelper_->GetReconnectState();
	ReconnectState reconnState = reconnectHelper_->HandleCallStateChange(state,
		static_cast<RTC::MeetingStatusChangeReason>(reason));

	DebugLog("lastReconnectState=%d, reconnectState=%d", lastState, reconnState);

	if (reconnState != ReconnectState::RECONNECT_IDLE &&
		!((lastState == reconnState) && reconnState == ReconnectState::RECONNECT_FAILED) &&
		!((lastState == reconnState) && reconnState == ReconnectState::RECONNECT_SUCCESS))
	{
		notify_meeting_reconnect_state(reconnState);

		if (reconnState == ReconnectState::RECONNECT_SUCCESS)
		{
			reconnectHelper_->ResetReconnectStatue();
			_call_state = FRTC_CALL_STATE::CALL_CONNECTED;
			_full_rosters_list.clear();
			FRTC_SDK_CALL_NOTIFICATION pNotify;
			pNotify._msg_name = "CallReconnected";
			OnCallNotification(pNotify);
		}
	}
	else if (lastState == reconnState &&
		reconnState == ReconnectState::RECONNECT_FAILED &&
		reason == RTC::MeetingStatusChangeReason::kAborted)
	{
		return;
	}
	else
	{
		reconnectHelper_->ResetReconnectStatue();

		if (state == RTC::MeetingStatus::kConnected)
		{
			if (_call_state == FRTC_CALL_STATE::CALL_CONNECTED ||
				_call_state == FRTC_CALL_STATE::CALL_DISCONNECTING)
			{
				WarnLog("connected msg received while already connected or disconnecting");
				return;
			}

			_call_state = FRTC_CALL_STATE::CALL_CONNECTED;

			FRTC_SDK_CALL_NOTIFICATION pNotify;
			pNotify._msg_name = "CallConnected";
			OnCallNotification(pNotify);
		}
		else if (state == RTC::MeetingStatus::kDisconnected)
		{
			_call_state = FRTC_CALL_STATE::CALL_DISCONNECTED;
			_full_rosters_list.clear();
			_is_guest_call = false;
			_hMain = NULL;

			FRTC_SDK_CALL_NOTIFICATION pNotify;
			pNotify._msg_name = "CallDisconnected";
			OnCallNotification(pNotify);
		}

		if (_ui_call_state_change_callback)
		{
			DebugLog("call status changed, status=%d, meeting id=%s, meeting name=%s",
				_call_state, _meeting_id.c_str(), _meeting_name.c_str());

			_ui_call_state_change_callback(
				static_cast<int>(_call_state),
				map_rtcsdk_state_reason_to_frtc_call_reason(static_cast<RTC::MeetingStatusChangeReason>(reason)),
				_meeting_id.c_str(),
				_meeting_name.c_str());
		}
	}
}

void FrtcManager::OnMuteLock(bool muted, bool allowSelfUnmute)
{
	InfoLog("got mute msg from server, mute=%d, allow self unmute=%d",
		muted, allowSelfUnmute);

	_is_audio_muted_by_server = muted;
	_allow_unmute_audio_by_self = allowSelfUnmute;

	if (muted)
	{
		InfoLog("muted from server,do mute");
		toggle_local_audio_mute(muted);
	}

	if (g_frtc_meeting_wnd != NULL)
	{
		Json::Value jsonMsg;
		jsonMsg["muted"] = muted;
		jsonMsg["allowSelfUnmute"] = allowSelfUnmute;
		OnMeetingControlMsg("on_audio_mute_changed", jsonMsg);
	}

	if (muted && _is_sending_content && _want_send_content_audio && _send_content_audio)
	{
		bool isSameDev = is_same_speaker_device();
		toggle_content_auido(false, isSameDev);
	}
}

void FrtcManager::OnParticipantList(std::set<std::string> uuidList)
{
	if (_full_rosters_list.size() == 0)
		return;

	{
		AutoLock lock(_roster_list_lock);

		unsigned int newSize = uuidList.size();

		DebugLog("participant list, uuid list size=%d", newSize);

		if (newSize == 0)
		{
			_full_rosters_list.clear();
		}
		else
		{
			Json::Value jsonTmpRosterList(Json::arrayValue);
			for (unsigned int i = 0; i < _full_rosters_list.size(); i++)
			{
				std::string uuid = _full_rosters_list[i]["UUID"].asString();
				if (uuid != get_app_uuid() && uuidList.find(uuid) != uuidList.end())
				{
					Json::Value tmp;
					jsonTmpRosterList[i] = _full_rosters_list[i];
				}
			}

			if (jsonTmpRosterList.size() != _full_rosters_list.size())
			{
				_full_rosters_list.clear();
				_full_rosters_list = jsonTmpRosterList;

				Json::FastWriter writer;
				const std::string json_file = writer.write(_full_rosters_list);
				DebugLog("OnParticipantList, new _full_rosters_list is %s", json_file.c_str());
			}
		}
	}

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "ParticipantChanged";
	OnCallNotification(callNotify);
	return;
}

void FrtcManager::OnPasscodeReject(RTC::MeetingStatusChangeReason reason)
{
	OnMeetingStatusChange(RTC::MeetingStatus::kDisconnected, (int)reason, "");
}

void FrtcManager::OnPasscodeRequest()
{
	if (reconnectHelper_->GetReconnectState() == ReconnectState::RECONNECT_TRYING || reconnectHelper_->GetReconnectState() == ReconnectState::RECONNECT_FAILED)
	{
		verify_password(reconnectHelper_->GetLastCallParam()->_meeting_pwd);
	}
	else if (_ui_call_state_change_callback)
	{
		_ui_call_state_change_callback(FRTC_CALL_STATE::CALL_CONNECTING, FRTC_SDK_CALL_REASON::CALL_PASSWORD_FAILED, _meeting_id.c_str(), _meeting_name.c_str());
	}
}

DWORD FrtcManager::OnAudioDeviceStateChanged()
{
	DebugLog("audio device state changed");
	::SetTimer(_hMain, IDT_UPDATE_AUDIO_DEVICE, 50, NULL);
	return 0;
}

DWORD FrtcManager::OnDefaultAudioChanged()
{
	DebugLog("default audio device changed");
	::SetTimer(_hMain, IDT_UPDATE_DEFAULT_AUDIO_DEVICE, 50, NULL);
	return 0;
}

void FrtcManager::OnError(DWORD err)
{
	if (err == AUDCLNT_E_DEVICE_INVALIDATED)
	{
		ErrorLog("audio device invalidated, err=%d, refresh audio device", err);
		OnAudioDeviceStateChanged();
	}
}

DWORD FrtcManager::OnPeakValue(float peak)
{
	if (_is_testing_mic || (_call_state == FRTC_CALL_STATE::CALL_CONNECTED && !is_local_audio_muted()))
	{
		Json::Value jsonMic;
		jsonMic["peak_value"] = std::to_string(peak);
		OnMeetingControlMsg("on_mic_test", jsonMic);
	}
	return 0;
}

DWORD FrtcManager::OnReadData(LPVOID buff, DWORD len, DWORD sampleRate)
{
	if (_apr_msid.empty())
	{
		return 0;
	}

	if (sampleRate == 44100)
	{
		_rtcInstance->ReceiveAudioFrame(_apr_msid, buff, len, sampleRate);
	}
	else
	{
		int half = len / 2;
		_rtcInstance->ReceiveAudioFrame(_apr_msid, buff, half, sampleRate);
		_rtcInstance->ReceiveAudioFrame(_apr_msid, static_cast<char*>(buff) + half, half, sampleRate);
	}

	return 0;
}

DWORD FrtcManager::OnWriteData(LPVOID buff, DWORD len, DWORD sampleRate)
{
	if (_is_sending_audio && buff != NULL)
	{
		_rtcInstance->SendAudioFrame(_apt_msid, buff, len, sampleRate);
	}
	return 0;
}

DWORD FrtcManager::OnWriteDataContent(LPVOID buff, DWORD len, DWORD sampleRate)
{
	if (_send_content_audio && buff)
	{
		_rtcInstance->SendContentAudioFrame(_apr_msid, buff, len, sampleRate);
	}
	return 0;
}

DWORD WINAPI FrtcManager::make_call_thread_proc(LPVOID param)
{
	FrtcManager* app = (FrtcManager*)param;
	if (!app->_signed_call_param._user_token.empty())
	{
		app->_is_guest_call = false;
		app->_rtcInstance->JoinMeetingLogin(app->_signed_call_param._server_address,
			app->_signed_call_param._meeting_number,
			app->_signed_call_param._display_name,
			app->_signed_call_param._user_token,
			app->_signed_call_param._meeting_pwd,
			app->_signed_call_param._call_rate);
	}
	else
	{
		app->_rtcInstance->JoinMeetingNoLogin(app->_guest_call_param._server_addr,
			app->_guest_call_param._meeting_number,
			app->_guest_call_param._user_display_name,
			app->_guest_call_param._call_rate,
			app->_guest_call_param._meeting_pwd);
	}

	return 0;
}

void FrtcManager::set_call_state_change_ui_callback(CallStateChangedCB callback)
{
	_ui_call_state_change_callback = callback;
}

void FrtcManager::set_meeting_control_msg_callback(MeetingControlMsgCB callback)
{
	_meeting_control_msg_callback = callback;
}

void FrtcManager::set_content_sending_state_callback(ContentShareStateCB callback)
{
	_content_sending_state_callback = callback;
}

void FrtcManager::set_password_request_ui_callback(CallPasswordProcessCB callback)
{
	_ui_pwd_request_callback = callback;
}

void FrtcManager::set_reminder_notify_ui_callback(ReminderNotifyCB callback)
{
	_ui_reminder_notify_callback = callback;
}

void FrtcManager::set_video_wnd_mouse_event_callback(WndMouseEventCB callback)
{
	_ui_window_mouse_event_callback = callback;
}

Json::Value FrtcManager::get_participant_list()
{
	AutoLock lock(_roster_list_lock);

	Json::Value jsonList;

	Json::Value jsonRosters;
	jsonRosters["fullList"] = true;

	jsonRosters["rosters"] = _full_rosters_list;
	jsonList["participant_list"] = jsonRosters;

	return jsonList;
}

std::string FrtcManager::get_participant_list_jstr()
{
	Json::Value jsonList = get_participant_list();

	Json::StreamWriterBuilder writerBuilder;
	writerBuilder["indentation"] = "";
	std::string listStr = Json::writeString(writerBuilder, jsonList);

	return listStr;
}

void FrtcManager::handle_svc_layout_changed(const RTC::LayoutDescription& layout)
{
	if (layout.layout_cells.empty())
	{
		toggle_local_video_wnd_hide(false);
	}

	InfoLog("layout changed, cell size=%zu", layout.layout_cells.size());

	_video_wnd_mgr->set_first_request_stream_uuid(layout.layout_cells.empty() ?
		"" : layout.layout_cells.front().uuid);

	std::string newContentStreamId = "";
	for (const auto& cell : layout.layout_cells)
	{
		DebugLog("get display name, uuid=%s, msid=%s, name=%s",
			cell.uuid.c_str(),
			cell.msid.c_str(),
			cell.display_name.c_str());

		_video_wnd_mgr->set_user_name(cell.msid, cell.display_name);
		_video_wnd_mgr->set_msid(cell.uuid, cell.msid);
		if (cell.msid.rfind("VCR", 0) == 0)
		{
			newContentStreamId = cell.msid;
		}
	}

	bool onlyOneRemoteInExistingList = (_layout_items_list.size() == 1);
	bool onlyOneRemoteInNewlist = (layout.layout_cells.size() == 1);

	std::vector<RTC::LayoutCell> toAdd;
	std::vector<RTC::LayoutCell> toRemove;
	std::vector<RTC::LayoutCell> toFrozen;

	for (const auto& existItem : _layout_items_list)
	{
		if (existItem.msid.rfind("VCR", 0) == 0 && existItem.msid != newContentStreamId)
		{
			toRemove.push_back(existItem);
		}
		else if (std::find_if(layout.layout_cells.begin(), layout.layout_cells.end(), [&](const RTC::LayoutCell& cell)
		{ return cell.uuid == existItem.uuid; }) == layout.layout_cells.end())
		{
			toRemove.push_back(existItem);
		}
	}

	for (const auto& newLayout : layout.layout_cells)
	{
		auto existLayout = std::find_if(_layout_items_list.begin(), _layout_items_list.end(), [&](const RTC::LayoutCell& cell)
		{ return cell.uuid == newLayout.uuid; });
		if (existLayout != _layout_items_list.end())
		{
			if (existLayout->width > 0 && newLayout.width < 0)
			{
				toFrozen.push_back(newLayout);
			}
		}
		else
		{
			toAdd.push_back(newLayout);
		}
	}

	_layout_items_list = layout.layout_cells;

	if (!toAdd.empty())
		set_name_card_begin_time(time(0));

	for (const auto& layoutToBeFrozen : toFrozen)
	{
		DebugLog("Frozen stream msid: %s, uuid = %s", layoutToBeFrozen.msid.c_str(), layoutToBeFrozen.uuid.c_str());

		if (layoutToBeFrozen.width < 0)
		{
			_video_wnd_mgr->show_video_stream(layoutToBeFrozen.msid, true);
		}
	}

	if (onlyOneRemoteInExistingList && onlyOneRemoteInNewlist)
	{
		for (const auto& layoutToBeRemoved : toRemove)
		{
			DebugLog("layout changed, remove stream, msid=%s, uuid=%s",
				layoutToBeRemoved.msid.c_str(),
				layoutToBeRemoved.uuid.c_str());

			_video_wnd_mgr->remove_video_stream(layoutToBeRemoved.msid, true);

			if (layoutToBeRemoved.msid.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
			{
				if (g_frtc_meeting_wnd != NULL)
					g_frtc_meeting_wnd->set_switch_layout_enable(true);
			}
		}
	}

	for (const auto& layoutToBeAdded : toAdd)
	{
		DebugLog("layout changed, add stream, msid=%s, uuid=%s",
			layoutToBeAdded.msid.c_str(),
			layoutToBeAdded.uuid.c_str());

		if (layoutToBeAdded.msid.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
		{
			if (g_frtc_meeting_wnd != NULL)
			{
				g_frtc_meeting_wnd->set_switch_layout_enable(false);
			}
		}
		_video_wnd_mgr->show_video_stream(layoutToBeAdded.msid, true);
	}

	if (!onlyOneRemoteInExistingList || !onlyOneRemoteInNewlist)
	{
		for (const auto& layoutToBeRemoved : toRemove)
		{
			DebugLog("layout changed, remove stream, msid=%s, uuid=%s, width=%d",
				layoutToBeRemoved.msid.c_str(),
				layoutToBeRemoved.uuid.c_str(),
				layoutToBeRemoved.width);

			if (layoutToBeRemoved.width < 0)
			{
				_video_wnd_mgr->remove_video_stream(layoutToBeRemoved.msid, true);

				if (layoutToBeRemoved.msid.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
				{
					if (g_frtc_meeting_wnd != NULL)
						g_frtc_meeting_wnd->set_switch_layout_enable(true);
				}
			}
		}
	}

	{
		AutoLock lock(_name_card_map_lock);
		if (!_name_card_map.empty())
		{
			std::string keySmall = "_FontSmall";
			std::string keyMedium = "_FontMeidum";
			std::string keyBig = "_FontBig";
			std::string keyTmp = "_FontTmp";
			for (auto itNameCard = _name_card_map.begin(); itNameCard != _name_card_map.end();)
			{
				std::string iterName = "";
				if (itNameCard->first.find(keySmall) != std::string::npos)
				{
					iterName = itNameCard->first.substr(0, itNameCard->first.find(keySmall));
				}
				else if (itNameCard->first.find(keyMedium) != std::string::npos)
				{
					iterName = itNameCard->first.substr(0, itNameCard->first.find(keyMedium));
				}
				else if (itNameCard->first.find(keyBig) != std::string::npos)
				{
					iterName = itNameCard->first.substr(0, itNameCard->first.find(keyBig));
				}
				else if (itNameCard->first.find(keyTmp) != std::string::npos)
				{
					iterName = itNameCard->first.substr(0, itNameCard->first.find(keyTmp));
				}

				if (!iterName.empty())
				{
					auto existItem = std::find_if(_layout_items_list.begin(), _layout_items_list.end(), [&](const RTC::LayoutCell& item)
					{
						std::string name = item.display_name + std::to_string(item.width) + std::to_string(item.height);
						return iterName == name; });

					if (existItem == _layout_items_list.end())
					{
						delete[] itNameCard->second;
						itNameCard = _name_card_map.erase(itNameCard);
						continue;
					}
				}

				++itNameCard;
			}
		}
	}

	update_icon_window_status();
}

void FrtcManager::update_icon_window_status()
{
	for (int i = 0; i < _full_rosters_list.size(); i++)
	{
		_video_wnd_mgr->toggle_audio_state_icon_window_show(
			_full_rosters_list[i]["UUID"].asString(),
			_full_rosters_list[i]["muteAudio"] == "true" ? true : false);
	}
	return;
}

void FrtcManager::on_participant_mute_state_changed(std::map<std::string, RTC::ParticipantStatus>& muteStatusList, bool isFullList)
{
	{
		AutoLock lock(_roster_list_lock);

		if (isFullList)
		{
			_full_rosters_list.clear();

			for (const auto& item : muteStatusList)
			{
				InfoLog("participant mute state changed, uuid=%s, name=%s, "
					"audioMute=%d, videoMute=%d",
					item.first.c_str(), item.second.display_name.c_str(),
					item.second.audio_mute, item.second.video_mute);

				Json::Value jsonRoster;
				jsonRoster["UUID"] = item.first;
				jsonRoster["name"] = item.second.display_name;
				jsonRoster["uerId"] = item.second.user_id;
				jsonRoster["muteAudio"] = item.second.audio_mute ? "true" : "false";
				jsonRoster["muteVideo"] = item.second.video_mute ? "true" : "false";

				std::string uuid = jsonRoster["UUID"].asString();
				if (uuid == _uuid)
				{
					std::string newName = item.second.display_name;
					std::string ansiStr = FRTCSDK::FRTCSdkUtil::get_ansi_string(newName);
					update_self_display_name(ansiStr);
					_full_rosters_list[0] = jsonRoster;
				}

				_full_rosters_list.append(jsonRoster);

				Json::FastWriter writer;
				const std::string json_file = writer.write(_full_rosters_list);
				DebugLog("participant mute state changed, full list=%s",
					json_file.c_str());

				_video_wnd_mgr->toggle_audio_state_icon_window_show(uuid,
					jsonRoster["muteAudio"].asString() == "true");
			}
		}
		else
		{
			for (int i = 0; i < _full_rosters_list.size(); i++)
			{
				auto it = muteStatusList.find(_full_rosters_list[i]["UUID"].asString());
				if (it != muteStatusList.end())
				{
					InfoLog("on_participant_mute_state_changed: UUID = %s, name = %s, audioMute = %d, videoMute = %d",
						it->first.c_str(), it->second.display_name.c_str(), it->second.audio_mute, it->second.video_mute);

					Json::Value jsonRoster;
					jsonRoster["UUID"] = it->first;
					jsonRoster["name"] = it->second.display_name;
					jsonRoster["userId"] = it->second.user_id;
					jsonRoster["muteAudio"] = it->second.audio_mute ? "true" : "false";
					jsonRoster["muteVideo"] = it->second.video_mute ? "true" : "false";

					std::string uuid = jsonRoster["UUID"].asString();
					if (uuid == _uuid)
					{
						std::string newName = it->second.display_name;
						std::string ansiStr = FRTCSDK::FRTCSdkUtil::get_ansi_string(newName);
						update_self_display_name(ansiStr);
					}

					_full_rosters_list[i] = jsonRoster;
					
					muteStatusList.erase(it);

					Json::FastWriter writer;
					const std::string json_file = writer.write(_full_rosters_list);
					DebugLog("on_participant_mute_state_changed, not full list, current full_rosters_list is %s",
						json_file.c_str());

					_video_wnd_mgr->toggle_audio_state_icon_window_show(
						jsonRoster["UUID"].asString(),
						jsonRoster["muteAudio"].asString() == "true");
				}
			}

			for (const auto& item : muteStatusList)
			{
				Json::Value jsonRoster;

				jsonRoster["UUID"] = item.first;
				jsonRoster["name"] = item.second.display_name;
				jsonRoster["userId"] = item.second.user_id;
				jsonRoster["muteAudio"] = item.second.audio_mute ? "true" : "false";
				jsonRoster["muteVideo"] = item.second.video_mute ? "true" : "false";

				std::string uuid = jsonRoster["UUID"].asString();
				if (uuid == _uuid)
				{
					std::string newName = item.second.display_name;
					std::string ansiStr = FRTCSDK::FRTCSdkUtil::get_ansi_string(newName);
					update_self_display_name(ansiStr);
				}

				_full_rosters_list.append(jsonRoster);		

				Json::FastWriter writer;
				const std::string json_file = writer.write(_full_rosters_list);
				DebugLog("on_participant_mute_state_changed, not fulllist, new added, current _full_rosters_list is %s", json_file.c_str());

				_video_wnd_mgr->toggle_audio_state_icon_window_show(
					jsonRoster["UUID"].asString(),
					jsonRoster["muteAudio"].asString() == "true");
			}
		}

		std::string strLocalSourceID;
		get_local_preview_msid(strLocalSourceID);
		_video_wnd_mgr->toggle_audio_state_icon_window_show(strLocalSourceID, _is_local_audio_muted);
	}

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "ParticipantChanged";
	OnCallNotification(callNotify);
}

void FrtcManager::update_self_display_name(const std::string& new_name)
{
	//std::string utf8name = FRTCSDK::FRTCSdkUtil::get_utf8_string(new_name);
	_is_guest_call ? _guest_user_display_name = new_name : _signed_user_display_name = new_name;
	reconnectHelper_->SetLastCallLatestDisplayName(new_name.c_str());
}

void FrtcManager::update_self_video_status()
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._is_hide_local_video = _is_local_video_muted;
	callNotify._msg_name = "NotifyUICameraStatus";
	OnCallNotification(callNotify);
}


void FrtcManager::update_video_windows()
{
}

void FrtcManager::on_update_video_windows()
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "UpdateVideoWindows";
	OnCallNotification(callNotify);
}

FRTC_SDK_CALL_REASON FrtcManager::map_rtcsdk_state_reason_to_frtc_call_reason(RTC::MeetingStatusChangeReason reason)
{
	switch (reason)
	{
	case RTC::MeetingStatusChangeReason::kStatusOk:
		return FRTC_SDK_CALL_REASON::CALL_SUCCESS;
	case RTC::MeetingStatusChangeReason::kUnsecure:
		return FRTC_SDK_CALL_REASON::CALL_SUCCESS_BUT_UNSECURE;
	case RTC::MeetingStatusChangeReason::kMeetingNoExist:
		return FRTC_SDK_CALL_REASON::CALL_NON_EXISTENT_MEETING;
	case RTC::MeetingStatusChangeReason::kAborted:
		return FRTC_SDK_CALL_REASON::CALL_ABORT;
	case RTC::MeetingStatusChangeReason::kMeetingLocked:
		return FRTC_SDK_CALL_REASON::CALL_LOCKED;
	case RTC::MeetingStatusChangeReason::kServerError:
		return FRTC_SDK_CALL_REASON::CALL_SERVER_ERROR;
	case RTC::MeetingStatusChangeReason::kAuthenticationFail:
		return FRTC_SDK_CALL_REASON::CALL_AUTH_FAILED;
	case RTC::MeetingStatusChangeReason::kServerReject:
		return FRTC_SDK_CALL_REASON::CALL_REJECTED;
	case RTC::MeetingStatusChangeReason::kMeetingStop:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_STOP;
	case RTC::MeetingStatusChangeReason::kMeetingInterrupt:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_INTERRUPT;
	case RTC::MeetingStatusChangeReason::kRemovedFromMeeting:
		return FRTC_SDK_CALL_REASON::CALL_REMOVE_FROM_MEETING;
	case RTC::MeetingStatusChangeReason::kPasscodeTooManyRetries:
		return FRTC_SDK_CALL_REASON::CALL_PASSWORD_FAILED_RETRY_MAX;
	case RTC::MeetingStatusChangeReason::kPasscodeTimeout:
		return FRTC_SDK_CALL_REASON::CALL_PASSWORD_FAILED_TIMEOUT;
	case RTC::MeetingStatusChangeReason::kMeetingExpired:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_EXPIRED;
	case RTC::MeetingStatusChangeReason::kMeetingNoStarted:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_NOT_START;
	case RTC::MeetingStatusChangeReason::kGuestUnallowed:
		return FRTC_SDK_CALL_REASON::CALL_GUEST_NOT_ALLOW;
	case RTC::MeetingStatusChangeReason::kMeetingFull:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_FULL;
	case RTC::MeetingStatusChangeReason::kPasscodeFailed:
		return FRTC_SDK_CALL_REASON::CALL_PASSWORD_FAILED;
	case RTC::MeetingStatusChangeReason::kLicenseNoFound:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_NO_LICENSE;
	case RTC::MeetingStatusChangeReason::kLicenseLimitReached:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_LICENSE_MAX_LIMIT_REACHED;
	case RTC::MeetingStatusChangeReason::kMeetingEndAbnormal:
		return FRTC_SDK_CALL_REASON::CALL_MEETING_END_ABNORMAL;
	default:
		return FRTC_SDK_CALL_REASON::CALL_FAILED;
	}
}

void FrtcManager::load_camera_muted_pic()
{
	HRSRC hrsrc = FindResourceW(g_hInstance, MAKEINTRESOURCE(IDR_YUV1), L"YUV");
	if (hrsrc)
	{
		DWORD dwResourceSize = SizeofResource(g_hInstance, hrsrc);
		if (dwResourceSize > 0)
		{
			HGLOBAL hGlobalResource = LoadResource(g_hInstance, hrsrc); // load it
			if (hGlobalResource)
			{
				void* imagebytes = LockResource(hGlobalResource); // get a pointer to the file bytes

				// copy image bytes into a real hglobal memory handle
				HGLOBAL hGlobal = ::GlobalAlloc(GHND, dwResourceSize);
				if (hGlobal)
				{
					void* pBuffer = ::GlobalLock(hGlobal);
					if (pBuffer)
					{
						memcpy(pBuffer, imagebytes, dwResourceSize);
						IStream* pStream = nullptr;
						HRESULT hr = CreateStreamOnHGlobal(hGlobal, TRUE, &pStream);
						if (SUCCEEDED(hr))
						{
							// pStream now owns the global handle and will invoke GlobalFree on release
							_local_camera_muted_pic_data_ptr = new BYTE[dwResourceSize];
							ULONG nRead = 0;
							pStream->Read(_local_camera_muted_pic_data_ptr, dwResourceSize, &nRead);
							pStream->Release();
						}
						::GlobalUnlock(hGlobal);
					}
					::GlobalFree(hGlobal);
				}
			}
		}
	}
}

void FrtcManager::load_share_window_paused_pic()
{
	HRSRC hrsrc = FindResourceW(g_hInstance, MAKEINTRESOURCE(IDR_YUV2), L"YUV");
	if (hrsrc)
	{
		DWORD dwResourceSize = SizeofResource(g_hInstance, hrsrc);
		if (dwResourceSize > 0)
		{
			HGLOBAL hGlobalResource = LoadResource(g_hInstance, hrsrc); // load it
			if (hGlobalResource)
			{
				void* imagebytes = LockResource(hGlobalResource); // get a pointer to the file bytes

				// copy image bytes into a real hglobal memory handle
				HGLOBAL hGlobal = ::GlobalAlloc(GHND, dwResourceSize);
				if (hGlobal)
				{
					void* pBuffer = ::GlobalLock(hGlobal);
					if (pBuffer)
					{
						memcpy(pBuffer, imagebytes, dwResourceSize);
						IStream* pStream = nullptr;
						HRESULT hr = CreateStreamOnHGlobal(hGlobal, TRUE, &pStream);
						if (SUCCEEDED(hr))
						{
							// pStream now owns the global handle and will invoke GlobalFree on release
							_share_window_paused_pic_data_ptr = new BYTE[dwResourceSize];
							ULONG nRead = 0;
							pStream->Read(_share_window_paused_pic_data_ptr, dwResourceSize, &nRead);
							pStream->Release();
						}
						::GlobalUnlock(hGlobal);
					}
					::GlobalFree(hGlobal);
				}
			}
		}
	}
}

unsigned char* FrtcManager::get_media_convert_buffer(int size)
{
	if (_media_convert_buffer != nullptr && _media_convert_buffer_size < size)
	{
		delete[] _media_convert_buffer;
		_media_convert_buffer = nullptr;
		_media_convert_buffer_size = 0;
	}

	if (_media_convert_buffer == nullptr)
	{
		_media_convert_buffer = new unsigned char[size];
		_media_convert_buffer_size = size;
	}

	return _media_convert_buffer;
}

int FrtcManager::get_signal_status(const std::string& statistics)
{
	Json::Value jsonStatistics;
	Json::Reader reader;
	if (!reader.parse(statistics, jsonStatistics))
	{
		DebugLog("statistics parse failed: %s", statistics.c_str());
		return 0;
	}

	Json::Value jsonMediaStatistics = jsonStatistics["mediaStatistics"];
	int aprLoss = get_average_lost_rate(jsonMediaStatistics["apr"]);
	int apsLoss = get_average_lost_rate(jsonMediaStatistics["aps"]);
	int vprLoss = get_average_lost_rate(jsonMediaStatistics["vpr"]);
	int vpsLoss = get_average_lost_rate(jsonMediaStatistics["vps"]);
	int vcrLoss = get_average_lost_rate(jsonMediaStatistics["vcr"]);

	if (vpsLoss >= VIDEO_LOSS_THRESHOLD_2 || vprLoss >= VIDEO_LOSS_THRESHOLD_2 ||
		apsLoss >= AUDIO_LOSS_THRESHOLD_2 || aprLoss >= AUDIO_LOSS_THRESHOLD_2 ||
		vcrLoss >= VIDEO_LOSS_THRESHOLD_2)
	{
		return SIGNAL_INTENSITY_LOW;
	}
	else if (vpsLoss >= VIDEO_LOSS_THRESHOLD_1 || vprLoss >= VIDEO_LOSS_THRESHOLD_1 ||
		apsLoss >= AUDIO_LOSS_THRESHOLD_1 || aprLoss >= AUDIO_LOSS_THRESHOLD_1 ||
		vcrLoss >= VIDEO_LOSS_THRESHOLD_1)
	{
		return SIGNAL_INTENSITY_MEDIAN;
	}
	else
	{
		return SIGNAL_INTENSITY_HIGH;
	}
}

int FrtcManager::get_average_lost_rate(const Json::Value& mediaStatistics)
{
	int pkgLost = 0;
	const int size = mediaStatistics.size();
	if (!mediaStatistics.isArray() || size == 0)
		return 0;

	for (int i = 0; i < size; i++)
	{
		pkgLost += mediaStatistics[i]["packageLossRate"].asInt();
	}

	return pkgLost / size;
}

int FrtcManager::get_network_intensity()
{
	std::string report = get_statistics_jstr();
	int intensity = get_signal_status(report);
	return intensity;
}

bool FrtcManager::is_same_speaker_device()
{
	AutoLock lock(_media_device_lock);
	if (_audio_render == nullptr || _content_audio_capture == nullptr)
	{
		return false;
	}

	GUID currentSpeaker = { 0 };
	_audio_render->GetDeviceId(&currentSpeaker);

	GUID currentSysSpeaker = { 0 };
	_content_audio_capture->GetDeviceId(&currentSysSpeaker);

	return IsEqualGUID(currentSpeaker, currentSysSpeaker) == 1;
}

void FrtcManager::start_send_content_audio(bool notifRTC)
{
	bool isSameDevice = is_same_speaker_device();
	if (!isSameDevice)
	{
		if (notifRTC)
		{
			DebugLog("no sending notification to RTC because it is not the same device");
			toggle_content_auido(false, isSameDevice);
		}

		return;
	}

	if (notifRTC)
	{
		DebugLog("enable content audio=%d, send notification to RTC",
			_send_content_audio);
		toggle_content_auido(_send_content_audio, isSameDevice);
	}

	if (_want_send_content_audio && isSameDevice)
	{
		_content_audio_capture->StartCapture();
	}
}

void FrtcManager::set_cpu_level_to_dshow_capture()
{
	int level = (int)_rtcInstance->GetCPULevel();
	_video_capture.set_cpu_level(level);
}

void FrtcManager::set_use_gdi_render(bool useGDIRender)
{
	_use_gdi_render = useGDIRender;
}

BOOL FrtcManager::initialized()
{
	return _status != S_CREATE && _status != S_DESTROY;
}

void FrtcManager::clean_sc_msg_queue()
{
	std::queue<FRTC_SDK_CALL_NOTIFICATION> empty;
	std::swap(_sc_msg_queue, empty);
}

FRTC_CALL_STATE FrtcManager::get_call_state()
{
	return _call_state;
}

bool FrtcManager::is_sending_content()
{
	return _is_sending_content;
}

ReconnectState FrtcManager::get_reconnect_state()
{
	return reconnectHelper_->GetReconnectState();
}

const FrtcCallParam* FrtcManager::get_lastcall_param()
{
	return reconnectHelper_->GetLastCallParam();
}

void FrtcManager::cancel_next_reconnect()
{
	reconnectHelper_->cancel_next_reconnect();
	reconnectHelper_->ResetReconnectStatue();
}

void FrtcManager::set_current_view(VideoWnd* pView)
{
	_video_wnd_mgr->set_current_view(pView);
}

void FrtcManager::set_main_cell_size()
{
	_video_wnd_mgr->set_main_cell_size();
	update_video_windows();
}

void FrtcManager::on_main_window_size_changed(LONG left,
	LONG top,
	LONG right,
	LONG bottom)
{
	if (!_hMain)
		return;

	DebugLog("set main window size, l=%ld, t=%ld, r=%ld, b=%ld",
		left, top, right, bottom);

	_video_wnd_mgr->on_main_window_size_changed(left, top, right, bottom);
	update_video_windows();
}

void FrtcManager::on_media_device_arrival()
{
	::SetTimer(_hMain, IDT_UPDATE_DEVICE_SETTING, 3000, NULL);
}

void FrtcManager::on_media_device_removal()
{
	::SetTimer(_hMain, IDT_UPDATE_DEVICE_SETTING, 3000, NULL);
}

void FrtcManager::update_audio_devices()
{
	DebugLog("update audio devices");
	AutoLock lock(_media_device_lock);

	if (!_using_os_default_speaker_device && !_audio_render->CheckDeviceExist())
	{
		set_audio_device_os_default(FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_SPEAKER, true);
	}

	if (!_using_os_default_mic_device && !_people_audio_capture->CurrentDeviceExist())
	{
		DebugLog("current mic device not exist, reset to OS default");
		set_audio_device_os_default(FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_MIC, true);
	}
}

void FrtcManager::update_content_audio_device()
{
	DebugLog("Updating content audio device");
	init_content_audio_device();
	GUID currentSysSpeaker = { 0 };
	_content_audio_capture->GetDeviceId(&currentSysSpeaker);
	bool notifRTC = !IsEqualGUID(currentSysSpeaker, _default_system_speaker_guid);
	_default_system_speaker_guid = currentSysSpeaker;
	start_send_content_audio(notifRTC);
}

void FrtcManager::update_media_devices()
{
	update_audio_devices();
	update_camera_devices();
}

void FrtcManager::reset_content_audio()
{
	if (_is_sharing_content && _content_audio_capture && _want_send_content_audio && !_is_local_audio_muted && is_same_speaker_device())
	{
		_send_content_audio = true;
		toggle_content_auido(true, true);
		_content_audio_capture->StartCapture();
	}
	else
	{
		_send_content_audio = false;
		toggle_content_auido(false, false);
	}
}

void FrtcManager::sync_audio_device_with_os()
{
	DebugLog("sync audio device with OS");
	AutoLock lock(_media_device_lock);

	if (_using_os_default_mic_device)
	{
		DebugLog("currently using default mic, sync mic device with OS");
		std::wstring strDefaultMic;
		if (FAILED(_people_audio_capture->GetOSDefaultDevice(strDefaultMic)))
		{
			ErrorLog("get OS default mic failed");
			return;
		}
		DebugLog("OS default mic=%s", FRTCSDK::FRTCSdkUtil::wstring_to_string(strDefaultMic).c_str());
		GUID g{ 0 };
		FRTCSDK::FRTCSdkUtil::get_guid_from_wstring(strDefaultMic.c_str(), &g);
		reset_mic_device(g);
	}

	if (_using_os_default_speaker_device)
	{
		_audio_render->SyncWithOS();
		_rtcInstance->ChangeAudioDevice(RTC::kSpeaker);
	}

	init_content_audio_device();
	reset_content_audio();
}

void FrtcManager::set_audio_device_os_default(FRTC_MEDIA_DEVICE_TYPE type, bool useDefault)
{
	DebugLog("set audio device to OS default, type=%d, useDefault=%d", type, useDefault);

	AutoLock lock(_media_device_lock);

	if (type == FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_MIC)
	{
		_using_os_default_mic_device = useDefault;

		if (useDefault)
		{
			std::wstring strDefaultMic;
			if (FAILED(_people_audio_capture->GetOSDefaultDevice(strDefaultMic)))
			{
				ErrorLog("get OS default mic failed");
				return;
			}
			DebugLog("OS default mic=%s", FRTCSDK::FRTCSdkUtil::wstring_to_string(strDefaultMic).c_str());
			GUID g{ 0 };
			FRTCSDK::FRTCSdkUtil::get_guid_from_wstring(strDefaultMic.c_str(), &g);
			reset_mic_device(g);
		}
	}
	else if (type == FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_SPEAKER)
	{
		_using_os_default_speaker_device = useDefault;

		if (useDefault)
		{
			_audio_render->SyncWithOS();
			_rtcInstance->ChangeAudioDevice(RTC::kSpeaker);
			init_content_audio_device();
			reset_content_audio();
		}
	}
}

void FrtcManager::update_camera_devices()
{
	AutoLock lock(_media_device_lock);
	// Update camera
	const video_device_list deviceList = _video_dev_manager.get_video_device_list(TRUE);
	if (deviceList.empty())
	{
		DebugLog("No camera found");
		_video_capture.set_camera_device(NULL);
		notify_camera_device_reset();
		return;
	}

	enable_camera(true);
	std::wstring strDefaultCameraId = _video_capture.get_current_camera_device_id();
	if (strDefaultCameraId.empty())
	{
		DebugLog("get camera device failed");
		_video_capture.set_camera_device(NULL);
		notify_camera_device_reset();
		return;

	}

	DebugLog("camera device count=%d", deviceList.size());

	bool found = false;
	for (const auto& it : deviceList)
	{
		if (0 == wcscmp(it->id, strDefaultCameraId.c_str()))
		{
			found = true;
			break;
		}
	}

	if (found)
	{
		notify_camera_device_reset();
	}
	else
	{
		DebugLog("Last used camera not found, switching to first camera");
		reset_camera_device(deviceList.front());
		notify_camera_device_reset();
	}
}

const TCHAR* FrtcManager::get_preferred_camera_device()
{
	auto videoPreferInput = _video_dev_manager.get_preferred_video_input();
	return videoPreferInput ? videoPreferInput->id : nullptr;
}

const TCHAR* FrtcManager::get_preferred_camera_device_name()
{
	auto videoPreferInput = _video_dev_manager.get_preferred_video_input();
	return videoPreferInput ? videoPreferInput->name : nullptr;
}

const TCHAR* FrtcManager::get_current_camera_device()
{
	return _video_capture.pDevice ? _video_capture.pDevice->id : nullptr;
}

const TCHAR* FrtcManager::get_current_camera_device_name()
{
	return _video_capture.pDevice ? _video_capture.pDevice->name : nullptr;
}

void FrtcManager::reset_camera_device(video_device* device)
{
	AutoLock lock(_media_device_lock);

	if (!device || !_video_capture.pDevice || device->id == NULL)
	{
		return;
	}

	if (0 != wcscmp(device->id, _video_capture.pDevice->id))
	{
		_video_capture.stop_preview();
		_video_capture.set_camera_device(NULL);

		VIDEO_CAPS_STRUCT maxCaps{ 0 };
		maxCaps.width = MAX_PEOPLE_VIDEO_WIDTH;
		maxCaps.height = MAX_PEOPLE_VIDEO_HEIGHT;
		maxCaps.framerate = WIN_VIDEO_INPUT_DEAULT_FRAMERATE;
		_video_capture.set_video_capabilities(maxCaps);
		_video_capture.set_camera_device(device);

		const VIDEO_CAPS_STRUCT* caps = _video_capture.get_video_capabilities();
		if (caps)
		{
			std::string resolution;
			create_resolution_str(caps->height, caps->framerate, resolution);
			DebugLog("Setting camera capability: %s", resolution.c_str());
			_rtcInstance->SetCameraCapability(resolution);
		}
	}
}

void FrtcManager::get_current_mic_device(GUID& guid)
{
	_people_audio_capture->GetDeviceId(&guid);
}

const TCHAR* FrtcManager::get_current_mic_device_name()
{
	GUID guid;
	_people_audio_capture->GetDeviceId(&guid);

	const std::vector<DevInfo>& listdev = _people_audio_capture->GetDeviceList();
	for (const auto& dev : listdev)
	{
		if (memcmp(&(dev.second), &guid, sizeof(GUID)) == 0)
		{
			return dev.first.c_str();
		}
	}

	return listdev.front().first.c_str();
}

void FrtcManager::reset_mic_device(const GUID& guid, bool forceReset)
{
	AutoLock lock(_media_device_lock);

	GUID current{ 0 };
	_people_audio_capture->GetDeviceId(&current);
	std::string guidstr;
	FRTCSDK::FRTCSdkUtil::guid_to_string(current, guidstr);

	DebugLog("reset mic device, force=%d, current mic device=%s",
		forceReset, guidstr.c_str());

	if (forceReset || current != guid)
	{
		_people_audio_capture->UpdateDeviceList(FALSE);
		std::vector<DevInfo> listdev = _people_audio_capture->GetDeviceList();
		bool found = false;

		for (const auto& devInfo : listdev)
		{
			if (devInfo.second == guid)
			{
				found = true;
				bool shareMode = _people_audio_capture->IsDeviceShareMode();
				bool capturing = _people_audio_capture->IsCapturing();

				_people_audio_capture->StopCapture();
				delete _people_audio_capture;

				_people_audio_capture = new PeopleAudioCapture();
				_people_audio_capture->SetDeviceShareMode(shareMode);

				std::wstring strGUID;
				FRTCSDK::FRTCSdkUtil::guid_to_wstring(guid, strGUID);
				set_microphone_device((TCHAR*)strGUID.c_str());

				_people_audio_capture->SetAudioSink(this);
				if (capturing || _call_state == FRTC_CALL_STATE::CALL_CONNECTED)
					_people_audio_capture->StartCapture();

				_rtcInstance->ChangeAudioDevice(RTC::kCapture);
				break;
			}
		}

		if (!found)
		{
			ErrorLog("Specified device not found, switch failed");
		}
	}
}

void FrtcManager::get_current_speaker_device(GUID& guid)
{
	_audio_render->GetDeviceId(&guid);
}

const TCHAR* FrtcManager::get_current_speaker_device_name()
{
	GUID guid;
	_audio_render->GetDeviceId(&guid);

	std::vector<DevInfo> DeviceList = _audio_render->GetDeviceList();
	for (const auto& device : DeviceList)
	{
		if (memcmp(&(device.second), &guid, sizeof(GUID)) == 0)
		{
			return device.first.c_str();
		}
	}

	return DeviceList.front().first.c_str();
}

void FrtcManager::reset_speaker_device(const GUID& guid)
{
	AutoLock lock(_media_device_lock);

	GUID current{ 0 };
	_audio_render->GetDeviceId(&current);
	if (memcmp(&current, &guid, sizeof(GUID)) != 0)
	{
		_audio_render->UpdateDeviceList(false);
		std::vector<DevInfo> DeviceList = _audio_render->GetDeviceList();
		for (const auto& device : DeviceList)
		{
			if (memcmp(&(device.second), &guid, sizeof(GUID)) == 0)
			{
				_audio_render->StopRender();
				_audio_render.reset(new AudioRender());
				_audio_render->SetAudioSource(this);
				_audio_render->SetDeviceId(guid);
				std::wstring strGUID;
				FRTCSDK::FRTCSdkUtil::guid_to_wstring(guid, strGUID);
				HRESULT hr = _audio_render->Init((TCHAR*)strGUID.c_str());
				if (SUCCEEDED(hr))
				{
					_audio_render->StartRender();
					_rtcInstance->ChangeAudioDevice(RTC::kSpeaker);
				}
				else
				{
					InfoLog("audio_render->Init failed, try os default");
					_audio_render->SyncWithOS();
					_rtcInstance->ChangeAudioDevice(RTC::kSpeaker);
				}
				break;
			}
		}
	}
	init_content_audio_device();
	reset_content_audio();
}

void FrtcManager::start_mic_test()
{
	AutoLock _lock(_media_device_lock);
	if (_people_audio_capture && !_people_audio_capture->IsCapturing())
	{
		_people_audio_capture->StartCapture();
	}
	_is_testing_mic = TRUE;
}

void FrtcManager::stop_mic_test()
{
	AutoLock _lock(_media_device_lock);
	_is_testing_mic = false;
	if (_call_state != FRTC_CALL_STATE::CALL_CONNECTED)
	{
		_people_audio_capture->StopCapture();
	}
}

float FrtcManager::get_mic_peak_meter()
{
	return _people_audio_capture->GetDevicePeakValue();
}

void FrtcManager::set_mic_share_mode(bool useShareMode)
{
	AutoLock _lock(_media_device_lock);
	if (_people_audio_capture)
	{
		_people_audio_capture->SetDeviceShareMode(useShareMode);
		if (_people_audio_capture->IsCapturing())
		{
			GUID deviceId;
			_people_audio_capture->GetDeviceId(&deviceId);
			reset_mic_device(deviceId, true);
		}
		else
		{
			TCHAR deviceGuid[40];
			_people_audio_capture->GetDeviceGUID(deviceGuid);

			if (std::wstring(deviceGuid).empty())
			{
				ErrorLog("Target microphone device not found");
			}
			else
			{
				set_microphone_device(deviceGuid);
			}
		}
	}
}

bool FrtcManager::get_mic_share_mode()
{
	return _people_audio_capture && _people_audio_capture->IsDeviceShareMode();
}

void FrtcManager::clear_main_window()
{
	_video_wnd_mgr->clear_params();
	_video_wnd_mgr->remove_main_window();

	std::string strLocal;
	get_local_preview_msid(strLocal);
	_video_wnd_mgr->set_local_stream_msid(strLocal);
	_video_wnd_mgr->init_free_video_wnd();
}

void FrtcManager::reset_main_window(HWND hwnd, HINSTANCE hInst)
{
	_video_wnd_mgr->set_main_window(hwnd, hInst, _use_gdi_render);
	_hMain = hwnd;

	std::string strLocal;
	get_local_preview_msid(strLocal);
	_video_wnd_mgr->set_local_stream_msid(strLocal);

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "SendQueue";
	OnCallNotification(callNotify);
}

void FrtcManager::enter_full_screen(bool bFull)
{
	_video_wnd_mgr->enter_full_screen(bFull);
}

void FrtcManager::exit_full_screen()
{
}

void FrtcManager::start_send_window_content(HWND hwnd)
{
	_frtc_content_capturer.reset(new frtc_content_capturer());
	_frtc_content_capturer->init(frtc_capture_type::window, (int)hwnd, L"Wnd");
	_frtc_content_capturer->set_webrtc_capture_callback(static_cast<IWebRTCCaptureCallback*>(this));
	_is_window_content = true;
	_rtcInstance->StartSendContent();
}

bool FrtcManager::start_send_desktop_content(const WCHAR* szMonitor, int MonitorIndex)
{
	DebugLog("Sharing desktop content, monitor name: %ls", szMonitor);

	if (szMonitor[0] == L'\0')
	{
		DebugLog("Unable to share desktop content");
		return false;
	}

	_frtc_content_capturer.reset(new frtc_content_capturer());
	_frtc_content_capturer->init(frtc_capture_type::monitor, MonitorIndex, szMonitor);
	_frtc_content_capturer->set_capture_callback(static_cast<interface_frtc_capture_content_callback*>(this));

	_display_name = szMonitor;
	_display_index = MonitorIndex;

	_rtcInstance->StartSendContent();
	return true;
}

void FrtcManager::stop_sending_content()
{
	_rtcInstance->StopSendContent();
	update_content_video_msid("");
}

void FrtcManager::start_send_content(const std::wstring& name, int index)
{
	DebugLog("Sharing content: %s, index: %d", FRTCSDK::FRTCSdkUtil::wstring_to_string(name).c_str(), index);

	_average_cpu_load = get_cpu_load_level();

	start_send_desktop_content(name.c_str(), index);
}

void FrtcManager::start_send_content(int hwnd)
{
	char title[200]{ 0 };
	GetWindowTextA((HWND)hwnd, title, 200);
	DebugLog("Sharing content - Title: %s, HWND: %d", title, hwnd);

	_average_cpu_load = get_cpu_load_level();
	start_send_window_content((HWND)hwnd);
}

void FrtcManager::set_share_content_with_audio(bool contentAudioEnable)
{
	_want_send_content_audio = contentAudioEnable;
}

void FrtcManager::init_content_audio_device()
{
	if (_content_audio_capture)
	{
		_content_audio_capture->StopCapture();
		delete _content_audio_capture;
		_content_audio_capture = nullptr;
	}

	_content_audio_capture = new ContentAudioCapture();
	_content_audio_capture->SetAudioSink(this);

	TCHAR* spkGUID = nullptr;
	_content_audio_capture->Init(spkGUID);
}

void FrtcManager::start_content_capture(bool useMinRate)
{
	DebugLog("start content capture");
	_frtc_content_capturer->start(useMinRate);
}

void FrtcManager::stop_content_capture()
{
	DebugLog("stop content capture");
	_frtc_content_capturer->stop();
}

void FrtcManager::on_content_capture(void* buffer, int width, int height)
{
	if (buffer)
	{
		send_content_video_frame(buffer, width * height * 4, width, height, RTC::kARGB);
		if (_content_paused && _want_send_content_audio && _send_content_audio)
		{
			toggle_content_auido(true, is_same_speaker_device());
		}
	}
	else
	{
		if (_share_window_paused_pic_data_ptr)
		{
			send_content_video_frame(_share_window_paused_pic_data_ptr, 1920 * 1080 * 3 / 2, 1920, 1080, RTC::kI420);
		}
		if (!_content_paused && _want_send_content_audio && _send_content_audio)
		{
			toggle_content_auido(false, is_same_speaker_device());
		}
	}
	_content_paused = !buffer;
}

void FrtcManager::on_content_capture_error()
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "StopContent";

	OnCallNotification(callNotify);
}

void FrtcManager::OnCapture(void* buffer, int width, int height)
{
	on_content_capture(buffer, width, height);
}

void FrtcManager::OnCaptureError()
{
	on_content_capture_error();
}

void FrtcManager::on_capture(void* buffer, int width, int height)
{
	on_content_capture(buffer, width, height);
}

void FrtcManager::on_capture_error()
{
	on_content_capture_error();
}

FRTC_REMINDER_TYPE FrtcManager::get_reminder_type(FRTC_TIP_TYPE type)
{
	switch (type)
	{
	case kAudioMuteDetect:
		return FRTC_REMINDER_TYPE::REMINDER_AUDIO_MUTE;
	case kCameraOpenFail:
		return FRTC_REMINDER_TYPE::REMINDER_CAMERA_ERROR;
	case kContentShareNoPermission:
		return FRTC_REMINDER_TYPE::REMINDER_SHARE_CONTENT_PROHIBITED;
	case kUplinkBitRateLimit:
		return FRTC_REMINDER_TYPE::REMINDER_LOW_UPLINK_BITRATE;
	default:
		return FRTC_REMINDER_TYPE::REMINDER_UNKNOW;
	}
}

void FrtcManager::set_reminder_type(FRTC_TIP_TYPE type)
{
	if (g_frtc_meeting_wnd != NULL && _ui_reminder_notify_callback)
	{
		FRTC_REMINDER_TYPE reminderType = get_reminder_type(type);
		_ui_reminder_notify_callback(reminderType);
	}
}

void FrtcManager::set_create_options(std::shared_ptr<CREATE_OPTIONS> create_options)
{
	_create_options = std::move(create_options);

	AutoLock autoLock(_msg_queue_lock);
	std::queue<FRTC_SDK_CALL_NOTIFICATION>().swap(_sc_msg_queue_temp);
}

BOOL FrtcManager::process_sc_msg_queue_msg()
{
	AutoLock autoLock(_msg_queue_lock);

	while (!_sc_msg_queue.empty())
	{
		auto notify = _sc_msg_queue.front();
		_sc_msg_queue.pop();

		InfoLog("handle message, msg=%s", notify._msg_name.c_str());

		if (!_is_processing_sc_msg && notify._msg_name != "CallDisconnected")
		{
			InfoLog("Ignoring message: %s", notify._msg_name.c_str());
			continue;
		}

		if (notify._msg_name == "ActiveSpeakerChanged")
		{
			_video_wnd_mgr->set_active_speaker(notify._target_id, false);
			update_video_windows();
		}
		else if (notify._msg_name == "AddAudioStream")
		{
			_apr_msid = notify._target_id;
			InfoLog("start audio render, msid=%s", notify._target_id.c_str());

			_audio_render->StartRender();
		}
		else if (notify._msg_name == "StopAudioStream")
		{
			_apt_msid = "";
			_is_sending_audio = false;
		}
		else if (notify._msg_name == "DeleteAudioStream")
		{
			_audio_render->StopRender();
			_apr_msid = "";
		}
		else if (notify._msg_name == "RequestAudioStream")
		{
			toggle_local_audio_mute(_is_local_audio_muted);
			_rtcInstance->SetNoiseBlock(_noise_blocker_enabled);
			_apt_msid = notify._target_id;
			_is_sending_audio = true;
		}
		else if (notify._msg_name == "CallConnected")
		{
			std::wstring wmeetingId = FRTCSDK::FRTCSdkUtil::string_to_wstring(_confrence_alias);

			if (g_frtc_meeting_wnd != NULL)
			{
				g_frtc_meeting_wnd->set_meeting_id(wmeetingId);
				g_frtc_meeting_wnd->start_statictisc_query_timer();
			}

			start_meeting_video();
			const video_device* pDev = _video_dev_manager.get_preferred_video_input();
			enable_camera(pDev != nullptr);
			toggle_local_video_mute(_is_local_video_muted);
			start_meeting_audio();
			toggle_local_audio_mute(_is_local_audio_muted);

			ShowWindow(_hMain, SW_SHOW);
			UpdateWindow(_hMain);
		}
		else if (notify._msg_name == "CallDisconnected")
		{
			clean_sc_msg_queue();
			end_meeting(0);
			return FALSE;
		}
		else if (notify._msg_name == "CallReconnected")
		{
			start_meeting_video();
			start_meeting_audio();
			toggle_local_audio_mute(_is_local_audio_muted);
		}
		else if (notify._msg_name == "CellCustomizeChanged")
		{
			_video_wnd_mgr->set_cell_customizations(notify._target_id);
			Json::Value jsonPin;
			jsonPin["uuid"] = notify._target_id;
			OnMeetingControlMsg("on_participant_pin_state_changed", jsonPin);
		}
		else if (notify._msg_name == "ContentStatusChanged")
		{
			if (g_frtc_meeting_wnd != NULL)
			{
				if ("startSharing" == notify._target_id)
				{
					if (!_is_sharing_content)
					{
						_video_wnd_mgr->setShareContent(true);
						_is_sharing_content = true;
						if (_content_sending_state_callback)
						{
							_content_sending_state_callback(true);
						}
					}
				}
				else
				{
					if (_is_sharing_content)
					{
						if (_content_sending_state_callback)
						{
							_content_sending_state_callback(false);
						}

						_video_wnd_mgr->setShareContent(false);
						_video_wnd_mgr->on_main_window_size_changed();
						_is_sharing_content = false;

						clear_content_sending_params();
					}
				}
			}
		}
		else if (notify._msg_name == "StopContentStream")
		{
			update_content_video_msid("", true, notify._target_id);

			if (_want_send_content_audio)
			{
				bool isSameDevice = is_same_speaker_device();
				toggle_content_auido(false, isSameDevice);
				_content_audio_capture->StopCapture();
			}
		}
		else if (notify._msg_name == "RequestContentStream")
		{
			if (_is_sharing_content)
			{
				bool useMinRate = (_average_cpu_load >= CPU_LOAD_HIGH);
				update_content_video_msid(notify._target_id);
				start_content_capture(useMinRate);
				_send_content_audio = _want_send_content_audio && !_is_local_audio_muted;
				start_send_content_audio(true);
			}
			else
			{
				InfoLog("Ignoring content video request while content is not sending");
			}
		}
		else if (notify._msg_name == "LayoutChanged")
		{
			RTC::LayoutDescription* layout = (RTC::LayoutDescription*)notify._param;
			handle_svc_layout_changed(*layout);
			delete layout;
		}
		else if (notify._msg_name == "ParticipantChanged")
		{
			if (get_call_state() == FRTC_CALL_STATE::CALL_CONNECTED && _hMain && IsWindow(_hMain))
			{
				OnMeetingControlMsg("on_participantslist_changed", get_participant_list());
			}
		}
		else if (notify._msg_name == "ParticipantStatusChange")
		{
			PFRTC_PARTI_STATUS_PARAM partiStatusNotify = (PFRTC_PARTI_STATUS_PARAM)notify._param;
			on_participant_mute_state_changed(partiStatusNotify->_parti_status_list, partiStatusNotify->_is_full_list);
			delete partiStatusNotify;
		}
		else if (notify._msg_name == "NotifyUICameraStatus")
		{
			Json::Value cameraState;
			cameraState["camera_state"] = !notify._is_hide_local_video;
			OnMeetingControlMsg("on_camera_state_changed", cameraState);
		}
		else if (notify._msg_name == "RecordingStreamingStatusChanged")
		{
			PFRTC_WATERMARK_PARAM waterMarkNotify = (PFRTC_WATERMARK_PARAM)notify._param;
			_video_wnd_mgr->set_watermark(waterMarkNotify->_enable_water_mark, waterMarkNotify->_water_mark_str);
			Json::Value jsonRS;
			jsonRS["recording_status"] = waterMarkNotify->_recording_status_str;
			jsonRS["live_status"] = waterMarkNotify->_live_status_str;
			jsonRS["live_url"] = waterMarkNotify->_live_meeting_url;
			jsonRS["live_pwd"] = waterMarkNotify->_live_meeting_pwd;
			OnMeetingControlMsg("on_recording_streaming_status_changed", jsonRS);
			delete waterMarkNotify;
		}
		else if (notify._msg_name == "SetGridLayoutMode")
		{
			_video_wnd_mgr->toggle_layout_grid_mode(notify._is_grid_layout_mode);
		}
		else if (notify._msg_name == "SetLocalVideoHideState")
		{
			_video_wnd_mgr->toggle_local_video_hidden(notify._is_hide_local_video);
		}
		else if (notify._msg_name == "StopContent")
		{
			stop_sending_content();
		}
		else if (notify._msg_name == "UpdateVideoWindows")
		{
			if (get_call_state() == FRTC_CALL_STATE::CALL_CONNECTED && _hMain && IsWindow(_hMain))
			{
				// TODO: Add your code here
			}
		}
		else if (notify._msg_name == "DetectVideoFreeze")
		{
			InfoLog("Stream %s frozen", notify._target_id.c_str());

			if (!_is_sharing_content)
			{
				_video_wnd_mgr->show_video_stream(notify._target_id, true);
			}
			update_video_windows();
		}
		else if (notify._msg_name == "AddVideoStream")
		{
			start_video_stream(notify);
		}
		else if (notify._msg_name == "StopVideoStream")
		{
			update_people_video_msid("");

			if (_max_camera_height > 0)
			{
				auto search = _msid_to_resolution_map.find(notify._target_id);
				if (search != _msid_to_resolution_map.end())
				{
					change_camera_resolution(search->second, false);
					_msid_to_resolution_map.erase(search);
				}
			}
		}
		else if (notify._msg_name == "DeleteVideoStream")
		{
			auto it = std::find_if(_layout_items_list.begin(), _layout_items_list.end(), [&](const RTC::LayoutCell& layout)
			{ return layout.msid == notify._target_id && layout.width < 0; });

			if (it == _layout_items_list.end())
			{
				stop_video_stream(notify);
			}
		}
		else if (notify._msg_name == "RequestVideoStream")
		{
			if (_is_local_video_muted)
			{
				toggle_local_video_mute(_is_local_video_muted);
			}
			update_people_video_msid(notify._target_id);

			PFRTC_VIDEO_STREAM_PARAM param = (PFRTC_VIDEO_STREAM_PARAM)notify._param;
			_video_capture.info_.bmiHeader.biWidth = param->_width;
			_video_capture.info_.bmiHeader.biHeight = param->_height;

			if (_max_camera_height > 0)
			{
				_msid_to_resolution_map.insert(std::pair<std::string, int>(notify._target_id, param->_height));
				change_camera_resolution(param->_height, true);
			}

			delete param;
		}

		InfoLog("handle message finish, msg=%s", notify._msg_name.c_str());
	}

	return TRUE;
}

void FrtcManager::mouse_event_callback(UINT message)
{
	if (g_frtc_meeting_wnd != NULL && _ui_window_mouse_event_callback)
	{
		static const std::map<UINT, FrtcMouseEvent> messageArgs = {
			{WM_LBUTTONDBLCLK, LMB_DBLCLICK},
			{WM_LBUTTONUP, LMB_UP},
			{WM_MOUSEHOVER, MOUSE_HOVER},
			{WM_MOUSELEAVE, MOUSE_LEAVE} };

		auto it = messageArgs.find(message);
		if (it != messageArgs.end())
		{
			_ui_window_mouse_event_callback(it->second);
		}
	}
}

void FrtcManager::report_mute_status()
{
	InfoLog("report mute status to server, local audio muted = %d, _is_audio_muted_by_server = %d, _is_local_video_muted = %d, _allow_unmute_audio_by_self = %d",
		_is_local_audio_muted, _is_audio_muted_by_server, _is_local_video_muted, _allow_unmute_audio_by_self);

	_rtcInstance->ReportMuteStatus(_is_local_audio_muted || _is_audio_muted_by_server, _is_local_video_muted, _allow_unmute_audio_by_self);
}

void FrtcManager::clear_params()
{
	clear_content_sending_params();

	_status = S_STOPPED;
	_guest_user_display_name = "";
}

void FrtcManager::clear_content_sending_params()
{
	update_content_video_msid("");
	stop_content_capture();

	_is_audio_muted_by_server = false;
	_allow_unmute_audio_by_self = true;

	_is_window_content = false;
}

void FrtcManager::clear_name_card()
{
	AutoLock autolock(_name_card_map_lock);
	for (auto it = _name_card_map.begin(); it != _name_card_map.end(); ++it)
	{
		delete[] it->second;
	}
	_name_card_map.clear();
}

void FrtcManager::on_monitor_list_changed()
{
	get_monitor_list();
}

void FrtcManager::set_video_data_zero_check_timer()
{
	std::string strLocalSourceID;
	get_local_preview_msid(strLocalSourceID);
	_video_wnd_mgr->set_video_data_zero_check_timer(strLocalSourceID);
}

bool FrtcManager::is_local_video_wnd_hidden()
{
	return _video_wnd_mgr->is_local_video_visible();
}

bool FrtcManager::is_switch_layout_enabled()
{
	return (g_frtc_meeting_wnd != NULL) && g_frtc_meeting_wnd->is_switch_layout_enabled();
}

void FrtcManager::toggle_name_card_visible(bool bVisible)
{
	_show_name_card = bVisible;
	_video_wnd_mgr->show_all_icon_window();
}

bool FrtcManager::is_namecard_visible()
{
	return _show_name_card;
}

void FrtcManager::stop_meeting_video()
{
	if (_status == S_STARTED)
	{
		_video_wnd_mgr->hide_video_window();
		stop_camera();
	}
}

void FrtcManager::start_meeting_audio()
{
	_people_audio_capture->SetAudioSink(this);
	_people_audio_capture->StartCapture();

	_audio_render->SetAudioSource(this);
}

void FrtcManager::stop_meeting_audio()
{
	_audio_render->StopRender();

	_people_audio_capture->StopCapture();
}

const std::string& FrtcManager::get_camera_device_list_jstr()
{
	const video_device_list& list = _video_dev_manager.get_video_device_list(TRUE);
	_camera_list = video_device_to_str(list);
	return _camera_list;
}

const video_device_list& FrtcManager::get_camera_devices(BOOL updateNow)
{
	return _video_dev_manager.get_video_device_list(updateNow);
}

void FrtcManager::set_camera_device(const TCHAR* deviceID)
{
	std::string strID = FRTCSDK::FRTCSdkUtil::wstring_to_string(deviceID);
	DebugLog("set preferred camera device=%s", strID.c_str());

	_video_dev_manager.set_preferred_video_input(deviceID);
}

void FrtcManager::notify_camera_device_reset()
{
	const TCHAR* wszId = get_current_camera_device();
	if (wszId)
	{
		std::wstring wsId(wszId);
		std::string strId = FRTCSDK::FRTCSdkUtil::wstring_to_string(wsId);

		Json::Value jsonDevice;
		jsonDevice["device_id"] = strId;
		OnMeetingControlMsg("on_camera_device_reset", jsonDevice);
	}
}

const std::string& FrtcManager::get_microphone_device_list_jstr()
{
	_people_audio_capture->UpdateDeviceList(FALSE);
	std::vector<DevInfo> listdev = _people_audio_capture->GetDeviceList();
	_microphone_list = audio_device_to_str(listdev);

	return _microphone_list;
}

void FrtcManager::get_microphone_devices(std::vector<DevInfo>& list)
{
	_people_audio_capture->UpdateDeviceList(false);
	list = _people_audio_capture->GetDeviceList();
}

void FrtcManager::set_microphone_device(const TCHAR* deviceID)
{
	DebugLog("set microphone device=%s", FRTCSDK::FRTCSdkUtil::wstring_to_string(deviceID).c_str());
	AutoLock _lock(_media_device_lock);
	GUID guid{ 0 };
	CLSIDFromString(deviceID, &guid);

	_people_audio_capture->SetDeviceId(guid);
	HRESULT hr = _people_audio_capture->ReInit(deviceID);
	if (FAILED(hr))
	{
		DebugLog("Failed to set microphone device 0x%x, trying to reset to default", hr);
		set_audio_device_os_default(FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_MIC, true);
		notify_audio_device_reset(FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_MIC);
	}
}

const std::string& FrtcManager::get_speaker_device_list_jstr()
{
	_audio_render->UpdateDeviceList(false);
	std::vector<DevInfo> DeviceList = _audio_render->GetDeviceList();
	_speaker_list = audio_device_to_str(DeviceList);
	return _speaker_list;
}

void FrtcManager::get_speaker_devices(std::vector<DevInfo>& list)
{
	_audio_render->UpdateDeviceList(false);
	list = _audio_render->GetDeviceList();
}

void FrtcManager::set_speaker_device(const TCHAR* deviceID)
{
	AutoLock _lock(_media_device_lock);
	GUID guid{ 0 };
	CLSIDFromString(deviceID, &guid);

	_audio_render->SetDeviceId(guid);
	HRESULT hr = _audio_render->Init((TCHAR*)deviceID);
	if (SUCCEEDED(hr))
	{
		init_content_audio_device();
	}
	else
	{
		DebugLog("_audio_render->Init failed, trying OS default device");
		_audio_render->SyncWithOS();
		_rtcInstance->ChangeAudioDevice(RTC::kSpeaker);
	}
}

void FrtcManager::notify_audio_device_reset(FRTC_MEDIA_DEVICE_TYPE type)
{
	if (_meeting_control_msg_callback)
	{
		std::string strType;
		std::string strId;
		if (type == FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_MIC)
		{
			strType = "mic";
			if (_using_os_default_mic_device)
			{
				strId = "os_default";
			}
			else
			{
				GUID guid{ 0 };
				get_current_mic_device(guid);
				FRTCSDK::FRTCSdkUtil::guid_to_string(guid, strId);
			}
		}
		else if (type == FRTC_MEDIA_DEVICE_TYPE::MEDIA_DEVICE_SPEAKER)
		{
			strType = "speaker";
			if (_using_os_default_speaker_device)
			{
				strId = "os_default";
			}
			else
			{
				GUID guid{ 0 };
				get_current_speaker_device(guid);
				FRTCSDK::FRTCSdkUtil::guid_to_string(guid, strId);
			}
		}

		Json::Value jsonDevice;
		jsonDevice["device_type"] = strType;
		jsonDevice["device_id"] = strId;
		OnMeetingControlMsg("on_audio_device_reset", jsonDevice);
	}
}

int FrtcManager::resize_yuv420(unsigned char* pSrc, int src_w, int src_h, unsigned char* pDst, int dst_w, int dst_h)
{
	return _rtcInstance->ScaleI420(pSrc, src_w, src_h, pDst, dst_w, dst_h);
}

const char* FrtcManager::get_meeting_statistics()
{
	_statistics_report_str = _rtcInstance->GetMediaStatistics();
	return _statistics_report_str.c_str();
}

const char* FrtcManager::get_app_uuid()
{
	return _uuid.c_str();
}

const char* FrtcManager::get_monitor_list()
{
	std::vector<FRTCSDK::DISPLAY_MONITOR_INFO> monitors;
	FRTCSDK::FRTCSdkUtil::get_monitor_list(monitors);

	Json::Value jsonMonitorsArray(Json::arrayValue);

	for (const auto& monitor : monitors)
	{
		Json::Value jsonMonitor;
		jsonMonitor["monitorName"] = FRTCSDK::FRTCSdkUtil::wstring_to_string(monitor.monitor_name);
		jsonMonitor["deviceName"] = FRTCSDK::FRTCSdkUtil::wstring_to_string(monitor.device_name);
		jsonMonitor["top"] = monitor.rect.top;
		jsonMonitor["left"] = monitor.rect.left;
		jsonMonitor["right"] = monitor.rect.right;
		jsonMonitor["bottom"] = monitor.rect.bottom;
		jsonMonitor["index"] = monitor.idx;
		jsonMonitor["handle"] = reinterpret_cast<UINT32>(monitor.monitor_handle);
		jsonMonitor["isPrimary"] = monitor.is_primary;
		jsonMonitorsArray.append(jsonMonitor);
	}

	Json::Value jsonMonitors;
	jsonMonitors["monitors"] = jsonMonitorsArray;

	Json::StreamWriterBuilder writerBuilder;
	writerBuilder["indentation"] = "";
	_monitor_list = Json::writeString(writerBuilder, jsonMonitors);

	return _monitor_list.c_str();
}

const char* FrtcManager::get_window_list()
{
	_frtc_content_capturer->get_content_source_windows_list(_windows_list, _create_options->ui_host_hwnd);
	return _windows_list.c_str();
}

const std::string& FrtcManager::get_statistics_jstr()
{
	_statistics_report_str = _rtcInstance->GetMediaStatistics();
	return _statistics_report_str;
}

std::string FrtcManager::audio_device_to_str(const std::vector<DevInfo>& device)
{
	Json::Value jsonDevices;
	Json::Value jsonDeviceArray(Json::arrayValue);

	for (const auto& devInfo : device)
	{
		if (!devInfo.first.empty())
		{
			Json::Value jsonDevice;
			std::wstring_convert<std::codecvt_utf8<TCHAR>, TCHAR> converter;
			jsonDevice["name"] = converter.to_bytes(devInfo.first);
			std::string stdGUID;
			FRTCSDK::FRTCSdkUtil::guid_to_string(devInfo.second, stdGUID);
			jsonDevice["id"] = stdGUID;
			AUDIO_DEVICE_TYPE type = get_audio_device_type_by_friendly_name(devInfo.first);
			jsonDevice["type"] = type;
			jsonDeviceArray.append(jsonDevice);
		}
	}

	jsonDevices["devices"] = jsonDeviceArray;

	Json::StreamWriterBuilder writerBuilder;
	std::string devices = Json::writeString(writerBuilder, jsonDevices);

	return devices;
}

AUDIO_DEVICE_TYPE FrtcManager::get_audio_device_type_by_friendly_name(const std::wstring& deviceName)
{
	HDEVINFO deviceInfo = NULL;
	SP_DEVINFO_DATA deviceInfoData;
	deviceInfoData.cbSize = sizeof(SP_DEVINFO_DATA);
	deviceInfo = SetupDiGetClassDevs(NULL, NULL, NULL, DIGCF_ALLCLASSES);
	if (deviceInfo == INVALID_HANDLE_VALUE)
	{
		return AUDIO_DEVICE_UNKWON;
	}

	TCHAR stProperty[256] = { 0 };
	BOOL ret;
	ULONG index = 0;
	AUDIO_DEVICE_TYPE type = AUDIO_DEVICE_UNKWON;

	while (ret = SetupDiEnumDeviceInfo(deviceInfo, index, &deviceInfoData))
	{
		ret = SetupDiGetDeviceRegistryProperty(deviceInfo,
			&deviceInfoData,
			SPDRP_CLASS,
			0,
			(PBYTE)stProperty,
			sizeof(stProperty),
			0);
		if (!(ret && (_tcscmp(stProperty, _T("MEDIA")) == 0)))
		{
			index++;
			continue;
		}

		ret = SetupDiGetDeviceRegistryProperty(deviceInfo,
			&deviceInfoData,
			SPDRP_SERVICE,
			0,
			(PBYTE)stProperty,
			sizeof(stProperty),
			0);
		if (!ret)
		{
			index++;
			continue;
		}

		if (_tcsncmp(_T("BthHFAud"), stProperty, sizeof(_T("BthHFAud"))) == 0)
		{
			type = AUDIO_DEVICE_BLUETOOTH;
		}
		else if (_tcsncmp(_T("usbaudio"), stProperty, sizeof(_T("usbaudio"))) == 0)
		{
			type = AUDIO_DEVICE_USB;
		}
		else
		{
			index++;
			continue;
		}

		ret = SetupDiGetDeviceRegistryProperty(deviceInfo,
			&deviceInfoData,
			SPDRP_FRIENDLYNAME,
			0,
			(PBYTE)stProperty,
			sizeof(stProperty),
			0);
		if (ret)
		{
			std::wstring strName = stProperty;
			if (strName.find(deviceName) != std::wstring::npos)
			{
				SetupDiDestroyDeviceInfoList(deviceInfo);
				DebugLog("Found audio device: %s", stProperty);
				return type;
			}
		}

		index++;
	}

	SetupDiDestroyDeviceInfoList(deviceInfo);

	WarnLog("audio device not found, device name=%s",
		FRTCSDK::FRTCSdkUtil::wstring_to_string(deviceName).c_str());

	return AUDIO_DEVICE_UNKWON;
}

std::string FrtcManager::video_device_to_str(const video_device_list& device)
{
	Json::Value jsonDevices;
	Json::Value jsonDeviceArray(Json::arrayValue);

	for (const auto& videoDevice : device)
	{
		if (videoDevice->name != nullptr)
		{
			Json::Value jsonDevice;
			std::wstring_convert<std::codecvt_utf8<TCHAR>, TCHAR> converter;
			jsonDevice["name"] = converter.to_bytes(videoDevice->name);
			jsonDevice["id"] = converter.to_bytes(videoDevice->id);
			jsonDeviceArray.append(jsonDevice);
		}
	}

	jsonDevices["devices"] = jsonDeviceArray;

	Json::StreamWriterBuilder writerBuilder;
	std::string devices = Json::writeString(writerBuilder, jsonDevices);

	return devices;
}

void FrtcManager::create_resolution_str(int height, int framerate, std::string& strResolution)
{
	strResolution.clear();
	if (height < 360)
	{
		strResolution = "180";
	}
	else if (height < 540)
	{
		strResolution = "360";
	}
	else if (height < 720)
	{
		strResolution = "540";
	}
	else if (height < 1080)
	{
		strResolution = "720";
	}
	else if (height >= 1080)
	{
		strResolution = "1080";
	}
	strResolution += "p";
	if (height > 180 && framerate < 30)
	{
		framerate = 30;
	}
	strResolution += std::to_string(framerate);
}

void FrtcManager::start_camera()
{
	std::string localID = "__local_preview_msid__";
	get_local_preview_msid(localID);
	clear_receive_video(localID);
	AutoLock _lock(_media_device_lock);
	const video_device* pDev = _video_dev_manager.get_preferred_video_input();
	if (!pDev)
	{
		ErrorLog("Failed to get preferred video input");
		enable_camera(false);
		_rtcInstance->SetCameraCapability("0p0");
		return;
	}

	_video_capture.set_capture_cap_by_cpu_level();
	if (_video_capture.set_camera_device(pDev) == S_FALSE ||
		_video_capture.start_preview() == S_FALSE)
	{
		ErrorLog("start camera preview failed");

		_rtcInstance->SetCameraCapability("0p0");
		enable_camera(false);
		show_camera_failed_reminder();
		update_self_video_status();

		return;
	}

	DebugLog("start camera preview success");

	_video_wnd_mgr->show_video_stream(localID, false);
	DebugLog("show camera stream success");

	update_video_windows();

	set_video_data_zero_check_timer();

	if (_camera_resolution_list.size() == 0)
	{
		_camera_resolution_list.push_back(0);
	}

	const VIDEO_CAPS_STRUCT* caps = _video_capture.get_video_capabilities();
	if (caps)
	{
		enable_camera(true);
		_max_camera_height = caps->height;
		std::string resolution;
		create_resolution_str(caps->height, caps->framerate, resolution);

		DebugLog("obtained video capabilities=%s", resolution.c_str());
		_rtcInstance->SetCameraCapability(resolution);
	}
	else
	{
		enable_camera(false);
		DebugLog("obtain video capabilities failed");

		_rtcInstance->SetCameraCapability("0p0");
	}
}

void FrtcManager::stop_camera(bool ShowMutePic)
{
	AutoLock _lock(_media_device_lock);
	std::string strLocal;
	get_local_preview_msid(strLocal);
	if (!ShowMutePic)
		_video_wnd_mgr->remove_video_stream(strLocal, false);
	else
		_video_wnd_mgr->show_video_stream(strLocal, true);

	update_video_windows();
	_video_capture.stop_preview();
}

void FrtcManager::start_video_stream(const FRTC_SDK_CALL_NOTIFICATION& callNotify)
{
	if (callNotify._target_id.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
	{
		if (g_frtc_meeting_wnd != nullptr)
		{
			g_frtc_meeting_wnd->set_switch_layout_enable(false);
		}

		Json::Value jsonContentReceiving;
		jsonContentReceiving["meeting_id"] = _meeting_id;
		jsonContentReceiving["content_receiving"] = true;
		OnMeetingControlMsg("on_content_receiving_state", jsonContentReceiving);
	}

	_video_wnd_mgr->show_video_stream(callNotify._target_id, false);
	update_video_windows();
}

void FrtcManager::stop_video_stream(const FRTC_SDK_CALL_NOTIFICATION& callNotify)
{
	if (callNotify._target_id.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
	{
		if (g_frtc_meeting_wnd != nullptr)
		{
			g_frtc_meeting_wnd->set_switch_layout_enable(true);
		}

		Json::Value jsonContentReceiving;
		jsonContentReceiving["meeting_id"] = _meeting_id;
		jsonContentReceiving["content_receiving"] = false;
		OnMeetingControlMsg("on_content_receiving_state", jsonContentReceiving);
	}

	_video_wnd_mgr->remove_video_stream(callNotify._target_id, false);
	update_video_windows();
}

BYTE* FrtcManager::get_video_mute_pic()
{
	return _local_camera_muted_pic_data_ptr;
}

void FrtcManager::get_receive_video(std::string& msid, void** buffer, unsigned int* length, unsigned int* width, unsigned int* height)
{
	_rtcInstance->ReceiveVideoFrame(msid, buffer, length, width, height);
}

void FrtcManager::clear_receive_video(std::string& msid)
{
	_rtcInstance->ResetVideoFrame(msid);
}

void FrtcManager::show_camera_failed_reminder()
{
	toggle_local_video_mute(true);
	set_reminder_type(kCameraOpenFail);
}

void FrtcManager::get_local_preview_msid(std::string& msid)
{
	_rtcInstance->GetLocalPreviewID(msid);
}

DWORD WINAPI FrtcManager::create_720p_name_card(LPVOID lpParame)
{
	NAME_CARD_PARAM* Tmpinfo = (NAME_CARD_PARAM*)lpParame;
	NAME_CARD_PARAM info;
	info._name = Tmpinfo->_name;
	info._width = Tmpinfo->_width;
	info._height = Tmpinfo->_height;
	info._namecard_type = Tmpinfo->_namecard_type;
	info._font_size_type = Tmpinfo->_font_size_type;
	info.frtc_mgr_ptr = Tmpinfo->frtc_mgr_ptr;

	InfoLog("create 720p name card=%s", info._name.c_str());

	BYTE* nameCard = FRTCSDK::FRTCSdkUtil::create_name_card(info._name,
		info._width,
		info._height,
		info._namecard_type,
		info._font_size_type);

	std::string str;
	switch (info._font_size_type)
	{
	case FRTCSDK::FONT_SIZE_BIG:
		str = "_FontBig";
		break;
	case FRTCSDK::FONT_SIZE_MEDIUM:
		str = "_FontMedium";
		break;
	case FRTCSDK::FONT_SIZE_SMALL:
		str = "_FontSmall";
		break;
	}

	std::string keyName = info._name + std::to_string(info._width) + std::to_string(info._height) + str;
	info.frtc_mgr_ptr->_name_card_map.emplace(keyName, nameCard);
	return 0;
}

DWORD WINAPI FrtcManager::create_720p_watermark(LPVOID lpParame)
{
	NAME_CARD_PARAM* Tmpinfo = (NAME_CARD_PARAM*)lpParame;
	NAME_CARD_PARAM info;
	info._name = Tmpinfo->_name;
	info._width = Tmpinfo->_width;
	info._height = Tmpinfo->_height;
	info.frtc_mgr_ptr = Tmpinfo->frtc_mgr_ptr;

	BYTE* nameCard = FRTCSDK::FRTCSdkUtil::create_water_mark(info._name, info._width, info._height);

	std::string keyName = std::to_string(info._width) + std::to_string(info._height) + "_contentWaterMark";
	info.frtc_mgr_ptr->_name_card_map[keyName] = nameCard;
	return 0;
}

BYTE* FrtcManager::get_name_card_yuv_data(std::string name,
	int w,
	int h,
	FRTCSDK::SDK_NAME_CARD_TYPE namecardType,
	FRTCSDK::SDK_NAME_CARD_FONT_SIZE_TYPE fontSizeType,
	int& newWidth)
{
	AutoLock autolock(_name_card_map_lock);

	std::string str;
	switch (fontSizeType)
	{
	case FRTCSDK::FONT_SIZE_BIG:
		str = "_FontBig";
		break;
	case FRTCSDK::FONT_SIZE_MEDIUM:
		str = "_FontMedium";
		break;
	case FRTCSDK::FONT_SIZE_SMALL:
		str = "_FontSmall";
		break;
	}

	std::string keyName = name + std::to_string(w) + std::to_string(h) + str;
	auto iter = _name_card_map.find(keyName);
	if (iter != _name_card_map.end())
	{
		return iter->second;
	}

	if (w == 1280 && h == 720 && namecardType == FRTCSDK::NAME_CARD_MEDIUM &&
		fontSizeType == FRTCSDK::FONT_SIZE_SMALL)
	{
		std::string keyNameTmp = name + std::to_string(320) + std::to_string(180) + "_FontTmp";
		auto iter = _name_card_map.find(keyNameTmp);
		if (iter != _name_card_map.end())
		{
			newWidth = 320;
			return iter->second;
		}

		BYTE* nameCard1 = FRTCSDK::FRTCSdkUtil::create_name_card(name,
			320,
			180,
			namecardType,
			FRTCSDK::FONT_SIZE_MEDIUM);
		_name_card_map.insert({ keyNameTmp, nameCard1 });

		AutoLock _lock(_namecard_lock);
		_name_card_info._name = name;
		_name_card_info._width = 1280;
		_name_card_info._height = 720;
		_name_card_info._namecard_type = namecardType;
		_name_card_info._font_size_type = fontSizeType;
		_name_card_info.frtc_mgr_ptr = this;

		std::thread(create_720p_name_card, &_name_card_info).detach();
		newWidth = 320;

		return nameCard1;
	}

	BYTE* nameCard = FRTCSDK::FRTCSdkUtil::create_name_card(name,
		w,
		h,
		namecardType,
		fontSizeType);
	_name_card_map.insert({ keyName, nameCard });

	return nameCard;
}

BYTE* FrtcManager::get_watermark_yuv_data(std::string msg, int w, int h, int& newWidth)
{
	AutoLock autolock(_name_card_map_lock);

	std::string str = "_contentWaterMark";

	std::string keyName = std::to_string(w) + std::to_string(h) + str;
	auto iter = _name_card_map.find(keyName);
	if (iter != _name_card_map.end())
	{
		return iter->second;
	}

	if (w >= 1280 && h >= 720)
	{
		std::string keyNameTmp = std::to_string(320) + std::to_string(180) + "_FontTmp";
		auto iter = _name_card_map.find(keyNameTmp);
		if (iter != _name_card_map.end())
		{
			newWidth = 320;
			return iter->second;
		}

		BYTE* watermark = FRTCSDK::FRTCSdkUtil::create_water_mark(msg, 320, 180);
		_name_card_map[keyNameTmp] = watermark;

		_name_card_info._name = msg;
		_name_card_info._width = w;
		_name_card_info._height = h;
		_name_card_info.frtc_mgr_ptr = this;

		std::thread(create_720p_watermark, &_name_card_info).detach();
		newWidth = 320;
		return watermark;
	}

	BYTE* watermark = FRTCSDK::FRTCSdkUtil::create_water_mark(msg, w, h);
	_name_card_map[keyName] = watermark;
	return watermark;
}

void FrtcManager::set_name_card_begin_time(time_t t)
{
	AutoLock autolock(_name_card_map_lock);
	_name_card_display_begin_time = t;
}

time_t FrtcManager::get_name_card_begin_time()
{
	return _name_card_display_begin_time;
}

CPU_LOAD_LEVEL FrtcManager::get_cpu_load_level()
{
	unsigned int usage = _rtcInstance->GetCPULoad();
	DebugLog("frtc CPU usage: %u", usage);

	if (usage >= CPU_LOAD_CRITICAL)
	{
		return CPU_LOAD_CRITICAL;
	}
	else if (usage >= CPU_LOAD_HIGH)
	{
		return CPU_LOAD_HIGH;
	}
	else
	{
		return CPU_LOAD_LOW;
	}
}

bool FrtcManager::check_video_data_zero(unsigned char* buffer, int nSize)
{
	for (int i = 0; i < nSize; i++)
	{
		if (buffer[i] != 0)
		{
			return false;
		}
	}

	return true;
}

unsigned int FrtcManager::camera_capture_callback(IMediaSample* pSample, void* context, CMediaType* pmt)
{
	static bool gpass = false;
	if (gpass)
		return 0;
	static unsigned int gcount = 0;

	DShowCapture* pCap = (DShowCapture*)context;

	static unsigned int prevtime_us;
	static long long prevtimestamp;

	unsigned int starttime_us = FRTCSDK::FRTCSdkUtil::timestamp();

	static long long starttimestamp, endtimestamp;
	pSample->GetTime(&starttimestamp, &endtimestamp);

	prevtimestamp = starttimestamp;
	prevtime_us = starttime_us;
	gcount++;

	if (pCap->_frame_count < 6 || pCap->_frame_count % 200 == 0)
	{
		DebugLog("camera capture name=%s, frame count=%d",
			pCap->_capture_state._capture_name.name,
			pCap->_frame_count);
	}

	AM_MEDIA_TYPE* pSampleMt = NULL;
	if (pCap->_video_filter_graph.pCapConfig && pCap->_video_filter_graph.pMtFromDevice)
	{
		pSampleMt = pCap->_video_filter_graph.pMtFromDevice;
	}

	if (pSampleMt && pSampleMt->lSampleSize <= pSample->GetActualDataLength())
	{
		if (pSampleMt->majortype != MEDIATYPE_Video)
		{
			ErrorLog("%s: pSampleMt->majortype != MEDIATYPE_Video",
				pCap->_capture_state._capture_name.name);
			return 1;
		}
		VIDEOINFO* pvi = (VIDEOINFO*)pSampleMt->pbFormat;
		int len = abs(pvi->bmiHeader.biWidth * pvi->bmiHeader.biHeight * 16 / 10);
		if (MEDIASUBTYPE_MJPG == pSampleMt->subtype)
		{
			len = abs(pvi->bmiHeader.biWidth * pvi->bmiHeader.biHeight * 41 / 10);
		}
		std::string localPreviewID;
		_rtcInstance->GetLocalPreviewID(localPreviewID);

		if (pCap->_frame_count == 1)
		{
			DebugLog("get first frame from camera, width=%ld, height=%ld, "
				"length=%ld, format=%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x",
				pvi->bmiHeader.biWidth, pvi->bmiHeader.biHeight,
				pSample->GetActualDataLength(), pSampleMt->subtype.Data1,
				pSampleMt->subtype.Data2, pSampleMt->subtype.Data3,
				pSampleMt->subtype.Data4[0], pSampleMt->subtype.Data4[1],
				pSampleMt->subtype.Data4[2], pSampleMt->subtype.Data4[3],
				pSampleMt->subtype.Data4[4], pSampleMt->subtype.Data4[5],
				pSampleMt->subtype.Data4[6], pSampleMt->subtype.Data4[7]);
		}

		unsigned char* buffer = NULL;
		LONG nSize = 0;
		RTC::VideoColorFormat colorFormat = RTC::kNoType;
		pSample->GetPointer(&buffer);
		nSize = pSample->GetActualDataLength();

		if (check_video_data_zero(buffer, nSize))
		{
			std::string localID = "__local_preview_msid__";
			get_local_preview_msid(localID);
			clear_receive_video(localID);
			DebugLog("Get zero data from camera");

			return 0;
		}

		if (MEDIASUBTYPE_I420 == pSampleMt->subtype)
		{
			colorFormat = RTC::kI420;
		}
		else if (MEDIASUBTYPE_RGB24 == pSampleMt->subtype)
		{
			int newSize = pvi->bmiHeader.biWidth * pvi->bmiHeader.biHeight * 3 / 2;
			unsigned char* cbuffer = get_media_convert_buffer(newSize);
			FRTCSDK::FRTCSdkUtil::rgb24_to_i420(buffer, pvi->bmiHeader.biWidth, pvi->bmiHeader.biHeight, cbuffer);
			colorFormat = RTC::kI420;
			buffer = cbuffer;
			nSize = newSize;
		}
		else if (MEDIASUBTYPE_YUY2 == pSampleMt->subtype)
		{
			if (pCap->_frame_count == 1)
			{
				DebugLog("get YUY2 data length=%ld, width=%ld, height=%ld",
					nSize, pvi->bmiHeader.biWidth,
					pvi->bmiHeader.biHeight);
			}
			colorFormat = RTC::kYUY2;
		}
		else if (MEDIASUBTYPE_NV12 == pSampleMt->subtype)
		{
			colorFormat = RTC::kNV12;
		}
		else
		{
			// Handle other media subtypes if needed
		}

		if (colorFormat != RTC::kNoType)
		{
			send_people_video_frame(buffer, nSize, pvi->bmiHeader.biWidth, pvi->bmiHeader.biHeight, colorFormat);
			_rtcInstance->SendVideoFrame(localPreviewID, buffer, nSize, pvi->bmiHeader.biWidth, pvi->bmiHeader.biHeight, colorFormat);
		}
	}
	else
	{
		InfoLog("%s: media subtype does not exist",
			pCap->_capture_state._capture_name.name);
		if (pSampleMt)
		{
			InfoLog("%s: the media subtype is %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x",
				pCap->_capture_state._capture_name.name,
				pSampleMt->subtype.Data1, pSampleMt->subtype.Data2,
				pSampleMt->subtype.Data3, pSampleMt->subtype.Data4[0],
				pSampleMt->subtype.Data4[1], pSampleMt->subtype.Data4[2],
				pSampleMt->subtype.Data4[3], pSampleMt->subtype.Data4[4],
				pSampleMt->subtype.Data4[5], pSampleMt->subtype.Data4[6],
				pSampleMt->subtype.Data4[7]);
		}
		return 1;
	}

	pCap->_frame_count++;

	return 0;
}

void FrtcManager::start_meeting_video()
{
	if (_status == S_CREATE || _status == S_DESTROY)
		return;

	_video_wnd_mgr->show_video_window();
	update_video_windows();
}

void FrtcManager::change_camera_resolution(int h, bool isRequest)
{
	VIDEO_CAPS_STRUCT newCap;
	bool needRestartCamera = false;

	DebugLog("change camera resolution, height=%d, request=%d, camera resolution "
		"list size=%d",
		h, isRequest, _camera_resolution_list.size());

	if (isRequest)
	{
		if (_camera_resolution_list.empty())
		{
			DebugLog("ERROR! _camera_resolution_list is empty!");
			return;
		}

		auto it = std::lower_bound(_camera_resolution_list.begin(), _camera_resolution_list.end(), h);
		if (it == _camera_resolution_list.begin() || h != *it)
		{
			it = _camera_resolution_list.insert(it, h);
			DebugLog("add requested resolution=%dp", h);
		}
		else
		{
			DebugLog("No need to change. Requested resolution already added: %dp", h);
		}

		if (it == _camera_resolution_list.begin())
		{
			newCap.height = h;
			switch (newCap.height)
			{
			case 1080:
				newCap.width = 1920;
				break;
			case 720:
			case 540:
				newCap.width = 1280;
				newCap.height = 720;
				break;
			case 360:
			case 180:
				newCap.width = 640;
				newCap.height = 360;
				break;
			default:
				DebugLog("Unknown resolution: %dp, resetting to maximum supported resolution", newCap.height);
				newCap.height = _max_camera_height;
				newCap.width = _max_camera_height * 16 / 9;
			}

			newCap.framerate = 30;
			if (_camera_resolution_height < newCap.height)
			{
				_video_capture.set_video_capabilities(newCap);
				needRestartCamera = true;
				_camera_resolution_height = newCap.height;
				DebugLog("resolution change required (request). new resolution=%dp",
					newCap.height);
			}
		}
	}
	else
	{
		if (_camera_resolution_list.empty())
		{
			DebugLog("ERROR! _camera_resolution_list is empty!");
			return;
		}

		auto it = std::find(_camera_resolution_list.begin(), _camera_resolution_list.end(), h);
		if (it != _camera_resolution_list.end())
		{
			_camera_resolution_list.erase(it);
			DebugLog("erase resolution=%dp", h);
		}
		else
		{
			DebugLog("no need to change. resolution %dp not found", h);
		}

		if (_camera_resolution_list.empty())
		{
			newCap.height = 0;
			newCap.width = 0;
		}
		else
		{
			newCap.height = _camera_resolution_list.front();
			switch (newCap.height)
			{
			case 720:
			case 540:
				newCap.width = 1280;
				newCap.height = 720;
				break;
			case 360:
			case 180:
				newCap.width = 640;
				newCap.height = 360;
				break;
			default:
				DebugLog("unknown resolution %dp, reset to max supported %dp",
					newCap.height, _max_camera_height);

				newCap.height = _max_camera_height;
				newCap.width = _max_camera_height * 16 / 9;
			}
		}

		newCap.framerate = 30;
		if (_camera_resolution_height > newCap.height)
		{
			_camera_resolution_height = newCap.height;
			if (newCap.height == 0)
			{
				newCap.height = _max_camera_height;
				newCap.width = _max_camera_height * 16 / 9;

				DebugLog("set local preview resolution=%dp", _max_camera_height);
			}

			_video_capture.set_video_capabilities(newCap);
			needRestartCamera = true;

			DebugLog("resolution change required (release). new resolution=%dp",
				newCap.height);
		}
	}

	if (needRestartCamera && !_is_local_video_muted && !_is_restart_camera_timer_set)
	{
		::SetTimer(_hMain, IDT_RESTART_CAMERA, 1500, NULL);
		_is_restart_camera_timer_set = true;
	}
}

void FrtcManager::restart_camera()
{
	_video_capture.stop_preview();

	const video_device* pDev = _video_dev_manager.get_preferred_video_input();
	if (!pDev)
	{
		enable_camera(false);
		ErrorLog("Failed to get preferred video input");
	}
	else
	{
		if (_video_capture.set_camera_device(pDev) == S_FALSE || _video_capture.start_preview() == S_FALSE)
		{
			enable_camera(false);
			DebugLog("Failed to restart camera");

			std::string localID = "__local_preview_msid__";
			get_local_preview_msid(localID);
			clear_receive_video(localID);
			show_camera_failed_reminder();
		}
		else
		{
			enable_camera(true);
			set_video_data_zero_check_timer();

			DebugLog("Camera restarted successfully. New resolution: %dp", _camera_resolution_height);
		}
	}

	::KillTimer(_hMain, IDT_RESTART_CAMERA);
	_is_restart_camera_timer_set = false;
}

void FrtcManager::enable_camera(bool enable)
{
	if (!enable)
	{
		std::string strLocal;
		get_local_preview_msid(strLocal);
		_video_wnd_mgr->show_video_stream(strLocal, true);
	}
}

void FrtcManager::set_camera_quality_preference(std::string& preference)
{
	std::string tmp(preference);
	std::transform(tmp.begin(), tmp.end(), tmp.begin(),
		[](unsigned char c)
	{ return tolower(c); });
	if (tmp == "framerate")
	{
		_video_capture.set_camera_quality_preference(VIDEO_QUALITY_MOTION);
		DebugLog("camera quality prefer frame rate");
	}
	else if (tmp == "resolution")
	{
		_video_capture.set_camera_quality_preference(VIDEO_QUALITY_SHARPNESS);
		DebugLog("camera quality prefer resolution");
	}
	else
	{
		ErrorLog("unrecognized preference type=%s", preference.c_str());
	}
}

void FrtcManager::update_people_video_msid(const std::string& msid)
{
	AutoLock autoLock(_vpt_msid_lock);
	_vpt_msid = msid;
}

void FrtcManager::update_content_video_msid(const std::string& msid, bool checkEqual, const std::string& checkId)
{
	AutoLock autoLock(_vcs_msid_lock);
	if (!checkEqual || _vcs_msid == checkId)
	{
		if (_vcs_msid != msid)
		{
			_vcs_msid = msid;
			_is_sending_content = !msid.empty();
		}
	}
}

void FrtcManager::send_people_video_frame(void* buffer, unsigned int length, unsigned int width, unsigned int height, RTC::VideoColorFormat type)
{
	AutoLock autolock(_vpt_msid_lock);
	if (!_vpt_msid.empty())
	{
		_rtcInstance->SendVideoFrame(_vpt_msid, buffer, length, width, height, type);
	}
}

void FrtcManager::send_content_video_frame(void* buffer, unsigned int length, unsigned int width, unsigned int height, RTC::VideoColorFormat type)
{
	AutoLock autolock(_vcs_msid_lock);
	if (!_vcs_msid.empty())
	{
		_rtcInstance->SendVideoFrame(_vcs_msid, buffer, length, width, height, type);
	}
}

// log upload
uint64_t FrtcManager::start_upload_logs(const std::string& metaData,
	const std::string& fileName,
	int fileCount,
	const std::string& coreDumpName)
{
	std::string utf8Str = FRTCSDK::FRTCSdkUtil::get_utf8_string(metaData);
	return _rtcInstance->StartUploadLogs(utf8Str, fileName, fileCount, coreDumpName);
}

int FrtcManager::get_log_update_status(uint64_t tractionId, int fileType, int* speed)
{
	std::string status = _rtcInstance->GetUploadStatus(tractionId, fileType);

	int progress = 0;
	*speed = 0;

	try
	{
		Json::Value jsonStatus;
		Json::Reader reader;
		if (reader.parse(status, jsonStatus))
		{
			progress = jsonStatus["progress"].asInt();
			*speed = jsonStatus["bitrate"].asInt();
		}
	}
	catch (const std::exception& e)
	{
		// Handle JSON parsing error
		DebugLog("JSON parsing error: %s", e.what());
	}

	return progress;
}

void FrtcManager::cancel_log_update(uint64_t tractionId)
{
	return _rtcInstance->CancelUploadLogs(tractionId);
}
