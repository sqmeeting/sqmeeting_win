#pragma once
#include "stdafx.h"

#include <atomic>
#include <queue>
#include <memory>

#include "rtc_interface.h"
#include "auto_lock.h"
#include "CameraDevice/DShowCapture.h"
#include "CameraDevice/video_device_manager.h"
#include "audio_capture.h"
#include "audio_device_monitor.h"
#include "audio_render.h"
#include "frtc_typedef.h"
#include "frtc_sdk_util.h"
#include "FRTCBaseWnd.h"
#include "rtc_event_observer.h"
#include "FrtcVideoWndMgr.h"
#include "ReconnectHelper.h"
#include "frtc_content_capturer.h"

typedef struct _GUEST_MAKE_CALL_PARAMS
{
public:
    std::string _server_addr;
    std::string _meeting_number;
    std::string _user_display_name;
    std::string _meeting_pwd;
    int _call_rate;
} GUEST_MAKE_CALL_PARAMS, *PGUEST_MAKE_CALL_PARAMS;

typedef struct _SIGNED_USER_MAKE_CALL_PARAMS
{
public:
    std::string _server_address;
    std::string _meeting_number;
    std::string _display_name;
    std::string _user_token;
    std::string _meeting_pwd;
    int _call_rate;
} SIGNED_USER_MAKE_CALL_PARAMS, *PSIGNED_USER_MAKE_CALL_PARAMS;

typedef enum _AUDIO_DEVICE_TYPE
{
    AUDIO_DEVICE_UNKWON = 0,
    AUDIO_DEVICE_INTEGRATED = 1,
    AUDIO_DEVICE_USB = 2,
    AUDIO_DEVICE_BLUETOOTH = 3,
    AUDIO_DEVICE_HDMI = 4,
} AUDIO_DEVICE_TYPE;

typedef enum _CPU_LOAD_LEVEL
{
    CPU_LOAD_LOW = 0,
    CPU_LOAD_HIGH = 70,
    CPU_LOAD_CRITICAL = 80
} CPU_LOAD_LEVEL;

typedef struct _NAME_CARD_PARAM
{
    std::string _name;
    int _width;
    int _height;
    FRTCSDK::SDK_NAME_CARD_TYPE _namecard_type;
    FRTCSDK::SDK_NAME_CARD_FONT_SIZE_TYPE _font_size_type;
    FrtcManager *frtc_mgr_ptr;
} NAME_CARD_PARAM, *PNAME_CARD_PARAM;

typedef struct _FRTC_WATERMARK_PARAM
{
    bool _enable_water_mark;
    std::string _water_mark_str;
    std::string _recording_status_str;
    std::string _live_status_str;
    std::string _live_meeting_url;
    std::string _live_meeting_pwd;
} FRTC_WATERMARK_PARAM, *PFRTC_WATERMARK_PARAM;

class FrtcManager : public RTCEventObserverCallback,
                    public IAudioDataSink,
                    public IAudioDataSource,
                    public IWebRTCCaptureCallback,
                    public interface_frtc_capture_content_callback
{
public:
    FrtcManager(void);
    virtual ~FrtcManager(void);

    BOOL init(const std::string &uuid);
    BOOL uninit();

    const char *get_rtc_version();

    // meeting and media control
    void join_meeting(const FrtcCallParam &param);
    void reconnect_current_meeting(const FrtcCallParam &param);
    void notify_meeting_reconnect_state(ReconnectState state);
    void join_meeting_logged_in(const std::string &server_address, const std::string &conferenceAlias, const std::string &userName, const std::string &user_token, const std::string &meeting_pwd, int call_rate);
    void join_meeting_guest(const std::string &server_address, const std::string &meeting_number, const std::string &user_display_name, int call_rate, const std::string &meeting_pwd);
    void verify_password(const char *passCode);
    void end_meeting(int callIndex);
    bool is_audio_only();
    bool is_local_audio_muted();
    bool is_local_only();
    bool is_local_video_muted();
    void set_layout_grid_mode(bool gridMode);
    void set_mute_state(bool mute);
    void toggle_camera_mirroring(bool mirror);
    void toggle_content_auido(bool enable, bool isSameDevice);
    void toggle_local_audio_mute(bool mute);
    void toggle_local_video_mute(bool mute);
    void toggle_local_video_wnd_hide(bool hide);
    void toggle_noise_block(bool enable);

    // rtc event callback
    void OnPasscodeRequest();
    void OnPasscodeReject(RTC::MeetingStatusChangeReason reason);
    void OnContentTokenResponse(bool rejected);
    void OnContentFailForLowBandwidth();
    void OnMeetingJoinInfo(const std::shared_ptr<MeetingInfo> &meetingInfo);
    void OnMeetingJoinFail(RTC::MeetingStatusChangeReason state);
    void OnMeetingStatusChange(RTC::MeetingStatus state, int reason, const std::string &callId);
    void OnParticipantList(std::set<std::string> uuidList);
    void OnDetectAudioMute();
    void OnMuteLock(bool muted, bool allowSelfUnmute);
    void OnLayoutSetting(int max_cell_count, const std::vector<std::string>& lectures);
    void OnMeetingSessionStatus(
        const std::string &watermark_msg,
        const std::string &recording_status,
        const std::string &streaming_status,
        const std::string &streaming_url,
        const std::string &streaming_pwd);
    void OnCallNotification(FRTC_SDK_CALL_NOTIFICATION &callNotify);
    void OnMeetingControlMsg(const std::string &msgType, const Json::Value &jsonMsg);

    // audio callbacks
    DWORD OnWriteData(LPVOID buff, DWORD len, DWORD sampleRate);
    DWORD OnReadData(LPVOID buff, DWORD len, DWORD sampleRate);
    void OnError(DWORD error);
    DWORD OnWriteDataContent(LPVOID buff, DWORD len, DWORD sampleRate);
    DWORD OnAudioDeviceStateChanged();
    DWORD OnDefaultAudioChanged();
    DWORD OnPeakValue(float peak);

    // video callbacks
    unsigned int camera_capture_callback(IMediaSample *pSample, void *context, CMediaType *pmt);

    // webrtc window capture callback
    void OnCapture(void *buffer, int width, int height);
    void OnCaptureError();

    // monitor capture callback
    void on_capture(void *buffer, int width, int height);
    void on_capture_error();

    // device functions
    const TCHAR *get_current_camera_device();
    const TCHAR *get_current_camera_device_name();
    const TCHAR *get_current_mic_device_name();
    const TCHAR *get_current_speaker_device_name();
    void get_current_mic_device(GUID &guid);
    void get_current_speaker_device(GUID &guid);
    const TCHAR *get_preferred_camera_device();
    const TCHAR *get_preferred_camera_device_name();
    float get_mic_peak_meter();
    bool get_mic_share_mode();
    void on_media_device_arrival();
    void on_media_device_removal();
    void reset_camera_device(video_device *device);
    void reset_content_audio();
    void reset_mic_device(const GUID &guid, bool forceReset = false);
    void reset_speaker_device(const GUID &guid);
    void set_audio_device_os_default(FRTC_MEDIA_DEVICE_TYPE type, bool useDefault);
    void set_mic_share_mode(bool useShareMode);
    void start_mic_test();
    void stop_mic_test();
    void sync_audio_device_with_os();
    void update_audio_devices();
    void update_camera_devices();
    void update_content_audio_device();
    void update_media_devices();

    // UI functions
    void clear_main_window();
    void enter_full_screen(bool bFull);
    void exit_full_screen();
    BYTE *get_video_mute_pic();
    void mouse_event_callback(UINT message);
    void on_main_window_size_changed(LONG left = 0, LONG top = 0, LONG right = 0, LONG bottom = 0);
    void reset_main_window(HWND hwnd, HINSTANCE hInst);
    void set_create_options(std::shared_ptr<CREATE_OPTIONS> create_options);
    void set_current_view(VideoWnd *pView);
    void set_main_cell_size();
    void set_use_gdi_render(bool useGDIRender);
    void set_video_data_zero_check_timer();

    // message queue
    void clean_sc_msg_queue();
    BOOL process_sc_msg_queue_msg();

    // call state and information
    BOOL initialized();
    FRTC_CALL_STATE get_call_state();
    bool is_sending_content();
    ReconnectState get_reconnect_state();
    void cancel_next_reconnect();
    const FrtcCallParam *get_lastcall_param();

    // UI and display
    bool is_local_video_wnd_hidden();
    bool is_switch_layout_enabled();
    void toggle_name_card_visible(bool bVisible);
    bool is_namecard_visible();

    Json::Value get_participant_list();
    std::string get_participant_list_jstr();

    void start_send_window_content(HWND hwnd);
    bool start_send_desktop_content(const WCHAR *szMonitor, int MonitorIndex);
    void stop_sending_content();
    void set_share_content_with_audio(bool enable);
    void start_send_content(const std::wstring &name, int index);
    void start_send_content(int hwnd);
    void on_monitor_list_changed();

    void get_receive_video(std::string &msid, void **buffer, unsigned int *length, unsigned int *width, unsigned int *height);
    void clear_receive_video(std::string &msid);
    void show_camera_failed_reminder();

    void get_local_preview_msid(std::string &msid);

    const std::string &get_camera_device_list_jstr();
    const video_device_list &get_camera_devices(BOOL updateNow);
    void set_camera_device(const TCHAR *deviceID);

    const std::string &get_microphone_device_list_jstr();
    void get_microphone_devices(std::vector<DevInfo> &list);
    void set_microphone_device(const TCHAR *deviceID);

    const std::string &get_speaker_device_list_jstr();
    void get_speaker_devices(std::vector<DevInfo> &list);
    void set_speaker_device(const TCHAR *deviceID);

    void notify_camera_device_reset();
    void notify_audio_device_reset(FRTC_MEDIA_DEVICE_TYPE type);

    int resize_yuv420(unsigned char *pSrc, int src_w, int src_h, unsigned char *pDst, int dst_w, int dst_h);
    void aes_decode_frtc(const std::string &salt, const std::string &ciphered, std::string &decoded);

    // log upload
    uint64_t start_upload_logs(const std::string &metaData,
                               const std::string &fileName,
                               int fileCount,
                               const std::string &coreDumpName);
    int get_log_update_status(uint64_t tractionId, int fileType, int *speed);
    void cancel_log_update(uint64_t tractionId);

    const char *get_meeting_statistics();
    const char *get_app_uuid();
    const char *get_monitor_list();
    const char *get_window_list();
    const std::string &get_statistics_jstr();

    void set_call_state_change_ui_callback(CallStateChangedCB callback);
    void set_password_request_ui_callback(CallPasswordProcessCB callback);
    void set_reminder_notify_ui_callback(ReminderNotifyCB callback);
    void set_video_wnd_mouse_event_callback(WndMouseEventCB callback);
    void set_meeting_control_msg_callback(MeetingControlMsgCB callback);
    void set_content_sending_state_callback(ContentShareStateCB callback);

    BYTE *get_name_card_yuv_data(std::string name, 
                                 int w, 
                                 int h, 
                                 FRTCSDK::SDK_NAME_CARD_TYPE namecardType, 
                                 FRTCSDK::SDK_NAME_CARD_FONT_SIZE_TYPE fontSizeType, 
                                 int &newWidth);

    BYTE *get_watermark_yuv_data(std::string msg, int w, int h, int &newWidth);
    void set_name_card_begin_time(time_t t);
    time_t get_name_card_begin_time();

    void change_camera_resolution(int h, bool isRequest);
    void restart_camera();
    void set_camera_quality_preference(std::string &preference);

    int get_network_intensity();

    static AUDIO_DEVICE_TYPE get_audio_device_type_by_friendly_name(const std::wstring& deviceName);

private:
    // Content capture callbacks
    void on_content_capture(void *buffer, int width, int height);
    void on_content_capture_error();

    // Reminder type
    void set_reminder_type(FRTC_TIP_TYPE type);
    FRTC_REMINDER_TYPE get_reminder_type(FRTC_TIP_TYPE type);

    // Content audio
    bool is_same_speaker_device();
    void init_content_audio_device();
    void start_meeting_audio();
    void stop_meeting_audio();
    void start_send_content_audio(bool notifyRTC);

    // Video control
    void start_meeting_video();
    void stop_meeting_video();
    void enable_camera(bool enable);
    void report_mute_status();
    void handle_svc_layout_changed(const RTC::LayoutDescription &layout);
    void start_camera();
    void stop_camera(bool ShowMutePic = false);
    void start_video_stream(const FRTC_SDK_CALL_NOTIFICATION &callNotify);
    void stop_video_stream(const FRTC_SDK_CALL_NOTIFICATION &callNotify);
    void start_content_capture(bool useMinRate = false);
    void stop_content_capture();
    void update_video_windows();
    void update_icon_window_status();
    void on_participant_mute_state_changed(std::map<std::string, RTC::ParticipantStatus> &muteStatusList, bool isFullList);
	void update_self_display_name(const std::string& new_name);
    void update_self_video_status();
    void on_update_video_windows();
    void clear_params();
    void clear_content_sending_params();
    void clear_name_card();
    void load_camera_muted_pic();
    void load_share_window_paused_pic();

    // Device information
    std::string video_device_to_str(const video_device_list &device);
    std::string audio_device_to_str(const std::vector<DevInfo> &device);

    // Media statistics
    int get_signal_status(const std::string &statistics);
    int get_average_lost_rate(const Json::Value &mediaStatistics);

    // State mapping
    FRTC_SDK_CALL_REASON map_rtcsdk_state_reason_to_frtc_call_reason(RTC::MeetingStatusChangeReason reason);

    // Thread procedures
    static DWORD WINAPI make_call_thread_proc(LPVOID param);
    static DWORD WINAPI create_720p_name_card(LPVOID lpParame);
    static DWORD WINAPI create_720p_watermark(LPVOID lpParame);

    // Utility functions
    void create_resolution_str(int height, int framerate, std::string &strResolution);
    unsigned char *get_media_convert_buffer(int size);
    bool check_video_data_zero(unsigned char *buffer, int size);
    CPU_LOAD_LEVEL get_cpu_load_level();
    void set_cpu_level_to_dshow_capture();

    // Video frame sending
    void update_people_video_msid(const std::string &msid);
    void update_content_video_msid(const std::string &msid, bool checkEqual = false, const std::string &checkId = "");
    void send_people_video_frame(void *buffer, unsigned int length, unsigned int width, unsigned int height, RTC::VideoColorFormat type);
    void send_content_video_frame(void *buffer, unsigned int length, unsigned int width, unsigned int height, RTC::VideoColorFormat type);

private:
    // Variables related to SDK and meeting information
    std::string _sdk_version_str;
    RTC::RTCInterface *_rtcInstance;
    RTCEventObserver *_rtc_event_observer;
    std::string _server_addr;

    enum STATUS
    {
        S_CREATE,
        S_INIT,
        S_STARTED,
        S_STOPPED,
        S_DESTROY
    } _status = S_CREATE;

    std::string _meeting_id;
    std::string _meeting_name;
    std::string _meeting_owner_id;
    std::string _meeting_owner_name;
    std::string _incall_window_title;
    std::string _user_token;
    FRTC_CALL_STATE _call_state;
    bool _is_audio_only;
    bool _is_guest_call;

    bool _is_sending_audio;
    bool _is_sending_content;

    // Variables related to audio rendering and capture
    BOOL _is_testing_mic = FALSE;
    std::unique_ptr<AudioRender> _audio_render;
    ContentAudioCapture *_content_audio_capture;
    PeopleAudioCapture *_people_audio_capture;

    // Variables related to video rendering
    bool _use_gdi_render;
    bool _mirror_local_video;
    std::unique_ptr<FRTCVideoWndMgr> _video_wnd_mgr;

    // Variables related to media stream IDs
    CritSec _vpt_msid_lock;
    CritSec _vcs_msid_lock;
    std::string _vpt_msid;
    std::string _vcs_msid;
    std::string _apt_msid;
    std::string _apr_msid;

    // Variables related to display and statistics
    std::wstring _display_name;
    int _display_index;
    std::string _statistics_report_str;
    std::string _uuid;
    std::string _monitor_list;
    std::string _windows_list;
    std::string _camera_list;
    std::string _microphone_list;
    std::string _speaker_list;
    time_t _name_card_display_begin_time;
    std::map<std::string, BYTE *> _name_card_map;
    NAME_CARD_PARAM _name_card_info;

    // Variables related to local video and content
    BYTE *_local_camera_muted_pic_data_ptr;
    BYTE *_share_window_paused_pic_data_ptr;
    bool _content_paused;

    // Variables related to message handling and queues
    bool _is_processing_sc_msg;
    std::vector<std::string> _audio_only_list;
    std::vector<RTC::LayoutCell> _layout_items_list;
    CritSec _msg_queue_lock;
    std::queue<FRTC_SDK_CALL_NOTIFICATION> _sc_msg_queue;
    std::queue<FRTC_SDK_CALL_NOTIFICATION> _sc_msg_queue_temp;

    // Variables related to roster and participant management
    CritSec _name_card_map_lock;
    CritSec _media_device_lock;
    bool _is_local_video_muted;
    DWORD _local_video_mute_reason = -1;
    bool _is_local_audio_muted;
    bool _is_grid_mode;
    bool _is_sharing_content;
    bool _is_window_content;
    bool _is_audio_muted_by_server;
    bool _allow_unmute_audio_by_self;
    GUID _default_system_speaker_guid;
    bool _noise_blocker_enabled;
    GUEST_MAKE_CALL_PARAMS _guest_call_param;
    SIGNED_USER_MAKE_CALL_PARAMS _signed_call_param;
    std::string _guest_user_display_name;
    std::string _signed_user_display_name;
    CritSec _roster_list_lock;
    Json::Value _full_rosters_list;

    // Variables related to UI and threading
    HWND _hMain;

    // Variables related to audio and video capture
    VideoDeviceManager _video_dev_manager;
    DShowCapture _video_capture;
    PeopleAudioMonitor _people_audio_monitor;
    bool _using_os_default_mic_device;
    bool _using_os_default_speaker_device;

    // Variables related to content sharing
    std::unique_ptr<frtc_content_capturer> _frtc_content_capturer;
    std::string _confrence_alias;

    // Variables related to media conversion and buffer
    unsigned char *_media_convert_buffer;
    int _media_convert_buffer_size;

    // Variables related to callbacks and reports
    CallStateChangedCB _ui_call_state_change_callback;
    CallPasswordProcessCB _ui_pwd_request_callback;
    ReminderNotifyCB _ui_reminder_notify_callback;
    WndMouseEventCB _ui_window_mouse_event_callback;
    ContentShareStateCB _content_sending_state_callback;
    MeetingControlMsgCB _meeting_control_msg_callback;

    CPU_LOAD_LEVEL _average_cpu_load;
    std::list<int> _camera_resolution_list;
    std::map<std::string, int> _msid_to_resolution_map;
    int _max_camera_height;
    int _camera_resolution_height;
    bool _is_restart_camera_timer_set;
    INT _restart_camera_failed_count;
    bool _is_media_encrypted;

    std::shared_ptr<CREATE_OPTIONS> _create_options;

    bool _send_content_audio;
    bool _want_send_content_audio;

    bool _show_name_card;
    CritSec _namecard_lock;

    std::unique_ptr<ReconnectHelper> reconnectHelper_;
};