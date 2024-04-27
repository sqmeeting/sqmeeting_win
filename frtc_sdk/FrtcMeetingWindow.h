#pragma once
#include "FRTCBaseWnd.h"
#include "resource.h"
#include "stdafx.h"
#include <vector>

#define WM_SHOW_AUDIO_MUTE_REMINDER_WINDOW  (WM_USER + 498)

class FrtcManager;

class FrtcMeetingWindow : public FRTCBaseWnd
{
public:
	FrtcMeetingWindow(std::shared_ptr<FrtcManager> frtc_mgr);
	~FrtcMeetingWindow();

	void init_main_window(std::shared_ptr<FrtcManager> frtc_mgr);
	void set_create_options(std::shared_ptr<CREATE_OPTIONS> create_options);
	void update_creat_options(std::shared_ptr<CREATE_OPTIONS> create_options);
	void toggle_full_screen(bool doFullScreen);
	void set_audio_only_mode(BOOL audioOnlyMode);
	bool get_content_state();
	void set_switch_layout_enable(bool enable);
	bool is_switch_layout_enabled();
	void start_statictisc_query_timer();
	void on_encryption_state_changed(bool encrypted);
	void set_meeting_id(std::wstring &meetingId);
	void set_window_title(std::wstring &title);
	void switch_layout(bool bGrid);
	void close_manually();
	void get_sharing_monitor_rect(RECT& rect);
	virtual LRESULT  WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);

private:
	void release_all();
	void clear_params();
	void set_subwindow_center_pos(HWND hWnd);

	BOOL RegisterDeviceNotification_ToHwnd(GUID if_class_guid, 
                                           HWND hWnd, 
                                           HDEVNOTIFY* device_notify);
	BOOL TrackMouseEvent_Register();

public:
	RECT _mvw_rc;

private:
	bool _is_video_started;
	bool _is_audio_started;
	bool _is_call_started;
    bool _is_sharing_content;
    bool _is_layout_enabled;
    bool _is_media_encrypted;
    BOOL _is_audio_only;

	std::shared_ptr <FrtcManager> _frtc_mgr;
	DWORD _wnd_style;
	DWORD _wnd_ex_style;
	std::shared_ptr<CREATE_OPTIONS> _create_options;
	std::vector<std::string> _roster_list;
	DWORD _click_start_time;
	std::wstring _meeting_id;
	std::wstring _window_title;
	time_t _meeting_start_time;
	HMONITOR _sharing_monitor_handle;
};
