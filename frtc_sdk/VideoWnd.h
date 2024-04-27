#pragma once

#include "FRTCBaseWnd.h"
#include "D3D9RenderComponent.h"
#include "FRTCStreamIconWindow.h"


class FRTCGDIRender;
class VideoWnd :public FRTCBaseWnd
{
public:
	VideoWnd();
	~VideoWnd();

    void set_d3d9_renderer(std::shared_ptr<D3D9RendererEngine>& renderer);
	void create_gdi_renderer();
    void set_icon_wnd_position(int parent_w, int parent_h);
    bool is_icon_window_visible();
    void update_icon_window();
    void show_audio_muted_icon_wnd(bool show);
    void show_pinned_icon_wnd(bool show);

	void start_video(BOOL local_preview, 
                     const std::string& msid, 
                     const std::string& uuid);
    void stop_video();
	void start_fake_video(const std::string& msid, const std::string& uuid);
	void start_timer();
    void stop_timer();
    void set_video_data_zero_check_timer();
    BOOL is_local_video();
	void set_main_cell(bool isMainCell);
	std::string get_msid();
	std::string get_uuid();
	
    bool check_stream_uuid(const std::string& UUID);
    void draw_dummy_frame(HDC hdc = NULL);
    void set_border(bool hasBorder);
    void set_is_content_stream(bool isContent);
    void set_watermark_str(const std::string& watermark);
    void draw_frame (HDC hdc = 0);
    void draw_local_preview(HDC hdc = 0);
	void check_local_video_data();
    void update_user_name(const std::string& user_name_str);
	void get_user_name(std::string* user_name_ptr);
	
	virtual LRESULT  WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);

private:
	bool _is_fake_video;
    bool _is_main_cell;
	bool _has_border;
	bool _is_content_stream;
    BOOL _use_gdi_render;
    BOOL _is_local_preview;
    BOOL _need_stop_timer;
    std::string	_msid;
	std::string _uuid;
	std::string	_user_name;
	std::string	_watermark_str;
    UINT_PTR _timer_id;
	void* _buffer;
    std::unique_ptr<FRTCGDIRender> _gdi_renderer;
	std::shared_ptr<D3D9RendererEngine>	_d3d9_renderer;
	std::unique_ptr<FRTCStreamIconWindow> _icon_wnd_audio_muted;
	std::unique_ptr<FRTCStreamIconWindow> _icon_wnd_pinned;
	std::unique_ptr<FRTCStreamIconWindow> _icon_wnd_user_name;

public:
	bool _show_audio_muted_icon;
	bool _show_pinned_icon;
	int	_show_wnd_cmd;
	HWND _video_wnd_handle;
	HDC _hdc;
    DWORD _set_timer_time;
	CritSec _time_lock;
	DWORD _click_start_time;
};

//	For some cases hardware renderer not available
class FRTCGDIRender
{
public:
	FRTCGDIRender();
	virtual ~FRTCGDIRender();
	void SetHWND(HWND hwnd);
	void Render(HDC hdc, 
                void* frameBuffer, 
                int width, 
                int height, 
                bool hasBorder, 
                int borderWidth, 
                bool isContent, 
                const std::string& water_mark_str);

private:
	HWND hwnd_;
	bool lastFrameHasBorder_;
	void* pArgbBuffer_;
	LONG bufferWidth_;
	LONG bufferHeight_;
};