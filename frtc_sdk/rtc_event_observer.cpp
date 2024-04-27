#include "stdafx.h"
#include "log.h"
#include "rtc_event_observer.h"


using namespace RTC;

#define CONTENT_SEND_MISD_PREFIX "VCS"
#define CONTENT_MSID_PREFIX_LEN 3

RTCEventObserver::RTCEventObserver(RTCEventObserverCallback *callback)
	: _callback(callback)
{
	_eventProcessor = new EventProcessor("RTCEventObserver");
	_eventProcessor->start();
}

RTCEventObserver::~RTCEventObserver()
{
	_eventProcessor->stop();
	delete _eventProcessor;
}

void RTCEventObserver::OnMeetingJoinInfo(const std::string &meeting_name,
										 const std::string &meeting_id,
										 const std::string &display_name,
										 const std::string &owner_id,
										 const std::string &owner_name,
										 const std::string &meeting_url,
										 const std::string &group_meeting_url,
										 const long long start_time,
										 const long long end_time)
{
	std::shared_ptr<MeetingInfo> meeting_info = std::make_shared<MeetingInfo>();
	meeting_info->meeting_name = meeting_name;
	meeting_info->meeting_id = meeting_id;
	meeting_info->display_name = display_name;
	meeting_info->owner_id = owner_id;
	meeting_info->owner_name = owner_name;
	meeting_info->meeting_url = meeting_url;
	meeting_info->group_meeting_url = group_meeting_url;
	meeting_info->start_time = start_time;
	meeting_info->end_time = end_time;

	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingJoinInfo, _callback, meeting_info);
}

void RTCEventObserver::OnMeetingJoinFail(MeetingStatusChangeReason reason)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingJoinFail, _callback, reason);
}

void RTCEventObserver::OnMeetingStatusChange(MeetingStatus status,
											 int reason,
											 const std::string &call_id)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingStatusChange, _callback, status, reason, call_id);
}

void RTCEventObserver::OnParticipantCount(int parti_count)
{
	Json::Value jsonMsg;
	jsonMsg["participant_num"] = parti_count;

	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingControlMsg, _callback, "on_participantsnum_changed", jsonMsg);
}

void RTCEventObserver::OnParticipantList(const std::set<std::string> &uuid_list)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnParticipantList, _callback, uuid_list);
}

void RTCEventObserver::OnParticipantStatusChange(
	const std::map<std::string, RTC::ParticipantStatus> &status_list, bool is_full)
{
	PFRTC_PARTI_STATUS_PARAM statusParam = new FRTC_PARTI_STATUS_PARAM();
	statusParam->_is_full_list = is_full;
	for (auto &status : status_list)
	{
		statusParam->_parti_status_list[status.first] = status.second;
	}

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = "ParticipantStatusChange";
	callNotify._param = statusParam;

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnTextOverlay(RTC::TextOverlay *overlay)
{
	Json::Value jsonMsg;
	jsonMsg["enabled"] = overlay->enabled;
	jsonMsg["text"] = overlay->text;
	jsonMsg["font"] = overlay->font;
	jsonMsg["fontSize"] = overlay->font_size;
	jsonMsg["color"] = overlay->color;
	jsonMsg["verticalPosition"] = overlay->vertical_position;
	jsonMsg["backgroundTransparency"] = overlay->background_transparency;
	jsonMsg["displaySpeed"] = overlay->display_speed;
	jsonMsg["displayRepetition"] = overlay->display_repetition;
	jsonMsg["type"] = overlay->text_overlay_type;

	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingControlMsg, _callback, "on_text_overlay_received", jsonMsg);
}

void RTCEventObserver::OnMeetingSessionStatus(const std::string &watermark_msg,
											  const std::string &recording_status,
											  const std::string &streaming_status,
											  const std::string &streaming_url,
											  const std::string &streaming_pwd)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingSessionStatus,
						  _callback,
						  watermark_msg,
						  recording_status,
						  streaming_status,
						  streaming_url,
						  streaming_pwd);
}

void RTCEventObserver::OnUnmuteRequest(const std::map<std::string, std::string> &parti_list)
{
	if (parti_list.empty())
		return;

	Json::Value jsonMsg;
	Json::Value jsonList(Json::arrayValue);

	for (const auto &participant : parti_list)
	{
		Json::Value jsonParti;
		jsonParti["id"] = participant.first;
		jsonParti["name"] = participant.second;
		jsonList.append(jsonParti);
	}

	jsonMsg["partiList"] = jsonList;

	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingControlMsg, _callback, "on_receive_unmute_applications", jsonMsg);
}

void RTCEventObserver::OnUnmuteAllow()
{
	Json::Value jsonMsg;
	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingControlMsg, _callback, "on_receive_unmute_approved", jsonMsg);
}

void RTCEventObserver::OnMuteLock(bool muted, bool allow_self_unmute)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnMuteLock, _callback, muted, allow_self_unmute);
}

void RTCEventObserver::OnContentStatusChange(ContentStatus status)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	if (status == RTC::ContentStatus::kContentSending)
		callNotify._target_id = "startSharing";
	else
		callNotify._target_id = "stopSharing";
	callNotify._msg_name = "ContentStatusChanged";

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnContentFailForLowBandwidth()
{
	_eventProcessor->post(&RTCEventObserverCallback::OnContentFailForLowBandwidth, _callback);
}

void RTCEventObserver::OnContentTokenResponse(bool rejected)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnContentTokenResponse, _callback, rejected);
}

void RTCEventObserver::OnLayoutChange(const LayoutDescription &layout)
{
	if (!layout.active_speaker_uuid.empty())
	{
		DebugLog("active speaker changed, uuid=%s", layout.active_speaker_uuid.c_str());

		FRTC_SDK_CALL_NOTIFICATION callNotify1;
		callNotify1._target_id = layout.active_speaker_uuid;
		callNotify1._msg_name = "ActiveSpeakerChanged";
		_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify1);
	}

	if (!layout.pin_speaker_uuid.empty())
    {
		DebugLog("pin speaker changed, uuid=%s, all msid count is %d", layout.pin_speaker_uuid.c_str(), layout.layout_cells.size());
    }

	{
		FRTC_SDK_CALL_NOTIFICATION callNotify2;
		callNotify2._target_id = layout.pin_speaker_uuid;
		callNotify2._msg_name = "CellCustomizeChanged";
		_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify2);
	}

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	RTC::LayoutDescription *layoutParam = new RTC::LayoutDescription(layout);
	*layoutParam = layout;
	callNotify._msg_name = "LayoutChanged";
	callNotify._param = layoutParam;
	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnLayoutSetting(int max_cell_count, const std::vector<std::string> &lectures)
{
	DebugLog("OnLayoutSetting msg received, max_cell_count is %d, lectures count is %d", max_cell_count, lectures.size());

	_eventProcessor->post(&RTCEventObserverCallback::OnLayoutSetting, _callback, max_cell_count);

	Json::Value jsonMsg;
	Json::Value jsonLectureList(Json::arrayValue);

	for (const std::string &lecture : lectures)
	{
		jsonLectureList.append(lecture);
	}

	jsonMsg["lectures"] = jsonLectureList;

	_eventProcessor->post(&RTCEventObserverCallback::OnMeetingControlMsg, _callback, "on_lecturers_changed", jsonMsg);
}

void RTCEventObserver::OnPasscodeRequest()
{
	_eventProcessor->post(&RTCEventObserverCallback::OnPasscodeRequest, _callback);
}

void RTCEventObserver::OnPasscodeReject(MeetingStatusChangeReason reason)
{
	_eventProcessor->post(&RTCEventObserverCallback::OnPasscodeReject, _callback, reason);
}

void RTCEventObserver::OnRequestVideoStream(const std::string &msid, int width, int height, float frame_rate)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	if (msid.substr(0, CONTENT_MSID_PREFIX_LEN) == CONTENT_SEND_MISD_PREFIX)
	{
		callNotify._msg_name = "RequestContentStream";
	}
	else
	{
		PFRTC_VIDEO_STREAM_PARAM videoParam = new FRTC_VIDEO_STREAM_PARAM();
		videoParam->_width = width;
		videoParam->_height = height;

		callNotify._msg_name = "RequestVideoStream";
		callNotify._param = videoParam;
	}

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnStopVideoStream(const std::string &msid)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	if (msid.substr(0, CONTENT_MSID_PREFIX_LEN) == CONTENT_SEND_MISD_PREFIX)
	{
		callNotify._msg_name = "StopContentStream";
	}
	else
	{
		callNotify._msg_name = "StopVideoStream";
	}

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnAddVideoStream(const std::string &msid,
									  int width,
									  int height,
									  uint32_t ssrc)
{
	PFRTC_VIDEO_STREAM_PARAM videoParam = new FRTC_VIDEO_STREAM_PARAM();
	videoParam->_ssrc = ssrc;
	videoParam->_width = width;
	videoParam->_height = height;

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	callNotify._msg_name = "AddVideoStream";
	callNotify._param = videoParam;

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnDeleteVideoStream(const std::string &msid)
{
	PFRTC_VIDEO_STREAM_PARAM videoParam = new FRTC_VIDEO_STREAM_PARAM();
	videoParam->_ssrc = 0;
	videoParam->_width = 0;
	videoParam->_height = 0;

	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	callNotify._param = videoParam;
	callNotify._msg_name = "DeleteVideoStream";

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnDetectVideoFreeze(const std::string &msid, bool frozen)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._msg_name = frozen ? "DetectVideoFreeze" : "DetectVideoUnfreeze";
	callNotify._target_id = msid;

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnRequestAudioStream(const std::string &msid)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	callNotify._msg_name = "RequestAudioStream";

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnStopAudioStream(const std::string &msid)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	callNotify._msg_name = "StopAudioStream";

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnAddAudioStream(const std::string &msid)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	callNotify._msg_name = "AddAudioStream";

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnDeleteAudioStream(const std::string &msid)
{
	FRTC_SDK_CALL_NOTIFICATION callNotify;
	callNotify._target_id = msid;
	callNotify._msg_name = "DeleteAudioStream";

	_eventProcessor->post(&RTCEventObserverCallback::OnCallNotification, _callback, callNotify);
}

void RTCEventObserver::OnDetectAudioMute()
{
	_eventProcessor->post(&RTCEventObserverCallback::OnDetectAudioMute, _callback);
}
