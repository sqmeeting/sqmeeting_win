using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting
{
    public delegate void OnFRTCCallStateChangeCallback(FrtcCallState callState, FrtcCallReason result, [MarshalAs(UnmanagedType.LPUTF8Str)] string meetingID, [MarshalAs(UnmanagedType.LPUTF8Str)] string meetingName);
    public delegate void OnMeetingPasswordCallback([MarshalAs(UnmanagedType.Bool)] bool isResponse, [MarshalAs(UnmanagedType.LPStr)] string reason);
    public delegate void OnMeetingControlMsgCallback(IntPtr meetingCtrlMsg);
    public delegate void OnContentSendingState([MarshalAs(UnmanagedType.Bool)] bool isSending);
    public delegate void OnEncryptedStateNotifyCallback([MarshalAs(UnmanagedType.Bool)] bool encrypted);
    public delegate void OnReminderNotify(FrtcReminderType type);
    public delegate void OnWndMouseEvent(SDKVIDEOEVENT type);

    [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
    public delegate void RTCStatisticsReportCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string report);

    public struct FrtcCallInitParam
    {
        public IntPtr uuid;
        public IntPtr serverAddress;
        public IntPtr callWndTitle;
        public OnFRTCCallStateChangeCallback funcCallStateChangedCB;
        public OnMeetingPasswordCallback funcCallPasswordProcessCB;
        public OnContentSendingState funcContentShareStateCB;
        public OnMeetingControlMsgCallback pfnOnMeetingCtrlMsg;
        public OnReminderNotify funcReminderNotifyCB;
        public OnWndMouseEvent funcWndMouseEvent;
    };

    public enum FrtcCallState
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING,
        DISCONNECTING
    };

    public enum FrtcReminderType
    {
        UNKNOW,
        MICROPHONEMUTED,
        SHARECONTENT_CPULIMITED,
        CAMERAERROR,
        SHARECONTENT_NOPERMISSION,
        SHARECONTENT_UPLINKBITRATELOW,


        //Only for app
        MEETING_INFO_COPIED = 400,
        MICROPHONE_UNMUTED,
        MICROPHONE_MUTED_BY_ADMIN,
        ENABLE_MUTE_ALL,
        DISABLE_MUTE_ALL,
        DISPLAY_NAME_RENAMED,
        YOU_ARE_LECTURER,
        MEETING_CONTROL_OPERATION_GENERAL_ERROR,
        MEETING_CONTROL_OPERATION_PARAM_ERROR,
        MEETING_CONTROL_OPERATION_AUTH_ERROR,
        MEETING_CONTROL_OPERATION_PERMISSION_ERROR,
        MEETING_CONTROL_OPERATION_NOMEETING_ERROR,
        MEETING_CONTROL_OPERATION_DATA_ERROR,
        MEETING_CONTROL_OPERATION_FORBIDDEN_ERROR,
        MEETING_CONTROL_OPERATION_STATUS_ERROR,
        MEETING_CONTROL_OPERATION_RECORDING_ERROR,
        MEETING_RECORDING_START,
        MEETING_RECORDING_START_OPERATOR,
        MEETING_RECORDING_STOP,
        MEETING_RECORDING_STOP_OPERATOR,
        MEETING_STREAMING_START,
        MEETING_STREAMING_START_OPERATOR,
        MEETING_STREAMING_STOP,
        MEETING_STREAMING_STOP_OPERATOR,
        STREAMING_INFO_COPIED,
        TEXT_OVERLAY_STARTED,
        TEXT_OVERLAY_STOPPED,
        VIOCE_ONLY_MEETING_NOTIFICATION,
        START_RECORDING_ERROR,
        START_RECORDING_PARAMS_ERROR,
        START_RECORDING_NO_LICENSE,
        START_RECORDING_LICENSE_LIMITATION_REACHED,
        START_RECORDING_INSUFFICIENT_RESOURCE,
        START_RECORDING_NO_SERVICE,
        START_RECORDING_MULTIPLY,
        STOP_RECORDING_ERROR,
        STOP_RECORDING_PARAMS_ERROR,
        START_STREAMING_ERROR,
        START_STREAMING_PARAMS_ERROR,
        START_STREAMING_NO_LICENSE,
        START_STREAMING_LICENSE_LIMITATION_REACHED,
        START_STREAMING_INSUFFICIENT_RESOURCE,
        START_STREAMING_NO_SERVICE,
        START_STREAMING_MULTIPLY,
        STOP_STREAMING_ERROR,
        STOP_STREAMING_PARAMS_ERROR,
        PIN_VIDEO_SUCCESS,
        UNPIN_VIDEO_SUCCESS,
        VIDEO_PINNED,
        VIDEO_UNPINNED,
        UNMUTE_APPLIED
    };

    public enum FrtcCallReason
    {
        // TODO:Add more if need
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
    };

    public enum FrtcCallExtConfigKey
    {
        CONFIG_ENABLE_NOISE_BLOCKER,
        CONFIG_CAMERA_QUALITY_PREFERENCE,	//"framerate" or "resolution"

        CONFIG_USE_SOFTWARE_RENDER = 100,   //windows only
        CONFIG_MIRROR_LOCAL_VIDEO,
        CONFIG_MICROPHONE_SHAREMODE
    };

    public struct FrtcCallParam : IDisposable
    {
        public IntPtr serverAddress;
        public IntPtr callNumberStr;
        public IntPtr displayNameStr;
        public IntPtr userToken;
        public IntPtr meetingPasswd;
        public IntPtr callRateStr;
        [MarshalAs(UnmanagedType.Bool)]
        public bool isAudioOnly;
        [MarshalAs(UnmanagedType.Bool)]
        public bool muteAudio;
        [MarshalAs(UnmanagedType.Bool)]
        public bool muteVideo;
        public FrtcLayout layout;
        public IntPtr monitorID;

        public void Dispose()
        {
            if (monitorID != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(monitorID);
                monitorID = IntPtr.Zero;
            }
            if (serverAddress != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(serverAddress);
                serverAddress = IntPtr.Zero;
            }
            if (callNumberStr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(callNumberStr);
                callNumberStr = IntPtr.Zero;
            }
            if (displayNameStr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(displayNameStr);
                displayNameStr = IntPtr.Zero;
            }
            if (userToken != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(userToken);
                userToken = IntPtr.Zero;
            }
            if (meetingPasswd != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(meetingPasswd);
                meetingPasswd = IntPtr.Zero;
            }
            if (callRateStr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(callRateStr);
                callRateStr = IntPtr.Zero;
            }
        }

    };
    public enum FrtcLayout
    {
        LAYOUT_AUTO,
        LAYOUT_GALLERY
    }

    public enum MEDIA_DEVICE_TYPE
    {
        MEDIA_DEVICE_TYPE_SPEAKER = 0,
        MEDIA_DEVICE_TYPE_MICROPHONE = 1,
        MEDIA_DEVICE_TYPE_CAMERA = 2
    }

    public enum FRTC_API_RESULT
    {
        SIGNIN_SUCCESS,
        SIGNIN_SUCCESS_TOKEN,
        SIGNOUT_SUCCESS,
        USER_ROOM_SUCCESS,
        USER_ROOM_FAILED,
        QUERY_USER_SUCCESS,
        QUERY_USER_FAILED,
        USER_VALID,
        USER_NOT_EXIST,
        NEED_SET_PASSWORD,
        NEED_CHANGE_PASSWORD,
        USER_IS_ADMIN,
        PASSWORD_RESET_SUCCESS,
        PASSWORD_RESET_FAILED,
        SIGNIN_SESSION_RESET,
        SIGNIN_FAILED_INVALID_TOKEN,
        SIGNIN_FAILED_INVALID_PASSWORD,
        CONNECTION_FAILED,
        SIGNIN_FAILED_UNKNOWN_ERROR,
        API_SESSION_INVALID,
        SIGNIN_FAILED_PWD_FREEZED,
        SIGNIN_FAILED_PWD_LOCKED
    }

    public enum SDKVIDEOEVENT
    {
        LBUTTONDBLCLKEVENT = 0,
        LBUTTONUPEVENT = 1,
        MOUSE_HOVER,
        MOUSE_LEAVE
    };

    public enum SDKIntensityType
    {
        SIGNALINTENSITYLOW = 1,
        SIGNALINTENSITYMEDIAN = 3,
        SIGNALINTENSITYHIGH = 5
    };

    public static class FRTCSDK
    {
        const string FRTCSDKDllFileName = "frtc_sdk.dll";

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_sdk_init([MarshalAs(UnmanagedType.Struct)] FrtcCallInitParam callback);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_sdk_ext_config(FrtcCallExtConfigKey key, string valueStr);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool frtc_call_join(FrtcCallParam param);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool frtc_call_join_uri(string uri, string token, string signInServer, string displayName, int layout);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr frtc_parent_hwnd_set(IntPtr uiHostHwnd);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_call_leave();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_call_password_send(string passCode);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr frtc_media_device_get(MEDIA_DEVICE_TYPE deviceType);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_media_device_set(MEDIA_DEVICE_TYPE deviceType, [MarshalAs(UnmanagedType.LPStr)] string deviceID);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_start_mic_test();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void frtc_stop_mic_test();

        //[DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        //public static extern float frtc_get_mic_peak_meter();

        #region Meeting Control API

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_desktop_share(int monitorIndex, bool withContentAudio);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_window_share(int hwnd, bool withContentAudio);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_content_stop();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_local_audio_mute(bool mute);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_local_video_start();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_local_video_stop();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_full_screen_switch(bool fullScreen);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_layout_config(FrtcLayout layout);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_local_preview_hide(bool hide);

        #endregion

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr frtc_sdk_version_get();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr frtc_statistics_collect();//JSON string

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr frtc_participants_collect();//JSON string

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr frtc_monitors_collect();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr frtc_windows_collect();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool frtc_local_preview_hide_check();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool frtc_layout_config_enabled_check();

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_name_card_switch(bool bVisible);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Int64 frtc_logs_upload_start(string metaData, string fileName, int count, string coreDumpName);

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Int16 frtc_logs_upload_status_query(Int64 tractionId, int fileType, ref int speed); //0, log file; 1, core dump file; 2, meta file;

        [DllImport(FRTCSDKDllFileName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void frtc_logs_upload_cancel(Int64 tractionId);

    }
}
