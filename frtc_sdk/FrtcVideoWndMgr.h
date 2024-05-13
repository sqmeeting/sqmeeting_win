#pragma once
#include "stdafx.h"
#include "auto_lock.h"
#include <list>
#include "VideoWnd.h"
#include "D3D9RenderComponent.h"

typedef struct _STEAM_INFO
{
	std::string uuid;
	bool _is_video_ready;
}STEAM_INFO, *PSTEAM_INFO;


class FRTCVideoWndMgr
{
public:
	FRTCVideoWndMgr();
	~FRTCVideoWndMgr();

	void init_free_video_wnd();
	void set_main_window(HWND hMain, HINSTANCE hInst, bool useGDIRender);
	void set_local_stream_msid(const std::string& localStreamID);
	void remove_main_window();
	void show_video_window();
	void hide_video_window();
	void set_watermark(bool enable, std::string msg);
	void show_video_stream(std::string msid, bool bFake);
	void remove_video_stream(std::string msid, bool bFake);
	void set_video_data_zero_check_timer(std::string msid);
	void set_layout_cell_max_count(int maxLayoutCells);

	void on_main_window_size_changed(LONG left = 0, 
                                     LONG top = 0, 
                                     LONG right = 0, 
                                     LONG bottom = 0);

	void set_main_cell_size();
	void set_current_view(VideoWnd * pView);
	void set_active_speaker(const std::string &uuid, bool forceSet);
	void set_cell_customizations(const std::string& uuid);
	void set_current_lecture(const std::string& uuid);
	void set_first_request_stream_uuid(const std::string &uuid);
	void set_user_name(const std::string& msid, const std::string& username);

	void update_user_name_by_uuid(const std::string& uuid, 
                                  const std::string& username);

	void set_msid(const std::string& uuid, const std::string& msid);
	std::string find_uuid(const std::string msid);
	void setShareContent(bool bShare);
	void enter_full_screen(bool bFull);
	void clear_params();
	void toggle_layout_grid_mode(bool gridMode);
	void toggle_local_video_hidden(bool hide);
	bool is_local_video_visible();
	bool is_local_only();
	void set_handle_layout_change(bool bHandle);
    
	void toggle_audio_state_icon_window_show(std::string uuid, 
                                             bool audioIconshow);
	void show_all_icon_window();

protected:
	void update_layout();
	void switch_to_1n5_layout();
	void switch_to_grid_layout();
	int  get_cells_count(const std::string& newStreamUUID);

private:
	void set_content_stream(const std::string& msid, VideoWnd* pViewWnd);
	void find_current_view_wnd();

protected:
	HWND										_hMain;
	RECT										_main_rect;
	std::shared_ptr<D3D9RendererEngine>			_d3d9_renderer;
	VideoWnd									_video_windows[10];
	std::list<VideoWnd*>						_free_video_wnd_list;
	std::list<VideoWnd*>						_occupied_video_wnd_List;
	std::map<std::string, VideoWnd*>			_occupied_video_wnd_map;
	std::list<STEAM_INFO>						_reserved_video_stream_list;
	std::map<std::string, std::string>			 _user_name_map;
	std::map<std::string, std::string>			_msid_map;
	VideoWnd									*_current_video_view;
	std::string									_active_speaker_uuid;
	std::string									_cell_customization_uuid;
	std::string									_current_lecture_uuid;
	std::string									_first_request_stream_uuid;
	int											_small_cell_width;
	int											_small_cell_height;
	std::string									_local_stream_msid;
	std::string									_local_preview_uuid;
	bool										_is_local_video_hidden;
	bool										_is_watermark_enabled;
	std::string									_watermark_str;
	CritSec										_uuid_to_msid_map_sec;

private:
	CritSec	_lock;
	bool    _is_sharing_content;
	bool	_is_full_screen;
	bool	_is_grid_mode;
	int		_wnd_count;
	int     _layout_cell_max_count;
	bool    _is_processing_layout_change;

private:
	const std::string kContentMSIDPrefixStr = "VCR";
};