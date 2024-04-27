#pragma once

#include "json/json.h"
#include "rtc_interface.h"
#include "event_processor.h"

typedef struct
{
    std::string meeting_name;
    std::string meeting_id;
    std::string display_name;
    std::string owner_id;
    std::string owner_name;
    std::string meeting_url;
    std::string group_meeting_url;
    long long start_time;
    long long end_time;
} MeetingInfo;

typedef struct _FRTC_PARTI_STATUS_PARAM
{
    bool _is_full_list;
    std::map<std::string, RTC::ParticipantStatus> _parti_status_list;
} FRTC_PARTI_STATUS_PARAM, *PFRTC_PARTI_STATUS_PARAM;

typedef struct _FRTC_VIDEO_STREAM_PARAM
{
    uint32_t _ssrc;
    int _width;
    int _height;
} FRTC_VIDEO_STREAM_PARAM, *PFRTC_VIDEO_STREAM_PARAM;

typedef struct _FRTC_SDK_CALL_NOTIFICATION
{
    std::string _msg_name;
    std::string _target_id;
    bool _is_grid_layout_mode;
    bool _is_hide_local_video;

    void *_param;
} FRTC_SDK_CALL_NOTIFICATION, *PFRTC_SDK_CALL_NOTIFICATION;

class RTCEventObserverCallback
{
public:
    virtual void OnMeetingJoinInfo(const std::shared_ptr<MeetingInfo> &meetingInfo) = 0;
    virtual void OnMeetingJoinFail(RTC::MeetingStatusChangeReason state) = 0;
    virtual void OnMeetingStatusChange(RTC::MeetingStatus state,
                                       int reason,
                                       const std::string &call_id) = 0;

    virtual void OnParticipantList(std::set<std::string> uuid_list) = 0;

    virtual void OnMeetingSessionStatus(
        const std::string &watermark_msg,
        const std::string &recording_status = "NOT_STARTED",
        const std::string &streaming_status = "NOT_STARTED",
        const std::string &streaming_url = "",
        const std::string &streaming_pwd = "") = 0;

    virtual void OnMuteLock(bool muted, bool allow_self_unmute) = 0;
    virtual void OnDetectAudioMute() = 0;

    virtual void OnContentFailForLowBandwidth() = 0;
    virtual void OnContentTokenResponse(bool rejected) = 0;

    virtual void OnLayoutSetting(int max_cell_count) = 0;

    virtual void OnPasscodeRequest() = 0;
    virtual void OnPasscodeReject(RTC::MeetingStatusChangeReason reason) = 0;

    virtual void OnCallNotification(FRTC_SDK_CALL_NOTIFICATION &callNotify) = 0;
    virtual void OnMeetingControlMsg(const std::string &msgType, const Json::Value &jsonMsg) = 0;
};

class RTCEventObserver : public RTC::RTCEventObserverInterface
{
public:
    RTCEventObserver(RTCEventObserverCallback *callback);
    virtual ~RTCEventObserver();

    void OnMeetingJoinInfo(const std::string &meeting_name,
                           const std::string &meeting_id,
                           const std::string &display_name,
                           const std::string &owner_id,
                           const std::string &owner_name,
                           const std::string &meeting_url,
                           const std::string &group_meeting_url,
                           long long start_time,
                           long long end_time);

    void OnMeetingJoinFail(RTC::MeetingStatusChangeReason reason);

    void OnMeetingStatusChange(RTC::MeetingStatus status,
                               int reason,
                               const std::string &call_id);

    void OnParticipantCount(int parti_count);

    void OnParticipantList(const std::set<std::string> &uuid_list);

    void OnParticipantStatusChange(const std::map<std::string, RTC::ParticipantStatus> &status_list,
                                   bool is_full);

    void OnTextOverlay(RTC::TextOverlay *overlay);

    void OnMeetingSessionStatus(const std::string &watermark_msg,
                                const std::string &recording_status = "NOT_STARTED",
                                const std::string &streaming_status = "NOT_STARTED",
                                const std::string &streaming_url = "",
                                const std::string &streaming_pwd = "");

    void OnUnmuteRequest(const std::map<std::string, std::string> &parti_list);

    void OnUnmuteAllow();

    void OnMuteLock(bool muted, bool allow_self_unmute);

    void OnContentStatusChange(RTC::ContentStatus status);

    void OnContentFailForLowBandwidth();

    void OnContentTokenResponse(bool rejected);

    void OnLayoutChange(const RTC::LayoutDescription &layout);

    void OnLayoutSetting(int max_cell_count,
                         const std::vector<std::string> &lectures);

    void OnPasscodeRequest();

    void OnPasscodeReject(RTC::MeetingStatusChangeReason reason);

    void OnRequestVideoStream(const std::string &msid,
                        int width,
                        int height,
                        float frame_rate);

    void OnAddVideoStream(const std::string &msid,
                        int width,
                        int height,
                        uint32_t ssrc);

    void OnStopVideoStream(const std::string &msid);

    void OnDeleteVideoStream(const std::string &msid);

    void OnDetectVideoFreeze(const std::string &msid, bool frozen);

    void OnRequestAudioStream(const std::string &msid);

    void OnAddAudioStream(const std::string &msid);

    void OnStopAudioStream(const std::string &msid);

    void OnDeleteAudioStream(const std::string &msid);

    void OnDetectAudioMute();

private:
    RTCEventObserverCallback *_callback;
    EventProcessor *_eventProcessor;
};
