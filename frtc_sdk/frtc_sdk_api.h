#pragma once

#define FRTC_SDK_API extern "C" __declspec (dllexport)
#define FRTC_SDK_CALLBACK __stdcall


typedef enum _FRTC_REMINDER_TYPE
{
	REMINDER_UNKNOW,
	REMINDER_AUDIO_MUTE,
	REMINDER_CPU_WARN,
	REMINDER_CAMERA_ERROR,
	REMINDER_SHARE_CONTENT_PROHIBITED,
	REMINDER_LOW_UPLINK_BITRATE
}FRTC_REMINDER_TYPE;

typedef enum FrtcLayout
{
	LAYOUT_AUTO,
	LAYOUT_GALLERY
};

typedef enum FrtcMouseEvent
{
	LMB_DBLCLICK,
	LMB_UP,
	MOUSE_HOVER,
	MOUSE_LEAVE
};

typedef struct FrtcCallParam
{
	char* _server_address;
	char* callNumber;				// Called Number
	char* _display_name;				// User name
	char* _user_token;				// User token if signed in
	char* _meeting_pwd;			// Meeting password
	char* _call_rate;				// Max call rate, default to 0 to enable Dynamic Bandwidth Ajust
	BOOL   isAudioOnly;
	BOOL   muteAudio;
	BOOL   muteVideo;
	FrtcLayout layout;

	/*
public:
	FrtcCallParam* clone() const
	{
		FrtcCallParam* target = new FrtcCallParam();
		size_t endingLen = 1;
		size_t len = strlen(this->_server_address);
		target->_server_address = new char[len + endingLen];
		memset(target->_server_address, 0, len + endingLen);
		strcpy_s(target->_server_address, len + endingLen, this->_server_address);

		len = strlen(this->callNumber);
		target->callNumber = new char[len + endingLen];
		memset(target->callNumber, 0, len + endingLen);
		strcpy_s(target->callNumber, len + endingLen, this->callNumber);

		len = strlen(this->_display_name);
		target->_display_name = new char[len + endingLen];
		memset(target->_display_name, 0, len + endingLen);
		strcpy_s(target->_display_name, len + endingLen, this->_display_name);

		if (this->_user_token)
		{
			len = strlen(this->_user_token);
			target->_user_token = new char[len + endingLen];
			memset(target->_user_token, 0, len + endingLen);
			strcpy_s(target->_user_token, len + endingLen, this->_user_token);
		}

		if (this->_meeting_pwd)
		{
			len = strlen(this->_meeting_pwd);
			target->_meeting_pwd = new char[len + endingLen];
			memset(target->_meeting_pwd, 0, len + endingLen);
			strcpy_s(target->_meeting_pwd, len + endingLen, this->_meeting_pwd);
		}

		len = strlen(this->_call_rate);
		target->_call_rate = new char[len + endingLen];
		memset(target->_call_rate, 0, len + endingLen);
		strcpy_s(target->_call_rate, len + endingLen, this->_call_rate);

		target->isAudioOnly = this->isAudioOnly;
		target->muteAudio = this->muteAudio;
		target->muteVideo = this->muteVideo;
		target->layout = this->layout;

		return target;
	}
	*/
};

typedef enum _FRTC_CALL_STATE
{
	CALL_CONNECTED,
	CALL_DISCONNECTED,
	CALL_CONNECTING,
	CALL_DISCONNECTING
}FRTC_CALL_STATE;

typedef enum _FRTC_SDK_CALL_REASON
{
	CALL_SUCCESS,
	CALL_SUCCESS_BUT_UNSECURE,
	CALL_NON_EXISTENT_MEETING,
	CALL_REJECTED,
	CALL_NO_ANSWER,
	CALL_UNREACHABLE,
	CALL_HANGUP,
	CALL_ABORT,
	CALL_CONNECTION_LOST,
	CALL_LOCKED,
	CALL_SERVER_ERROR,
	CALL_NO_PERMISSION,
	CALL_AUTH_FAILED,
	CALL_UNABLE_PROCESS,
	CALL_MEETING_STOP,
	CALL_MEETING_INTERRUPT,
	CALL_REMOVE_FROM_MEETING,
	CALL_FAILED,
	CALL_PASSWORD_FAILED_RETRY_MAX,
	CALL_PASSWORD_FAILED_TIMEOUT,
	CALL_PASSWORD_FAILED,
	CALL_GUEST_NOT_ALLOW,
	CALL_MEETING_FULL,
	CALL_MEETING_NOT_START,
	CALL_MEETING_EXPIRED,
	CALL_MEETING_NO_LICENSE,
	CALL_MEETING_LICENSE_MAX_LIMIT_REACHED,
	CALL_MEETING_END_ABNORMAL
}FRTC_SDK_CALL_REASON;

typedef enum _FRTC_MEDIA_DEVICE_TYPE
{
	MEDIA_DEVICE_SPEAKER,
	MEDIA_DEVICE_MIC,
	MEDIA_DEVICE_CAMERA
}FRTC_MEDIA_DEVICE_TYPE;

typedef enum _FRTC_SDK_EXT_CONFIG_KEY
{
	CONFIG_ENABLE_NOISE_BLOCKER,
	CONFIG_CAMERA_QUALITY_PREFERENCE,

	CONFIG_USE_SOFTWARE_RENDER = 100,   //windows only
	CONFIG_MIRROR_LOCAL_VIDEO,
	CONFIG_MICROPHONE_SHAREMODE
}FRTC_SDK_EXT_CONFIG_KEY;

/*------------------------------------------------------------------------------
 *  CALLBACKS
 */

 /** Call state changed callback
  *  @param [in] callState: new state
  *  @param [in] reason: the reason of state changed
  *  @param [in] meetingID: meeting ID of the call joined
  *  @param [in] meetingName: meeting Name of the call joined
  */
typedef void(FRTC_SDK_CALLBACK* CallStateChangedCB)(int callState, FRTC_SDK_CALL_REASON reason, const char* meetingID, const char* meetingName);

/** Content sharing state changed callback
 *  @param [in] isSharing: true=started sharing, false=stopped sharing
 */
typedef void(FRTC_SDK_CALLBACK* ContentShareStateCB)(bool isSharing);

/** Call password requested callback
 *  @param [in] isResponse: false=request, true=response
 *  @param [in] reason: reason string, only used in response
 */
typedef void(FRTC_SDK_CALLBACK* CallPasswordProcessCB)(bool isResponse, const char* reason);

/** Meeting control message callback
 *  @param [in] msg: message formated as Json
 *  @see reference document for message details
 */
typedef void(FRTC_SDK_CALLBACK* MeetingControlMsgCB)(const char* msg);

/** Remainder notification callback
 *  @param [in] type: remainder type
 */
typedef void(FRTC_SDK_CALLBACK* ReminderNotifyCB)(FRTC_REMINDER_TYPE type);

/** Window mouse event callback
 *  @param [in] type: mouse event
 */
typedef void(FRTC_SDK_CALLBACK* WndMouseEventCB)(FrtcMouseEvent type);

/*------------------------------------------------------------------------------
 *  GENERAL
 */

typedef struct FrtcCallInitParam {
	char* uuid;
	char* _server_address;
	char* callWndTile;
	CallStateChangedCB      funcCallStateChangedCB;
	CallPasswordProcessCB   funcCallPasswordProcessCB;
	ContentShareStateCB     funcContentShareStateCB;
	MeetingControlMsgCB     funcMeetingControlMsgCB;
	ReminderNotifyCB        funcReminderNotifyCB;
	WndMouseEventCB         funcWndMouseEventCB;
};

/** API Init
 *  @param [in] param: init parameters
 */
FRTC_SDK_API void frtc_sdk_init(FrtcCallInitParam param);

/** Get SDK full version
 *  @retval version string got
 */
FRTC_SDK_API const char* frtc_sdk_version_get();//Include the full version

/** Extended configuration
 *  @param [in] key: enum value of configuration key
 *  @param [in] value: string encoded configuration value
 */
FRTC_SDK_API void frtc_sdk_ext_config(FRTC_SDK_EXT_CONFIG_KEY key, char* value);

/*------------------------------------------------------------------------------
 *  CALL CONTROL
 */

 /** Join a call
  *  @param [in] params: call parameters
  */
FRTC_SDK_API BOOL frtc_call_join(FrtcCallParam params);

/** Leave a call
 */
FRTC_SDK_API void frtc_call_leave();

/** Send input call password
 *  @param [in] password: input call password
 */
FRTC_SDK_API void frtc_call_password_send(const char* password);

/** Set parent window for video window
 *  @param [in] parent: parent window handle
 *  @retval video window handle
 */
FRTC_SDK_API UINT32 frtc_parent_hwnd_set(HWND parent);

/*------------------------------------------------------------------------------
 *  VIDEO CONTROL
 */

 /** Start share desktop content
  *  @param [in] monitorIndex: desktop monitor index
  *  @param [in] withContentAudio: if share content audio or not
  */
FRTC_SDK_API void frtc_desktop_share(int monitorIndex, bool withContentAudio);

/** Start share window content
 *  @param [in] hwnd: window handle
 *  @param [in] withContentAudio: if share content audio or not
 */
FRTC_SDK_API void frtc_window_share(int hwnd, bool withContentAudio);

/** Stop share content
 */
FRTC_SDK_API void frtc_content_stop();

/** Start local video
 */
FRTC_SDK_API void frtc_local_video_start();

/** Stop local video
 */
FRTC_SDK_API void frtc_local_video_stop();

/** Hide/unhide local preview
 *  @param [in] hide: true=hide, false=unhide
 */
FRTC_SDK_API void frtc_local_preview_hide(bool hide);

/** Check local preview hidden state
 *  @retval: true=hidden, false=unhidden
 */
FRTC_SDK_API const BOOL frtc_local_preview_hide_check();

/** Config layout
 *  @param [in] layout: layout to config
 */
FRTC_SDK_API void frtc_layout_config(FrtcLayout layout);

/** Check if it's able to config layout
 *  @retval true=yes, false=no
 */
FRTC_SDK_API const BOOL frtc_layout_config_enabled_check();

/** Switch on/off fullscreen
 *  @param [in] fullScreen: true=on, false=off
 */
FRTC_SDK_API void frtc_full_screen_switch(bool fullScreen);

/** Switch on/off namecard
 *  @param [in] visible: true=on, false=off
 */
FRTC_SDK_API void frtc_name_card_switch(bool visible);

/*------------------------------------------------------------------------------
 *  AUDIO CONTROL
 */

 /** Mute/unmute local audio
  *  @param [in] mute: true=mute, false=unmute
  */
FRTC_SDK_API void frtc_local_audio_mute(bool mute);

/** Check local audio mute/unmute state
 *  @retval true=mute, faluse=unmute
 */
FRTC_SDK_API const BOOL frtc_audio_mute_check();

/*------------------------------------------------------------------------------
 *  DEVICE MANAGEMENT
 */

 /** Get media device list
  *  @param [in] deviceType: the type of the device list
  *  @retval string encoded device list
  */
FRTC_SDK_API const char* frtc_media_device_get(FRTC_MEDIA_DEVICE_TYPE deviceType);

/** Set media device to use
 *  @param [in] deviceType: the type of the device
 *  @param [in] deviceID: ID of the device, for audio devices, if set to "os_default" will use OS default device
 */
FRTC_SDK_API void frtc_media_device_set(FRTC_MEDIA_DEVICE_TYPE deviceType, const char* deviceID);

FRTC_SDK_API void frtc_start_mic_test();
FRTC_SDK_API void frtc_stop_mic_test();

FRTC_SDK_API float frtc_get_mic_peak_meter();

/*------------------------------------------------------------------------------
 *  INFORMATION COLLECT
 */

 /** Collect participant list
  *  @retval Json string encoded participant list, for example:
  *              { "participant_list": [ "User ABC", "User XYZ" ] }
  */
FRTC_SDK_API const char* frtc_participants_collect();

/** Collect call statistics
 *  @retval Json string encoded statistics
 *  @see reference document for statistics details
 */
FRTC_SDK_API const char* frtc_statistics_collect();

/** Collect all monitors connected
 *  @retval Json string encoded monitor list, for example:
 *              { "monitors": [ { "deviceName": "\\\\.\\DISPLAY1", "monitorName": "Desktop 1", "bottom": 1080, "handle": 527567565, "index": 1, "left": 0, "right": 1920, "top": 0, "isPrimary": true },
				  { "deviceName": "\\\\.\\DISPLAY2", "monitorName": "Desktop 2", "bottom": 2160, "handle": 1478367895, "index": 2, "left": 1920, "right": 5760, "top": 0, "isPrimary": false } ] }
 */
FRTC_SDK_API const char* frtc_monitors_collect();


/** Collect all windows could be capture
 *  @retval Json string encoded monitor list, for example:
 *              { "windows": [ { "windowTitle": "myWindow", "hwnd": 66666},
				  { "windowTitle": "myWindow2", "hwnd": "88888"} ] }
 */
FRTC_SDK_API const char* frtc_windows_collect();


/*
 * upload logs to frtc team
 * metaData contains some basic infomations be like:
 * {"version": "client_version",                                   //3.1.0.248

	"platform": "windows|android|mac|ios|linux",     // linux or linux-amd64, linux-arm64, linux-longson

	"os": "os details",                                                 // windows 11 buildxxx,  macos 12.1 xxx, iphoneOS 14.2..

	"device": "device info"                                         // huawei pc,   xiaomi 12,  iphone 13 max

	"issue": "xxxx can not join meeting"}

 * fileName could contains multiple files be like "file1.log, file1.log.1, ...etc"
 * return traction id
*/
FRTC_SDK_API long long frtc_logs_upload_start(const char* metaData, const char* fileName, int count, const char* coreDumpName);

FRTC_SDK_API int frtc_logs_upload_status_query(long long tractionId, int fileType, int* speed);
FRTC_SDK_API void frtc_logs_upload_cancel(long long tractionId);