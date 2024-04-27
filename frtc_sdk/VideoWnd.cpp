#include <memory>
#include "stdafx.h"
#include "VideoWnd.h"
#include "frtccall_manager.h"
#include "FrtcMeetingWindow.h"
#include "frtc_sdk_util.h"
#include "log.h"

extern std::shared_ptr<FrtcManager> g_frtc_mgr;
extern HINSTANCE g_hInstance;
extern FrtcMeetingWindow* g_frtc_meeting_wnd;

#define IDT_TIMER_NULL	0
#define IDT_TIMER_VID1	1
#define IDT_TIMER_VID2	2
#define IDT_TIMER_CLICK	3
#define IDT_TIMER_MOUSEENTER_VID 4
#define IDT_TIMER_MOUSELEAVE_VID 5

VideoWnd::VideoWnd() 
    : FRTCBaseWnd(false), 
      _is_fake_video(true),
      _has_border(false),
      _is_content_stream(false), 
      _use_gdi_render(FALSE), 
      _is_local_preview(FALSE),
      _need_stop_timer(FALSE),
      _watermark_str(""), 
      _timer_id(IDT_TIMER_NULL), 
      _buffer(malloc(1920 * 1080 * 3 / 2)),
      _gdi_renderer(nullptr), 
      _d3d9_renderer(nullptr), 
      _icon_wnd_audio_muted(nullptr),
	  _icon_wnd_pinned(nullptr),
	  _icon_wnd_user_name(nullptr),
      _show_audio_muted_icon(false),
      _show_pinned_icon(false),
      _show_wnd_cmd(SW_HIDE),
      _set_timer_time(0),
      _click_start_time(0)
{
}

VideoWnd::~VideoWnd()
{
	free(_buffer);
}

void  VideoWnd::set_d3d9_renderer(std::shared_ptr<D3D9RendererEngine>& renderer) {
    _d3d9_renderer = renderer;
}

void VideoWnd::create_gdi_renderer()
{
	_gdi_renderer = std::make_unique<FRTCGDIRender>();
	_gdi_renderer->SetHWND((HWND)*this);
	_use_gdi_render = TRUE;
}

void VideoWnd::set_icon_wnd_position(int parent_w, int parent_h)
{
	update_icon_window();
}

bool VideoWnd::is_icon_window_visible()
{
	int interval = time(0) - g_frtc_mgr->get_name_card_begin_time();
	bool namecard_visible = g_frtc_mgr->is_namecard_visible();;	

	return namecard_visible;
}

void VideoWnd::update_icon_window()
{
	if (_icon_wnd_audio_muted == nullptr || 
        _icon_wnd_pinned == nullptr ||
        _icon_wnd_user_name == nullptr)
    {
		return;
    }

	if (get_uuid() == "" || get_uuid() == "__local_preview_msid__")
	{
		::ShowWindow(*_icon_wnd_audio_muted, SW_HIDE);
		::ShowWindow(*_icon_wnd_pinned, SW_HIDE);
		::ShowWindow(*_icon_wnd_user_name, SW_HIDE);

		return;
	}

	if (!g_frtc_mgr->is_namecard_visible())
	{
		::ShowWindow(*_icon_wnd_audio_muted, SW_HIDE);
		::ShowWindow(*_icon_wnd_pinned, SW_HIDE);
		::ShowWindow(*_icon_wnd_user_name, SW_HIDE);

		return;
	}

	_icon_wnd_audio_muted->set_audio_mute_status(_show_audio_muted_icon);

	BOOL indicator_show = IsWindowVisible((HWND)*_icon_wnd_user_name);

	RECT client_rect;
	GetClientRect(*this, &client_rect);
	int top = client_rect.bottom - client_rect.top - 24;
	if (_is_main_cell)
	{
		top = client_rect.top;
	}

	if (_show_pinned_icon)
	{
		//if (_show_audio_muted_icon)
		{
			if (_user_name != "" && !_is_fake_video)
			{
				::SetWindowPos(*_icon_wnd_user_name, 
                               NULL, 
                               0, 
                               top, 
                               _icon_wnd_user_name->get_user_name_text_width() + 8, 
                               24, 
                               SWP_NOOWNERZORDER);

				::SetWindowPos(*_icon_wnd_pinned, 
                               NULL, 
                               _icon_wnd_user_name->get_user_name_text_width() + 8, 
                               top, 
                               24, 
                               24, 
                               SWP_NOOWNERZORDER);

				::SetWindowPos(*_icon_wnd_audio_muted, 
                               NULL, 
                               24 + _icon_wnd_user_name->get_user_name_text_width() + 8, 
                               top, 
                               24, 
                               24, 
                               SWP_NOOWNERZORDER);

				if (!indicator_show)
				{
					::ShowWindow(*_icon_wnd_user_name, SW_SHOW);
					::UpdateWindow(*_icon_wnd_user_name);
					::ShowWindow(*_icon_wnd_pinned, SW_SHOW);
					::UpdateWindow(*_icon_wnd_pinned);
					::ShowWindow(*_icon_wnd_audio_muted, SW_SHOW);
					::UpdateWindow(*_icon_wnd_audio_muted);
				}
				else
				{
					::RedrawWindow(*_icon_wnd_user_name, 
                                   NULL, 
                                   NULL, 
                                   RDW_INVALIDATE | RDW_ERASE);

					::RedrawWindow(*_icon_wnd_pinned, 
                                   NULL, 
                                   NULL, 
                                   RDW_INVALIDATE | RDW_ERASE);

					::RedrawWindow(*_icon_wnd_audio_muted, 
                                   NULL, 
                                   NULL, 
                                   RDW_INVALIDATE | RDW_ERASE);
				}
			}
			else
			{
				::SetWindowPos(*_icon_wnd_pinned, 
                               NULL, 
                               0, 
                               top, 
                               24, 
                               24, 
                               SWP_NOOWNERZORDER);

				::ShowWindow(*_icon_wnd_pinned, SW_SHOW);
				::ShowWindow(*_icon_wnd_user_name, SW_HIDE);

				::SetWindowPos(*_icon_wnd_audio_muted, 
                               NULL, 
                               24, 
                               top, 
                               24, 
                               24, 
                               SWP_NOOWNERZORDER);

				::ShowWindow(*_icon_wnd_audio_muted, SW_SHOW);
				::UpdateWindow(*_icon_wnd_audio_muted);
			}
		}
	}
	else
	{
		::ShowWindow(*_icon_wnd_pinned, SW_HIDE);
		//if (_show_audio_muted_icon)
		{
			if (_user_name != "" && !_is_fake_video)
			{
				::SetWindowPos(*_icon_wnd_audio_muted, 
                               NULL, 
                               _icon_wnd_user_name->get_user_name_text_width() + 8, 
                               top, 
                               24, 
                               24, 
                               SWP_NOOWNERZORDER);

				::SetWindowPos(*_icon_wnd_user_name, 
                               NULL, 
                               0, 
                               top, 
                               _icon_wnd_user_name->get_user_name_text_width() + 8, 
                               24, 
                               SWP_NOOWNERZORDER);

				if (!indicator_show)
				{
					::ShowWindow(*_icon_wnd_user_name, SW_SHOW);
					::UpdateWindow(*_icon_wnd_user_name);
					::ShowWindow(*_icon_wnd_audio_muted, SW_SHOW);
					::UpdateWindow(*_icon_wnd_audio_muted);
				}
				else
				{
					::RedrawWindow(*_icon_wnd_user_name, 
                                   NULL, 
                                   NULL, 
                                   RDW_INVALIDATE | RDW_ERASE);

					::RedrawWindow(*_icon_wnd_audio_muted, 
                                   NULL, 
                                   NULL, 
                                   RDW_INVALIDATE | RDW_ERASE);
				}
			}
			else
			{
				::ShowWindow(*_icon_wnd_user_name, SW_HIDE);

				::SetWindowPos(*_icon_wnd_audio_muted, 
                               NULL, 
                               0, 
                               top, 
                               24, 
                               24, 
                               SWP_NOOWNERZORDER);

				::ShowWindow(*_icon_wnd_audio_muted, SW_SHOW);
				::UpdateWindow(*_icon_wnd_audio_muted);
			}
		}
		/*else
		{
			::ShowWindow(*_icon_wnd_audio_muted, SW_HIDE);

			if (_user_name != "" && !_is_fake_video)
			{
				::SetWindowPos(*_icon_wnd_user_name, 
                               NULL, 
                               0, 
                               top, 
                               _icon_wnd_user_name->get_user_name_text_width() + 8, 
                               24, 
                               SWP_NOOWNERZORDER);
				::ShowWindow(*_icon_wnd_user_name, SW_SHOW);
			}
			else
			{
				::ShowWindow(*_icon_wnd_user_name, SW_HIDE);
			}
		}*/
	}
}

void VideoWnd::show_audio_muted_icon_wnd(bool show)
{
	_show_audio_muted_icon = show;
	//update_icon_window();
}

void VideoWnd::show_pinned_icon_wnd(bool show)
{
	_show_pinned_icon = show;
}

void  VideoWnd::start_video(BOOL local_preview, 
                            const std::string& msid, 
                            const std::string& uuid)
{
	DebugLog("start video, local preview=%d, msid=%s, uuid=%s", 
              local_preview, msid.c_str(), uuid.c_str());

	if (_icon_wnd_audio_muted == nullptr)
	{
		_icon_wnd_audio_muted = std::make_unique<FRTCStreamIconWindow>(AUDIO_MUTE_STATUS);
		_icon_wnd_audio_muted->Create(WS_EX_LAYERED, 
                                      L"", 
                                      WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
			                          0, 
                                      100, 
                                      24, 
                                      24, 
                                      (HWND)*this, 
                                      NULL, 
                                      g_hInstance, 
                                      FALSE);
		SetLayeredWindowAttributes((HWND)*_icon_wnd_audio_muted, 0, 204, LWA_ALPHA);
	}

	if (_icon_wnd_pinned == NULL)
	{
		_icon_wnd_pinned = std::make_unique<FRTCStreamIconWindow>(PINNED_STATUS);
		_icon_wnd_pinned->Create(0, 
                                 L"", 
                                 WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
			                     0, 
                                 100, 
                                 24, 
                                 24, 
                                 (HWND)*this, 
                                 NULL, 
                                 g_hInstance, 
                                 FALSE);
		//SetLayeredWindowAttributes((HWND)_icon_wnd_pinned, 0, 0x88, LWA_ALPHA);
	}

	if (_icon_wnd_user_name == NULL)
	{
		_icon_wnd_user_name = std::make_unique<FRTCStreamIconWindow>(USER_NAME);
		_icon_wnd_user_name->Create(WS_EX_LAYERED, 
                                    L"", 
                                    WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
			                        0, 
                                    100, 
                                    24, 
                                    24, 
                                    (HWND)*this, 
                                    NULL, 
                                    g_hInstance, 
                                    FALSE);
		SetLayeredWindowAttributes((HWND)*_icon_wnd_user_name, NULL, 204, LWA_ALPHA);
	}

	_icon_wnd_user_name->set_user_name(this->_user_name);

	_is_fake_video = false;
	_show_wnd_cmd = SW_SHOW;
	_is_local_preview = local_preview;
	if (_timer_id != IDT_TIMER_NULL)
	{
		WarnLog("starting video while there is already video active: msid=%s timerID=%u", 
                 _msid.c_str(), _timer_id);
		stop_video();
	}

	_msid = msid;
	_uuid = uuid;

	update_icon_window();

	_need_stop_timer = FALSE;
	_timer_id = IDT_TIMER_VID1;
	::SetTimer(*this, _timer_id, 33, NULL);

	AutoLock lock(_time_lock);
	_set_timer_time = GetTickCount();

	DebugLog("start video finish, set_timer_time=%u", _set_timer_time);
}

void VideoWnd::stop_video()
{
	if (_icon_wnd_audio_muted != nullptr)
	{
		_icon_wnd_audio_muted.reset();
	}

	if (_icon_wnd_pinned != nullptr)
	{
		_icon_wnd_pinned.reset();
	}

	if (_icon_wnd_user_name != nullptr)
	{
		_icon_wnd_user_name.reset();
	}

	::KillTimer(*this, _timer_id);

	_timer_id = IDT_TIMER_NULL;
	_show_wnd_cmd = SW_HIDE;
	_is_local_preview = FALSE;
	_msid = "";
	_uuid = "";
	_need_stop_timer = FALSE;
	_is_fake_video = true;
	_watermark_str = "";
	_is_content_stream = false;
}

void  VideoWnd::start_fake_video(const std::string& msid, const std::string& uuid)
{
	DebugLog("start fake video, msid=%s", msid.c_str());

	if (_icon_wnd_audio_muted == nullptr)
	{
		_icon_wnd_audio_muted = std::make_unique<FRTCStreamIconWindow>(
                                    AUDIO_MUTE_STATUS);
		_icon_wnd_audio_muted->Create(0, 
                                      L"", 
                                      WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
			                          0, 
                                      100, 
                                      ICON_WND_WIDTH, 
                                      24, 
                                      (HWND)*this, 
                                      NULL, 
                                      g_hInstance, 
                                      FALSE);
	}

	if (_icon_wnd_pinned == nullptr)
	{
		_icon_wnd_pinned = std::make_unique<FRTCStreamIconWindow>(PINNED_STATUS);
		_icon_wnd_pinned->Create(0, 
                                 L"", 
                                 WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
			                     0, 
                                 100, 
                                 24, 
                                 24, 
                                 (HWND)*this, 
                                 NULL, 
                                 g_hInstance, 
                                 FALSE);
	}

	if (_icon_wnd_user_name == nullptr)
	{
		_icon_wnd_user_name = std::make_unique<FRTCStreamIconWindow>(USER_NAME);
		_icon_wnd_user_name->set_user_name(this->_user_name);
		_icon_wnd_user_name->Create(0, 
                                    L"", 
                                    WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
			                        0, 
                                    100, 
                                    24, 
                                    24, 
                                    (HWND)*this, 
                                    NULL, 
                                    g_hInstance, 
                                    FALSE);
	}

	_is_fake_video = true;
	_show_wnd_cmd = SW_SHOW;

	std::string strLocalMSID;
	g_frtc_mgr->get_local_preview_msid(strLocalMSID);
	_is_local_preview = (msid == strLocalMSID);
	if (_timer_id != IDT_TIMER_NULL)
	{
		WarnLog("start fake video, already exist an active one, msid=%s timer_id=%u", 
                 _msid.c_str(), _timer_id);
		stop_video();
	}
	_msid = msid;
	_uuid = uuid;

	update_icon_window();

	_need_stop_timer = FALSE;
	_timer_id = IDT_TIMER_VID2;
	::SetTimer(*this, _timer_id, 33, NULL);
}

void VideoWnd::start_timer()
{
	if (*this != NULL && _need_stop_timer)
	{
		_need_stop_timer = FALSE;
		UINT_PTR ptr = ::SetTimer(*this, _timer_id, 33, NULL);
	}
}

void VideoWnd::stop_timer()
{
	if (*this != NULL)
	{
		_need_stop_timer = TRUE;
	}
}

void VideoWnd::set_video_data_zero_check_timer()
{
	AutoLock lock(_time_lock);
	_set_timer_time = GetTickCount();

	DebugLog("set video data zero check timer=%u", _set_timer_time);
}

BOOL  VideoWnd::is_local_video()
{
	return _is_local_preview;
}

void  VideoWnd::set_main_cell(bool isMainCell)
{
	this->_is_main_cell = isMainCell;
}

std::string VideoWnd::get_msid()
{
	return _msid;
}

std::string VideoWnd::get_uuid()
{
	return _uuid;
}

bool VideoWnd::check_stream_uuid(const std::string& UUID) 
{ 
    return _uuid == UUID; 
}

void VideoWnd::draw_dummy_frame(HDC hdc)
{
	if (g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTING || 
        g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTED)
	{
		return;
	}

	if (_is_local_preview)
	{
		BYTE* dummyPic = g_frtc_mgr->get_video_mute_pic();
		if (_use_gdi_render)
		{
			if (_gdi_renderer != nullptr)
			{
				int borderWidth = 5;
				RECT rt;
				GetWindowRect(*this, &rt);
				if (rt.right - rt.left >= 1080)
					borderWidth = 3;
				else if (rt.right - rt.left >= 720)
					borderWidth = 5;
				else if (rt.right - rt.left >= 360)
					borderWidth = 8;
				else if (rt.right - rt.left >= 240)
					borderWidth = 10;
				else
					borderWidth = 12;
				DebugLog("Draw dummy Frame gdi");
				this->_gdi_renderer->Render(hdc, 
                                            dummyPic, 
                                            1280, 
                                            720, 
                                            false, 
                                            borderWidth, 
                                            false, 
                                            "");
			}
		}
		else
		{
			HRESULT hr = _d3d9_renderer->draw_frame(*this, 
                                                    D3DFMT_YV12, 
                                                    dummyPic, 
                                                    1280, 
                                                    720, 
                                                    false, 
                                                    false, 
                                                    "");
			if (!SUCCEEDED(hr))
			{
				ErrorLog("Draw dummy Frame failed : %ld", hr);
			}
		}
	}
	else
	{
		int width = 1280;
		int height = 720;
		int new_width = 0;
        BYTE* buf = NULL;

        RECT rc;
		GetClientRect(*this, &rc);
		if (SMALL_CELL_WIDTH >= (rc.right - rc.left))
		{
			width = 320;
			height = 180;
			buf = g_frtc_mgr->get_name_card_yuv_data(_user_name, 
                                                     width, 
                                                     height, 
                                                     FRTCSDK::NAME_CARD_SMALL, 
                                                     FRTCSDK::FONT_SIZE_MEDIUM, 
                                                     new_width);
		}
		else
		{
			buf = g_frtc_mgr->get_name_card_yuv_data(_user_name, 
                                                     width, 
                                                     height, 
                                                     FRTCSDK::NAME_CARD_MEDIUM, 
                                                     FRTCSDK::FONT_SIZE_SMALL, 
                                                     new_width);
		}

		if (_use_gdi_render)
		{
			if (_gdi_renderer != nullptr)
			{
				int borderWidth = 5;
				RECT rt;
				GetWindowRect(*this, &rt);
				if (rt.right - rt.left >= 1080)
					borderWidth = 3;
				else if (rt.right - rt.left >= 720)
					borderWidth = 5;
				else if (rt.right - rt.left >= 360)
					borderWidth = 8;
				else if (rt.right - rt.left >= 240)
					borderWidth = 10;
				else
					borderWidth = 12;
				if (new_width != 0)
                {
					this->_gdi_renderer->Render(hdc, 
                                                buf, 
                                                320, 
                                                180, 
                                                _has_border, 
                                                borderWidth, 
                                                _is_content_stream, 
                                                _watermark_str);
                }
				else
                {
					this->_gdi_renderer->Render(hdc, 
                                                buf, 
                                                width, 
                                                height, 
                                                _has_border, 
                                                borderWidth, 
                                                _is_content_stream, 
                                                _watermark_str);
                }
			}
		}
		else
		{
			HRESULT hr;
			if (new_width != 0)
            {
				hr = _d3d9_renderer->draw_frame(*this, 
                                                D3DFMT_YV12, 
                                                buf, 
                                                320, 
                                                180, 
                                                _has_border, 
                                                _is_content_stream, 
                                                _watermark_str);
            }
			else
            {
				hr = _d3d9_renderer->draw_frame(*this, 
                                                D3DFMT_YV12, 
                                                buf, 
                                                width, 
                                                height, 
                                                _has_border, 
                                                _is_content_stream, 
                                                _watermark_str);
            }

			if (!SUCCEEDED(hr))
			{
				ErrorLog("Draw dummy Frame failed : %ld", hr);
			}
		}
	}

}

void  VideoWnd::set_border(bool hasBorder)
{
	_has_border = hasBorder;
}

void  VideoWnd::set_is_content_stream(bool isContent)
{
	_is_content_stream = isContent;
}

void  VideoWnd::set_watermark_str(const std::string& watermark)
{
	_watermark_str = watermark;
}

void VideoWnd::draw_frame(HDC hdc)
{
	if (g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTING || 
        g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTED)
	{
		return;
	}

	unsigned int length = 0;
	unsigned int width = 0;
	unsigned int height = 0;
	g_frtc_mgr->get_receive_video(this->_msid, 
                                  &_buffer, 
                                  &length, 
                                  &width, 
                                  &height);
	if (length == 0)
	{
		return;
	}

	std::string _user_name_str = "";
	if (!_is_local_preview)
	{
		_user_name_str = _user_name;
	}
	if (_use_gdi_render)
	{
		if (_gdi_renderer != nullptr)
		{
			int borderWidth = 5;
			RECT rt;
			GetWindowRect(*this, &rt);
			if (rt.right - rt.left >= 1080)
				borderWidth = 3;
			else if (rt.right - rt.left >= 720)
				borderWidth = 5;
			else if (rt.right - rt.left >= 360)
				borderWidth = 8;
			else if (rt.right - rt.left >= 240)
				borderWidth = 10;
			else
				borderWidth = 12;

			this->_gdi_renderer->Render(hdc, 
                                        _buffer, 
                                        width, 
                                        height, 
                                        _has_border, 
                                        borderWidth, 
                                        _is_content_stream, 
                                        _watermark_str);
		}
	}
	else
	{
		HRESULT hr2 = _d3d9_renderer->draw_frame(*this, 
                                                 D3DFMT_YV12, 
                                                 (BYTE*)_buffer, 
                                                 width, 
                                                 height, 
                                                 _has_border, 
                                                 _is_content_stream, 
                                                 _watermark_str);
		if (!SUCCEEDED(hr2))
		{
			ErrorLog("Draw Frame failed : %ld", hr2);
		}
	}
}

void VideoWnd::draw_local_preview(HDC hdc)
{
	if (g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTING || 
        g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTED)
	{
		return;
	}

	std::string local_id = "__local_preview_msid__";
	g_frtc_mgr->get_local_preview_msid(local_id);

	unsigned int length = 0;
	unsigned int width = 0;
	unsigned int height = 0;
	g_frtc_mgr->get_receive_video(local_id, &_buffer, &length, &width, &height);
	if (length == 0)
	{
		return;
	}

	if (g_frtc_mgr->is_local_video_muted())
	{
		g_frtc_mgr->clear_receive_video(local_id);
		DebugLog("isLocalVideoMuted  ClearReceiveVideo");
		return;
	}

	if (_use_gdi_render)
	{
		if (_gdi_renderer != nullptr)
		{
			int borderWidth = 5;
			RECT rt;
			GetWindowRect(*this, &rt);
			if (rt.right - rt.left >= 1080)
				borderWidth = 3;
			else if (rt.right - rt.left >= 720)
				borderWidth = 5;
			else if (rt.right - rt.left >= 360)
				borderWidth = 8;
			else if (rt.right - rt.left >= 240)
				borderWidth = 10;
			else
				borderWidth = 12;

			this->_gdi_renderer->Render(hdc, 
                                        _buffer, 
                                        width, 
                                        height, 
                                        false, 
                                        borderWidth, 
                                        false, 
                                        "");
		}
	}
	else
	{
		HRESULT hr = _d3d9_renderer->draw_frame(*this, 
                                                D3DFMT_YV12, 
                                                (BYTE*)_buffer, 
                                                width, 
                                                height, 
                                                false, 
                                                false, 
                                                "");
		if (!SUCCEEDED(hr))
		{
			ErrorLog("Draw local view failed : %ld", hr);
		}
	}
}

void VideoWnd::check_local_video_data()
{
	if (g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTING || 
        g_frtc_mgr->get_call_state() == FRTC_CALL_STATE::CALL_DISCONNECTED)
	{
		return;
	}

	std::string local_id = "__local_preview_msid__";
	g_frtc_mgr->get_local_preview_msid(local_id);

	unsigned int length = 0;
	unsigned int width = 0;
	unsigned int height = 0;
	g_frtc_mgr->get_receive_video(local_id, &_buffer, &length, &width, &height);
	if (length == 0)
	{
		ErrorLog("No video data");
		g_frtc_mgr->clear_receive_video(local_id);
		g_frtc_mgr->show_camera_failed_reminder();
		return;
	}

	return;
}

bool mouseEnteredVid = false;
BOOL RegisterMouseTrackEvent(HWND hWnd);
BOOL RegisterMouseTrackEvent(HWND hWnd)
{
	TRACKMOUSEEVENT mev{ 0 };
	mev.hwndTrack = hWnd;
	mev.dwFlags = TME_LEAVE;
	mev.dwHoverTime = HOVER_DEFAULT;
	mev.cbSize = sizeof(mev);
	BOOL ret = TrackMouseEvent(&mev);
	if (!ret)
	{
		ErrorLog("TrackMouseEvent failed, error is %d", GetLastError());
	}
	return ret;
}

void VideoWnd::update_user_name(const std::string& user_name_str)
{
	if (_is_local_preview)
		return;
	if (this->_user_name != user_name_str)
	{
		this->_user_name = user_name_str;
		if (this->_icon_wnd_user_name)
			this->_icon_wnd_user_name->set_user_name(user_name_str);
	}
}

void VideoWnd::get_user_name(std::string* user_name_ptr)
{
	if(user_name_ptr)
		*user_name_ptr = this->_user_name;
}

LRESULT VideoWnd::WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    if (message == WM_NCPAINT)
    {
		RECT rc;
		GetWindowRect(hWnd, &rc);
		OffsetRect(&rc, -rc.left, -rc.top);
		auto hdc = GetWindowDC(hWnd);
		auto hpen = CreatePen(PS_SOLID, 2, RGB(0, 0, 0));
		auto oldpen = SelectObject(hdc, hpen);
		SelectObject(hdc, GetStockObject(NULL_BRUSH));
		Rectangle(hdc, rc.left, rc.top, rc.right, rc.bottom);
		SelectObject(hdc, oldpen);
		DeleteObject(oldpen);
		ReleaseDC(hWnd, hdc);

		return 0;
    }
    else if (message == WM_CREATE)
    {
		WTSRegisterSessionNotification(hWnd, NOTIFY_FOR_THIS_SESSION);
		return DefWindowProc(hWnd, message, wParam, lParam);
    }
    else if (message == WM_DESTROY)
    {
        WTSUnRegisterSessionNotification(hWnd);
    }
    else if (message == WM_WTSSESSION_CHANGE)
    {
        if (wParam == WTS_SESSION_UNLOCK)
        {
			HRESULT hr = _d3d9_renderer->test_cooperative_level();
			WarnLog("WTS_SESSION_UNLOCK, test_cooperative_level result = 0x%X", hr);
			if (S_OK != hr)
			{
				hr = _d3d9_renderer->reset_d3d9_device();
				ErrorLog("WTS_SESSION_UNLOCK, reset_d3d9_device result = 0x%X", hr);
			}
        }
    }
    else if (message == WM_WINDOWPOSCHANGED)
    {
        return DefWindowProc(hWnd, message, wParam, lParam);
    }
    else if (message == WM_LBUTTONDBLCLK)
    {
		_click_start_time = GetTickCount();
		::KillTimer(*this, IDT_TIMER_CLICK);

		g_frtc_mgr->mouse_event_callback(message);

		if (this->_is_main_cell)
		{
			g_frtc_mgr->set_current_view(this);
			return 1;
		}
    }
    else if (message == WM_LBUTTONUP)
    {
		DWORD now = GetTickCount();
		UINT double_click_time = GetDoubleClickTime();
		if (_click_start_time != 0 && 
            (now - _click_start_time) > double_click_time)
		{
			::SetTimer(*this, IDT_TIMER_CLICK, double_click_time, NULL);
		}
		else if (_click_start_time == 0)
		{
			::SetTimer(*this, IDT_TIMER_CLICK, double_click_time, NULL);
		}

		_click_start_time = GetTickCount();

		return 1;
    }
    else if (message == WM_PAINT)
    {
        PAINTSTRUCT ps;
		_hdc = ::BeginPaint(hWnd, &ps);
		if (_use_gdi_render)
		{
			if (_is_fake_video)
			{
				draw_dummy_frame(_hdc);
			}
			else
			{
				if (_is_local_preview) 
                {
					draw_local_preview(_hdc);
					AutoLock lock(_time_lock);
					if (_set_timer_time != 0)
					{
						DWORD now = GetTickCount();
						DWORD diff = now - _set_timer_time;
						if (diff >= 3000)
						{
							check_local_video_data();
							_set_timer_time = now;
						}
					}
				}
				else 
                {
					draw_frame(_hdc);
				}
			}
		}

		::EndPaint(hWnd, &ps);
		return 1;
    }
    else if (message == WM_TIMER)
    {
        if (wParam == IDT_TIMER_VID1)
        {
			if (_need_stop_timer)
			{
				DebugLog("stop timer, msid = %s", _msid.c_str());
				::KillTimer(*this, IDT_TIMER_VID1);
				return 1;
			}

			if (_use_gdi_render)
			{
				RECT r{ 0 };
				GetClientRect(hWnd, &r);
				InvalidateRect(hWnd, &r, FALSE);
			}
			else if (_is_local_preview) 
            {
				draw_local_preview();
				AutoLock lock(_time_lock);
				if (_set_timer_time != 0)
				{
					DWORD now = GetTickCount();
					DWORD diff = now - _set_timer_time;
					if (diff >= 3000)
					{
						check_local_video_data();
						_set_timer_time = now;
					}
				}

			}
			else 
            {
				draw_frame();
				//update_icon_window();
			}
        }
        else if (wParam == IDT_TIMER_VID2)
        {
			if (_need_stop_timer)
			{
				DebugLog("stop timer, msid = %s", _msid.c_str());
				::KillTimer(*this, IDT_TIMER_VID2);
				return 1;
			}

			if (_use_gdi_render)
			{
				RECT r{ 0 };
				GetClientRect(hWnd, &r);
				InvalidateRect(hWnd, &r, FALSE);
			}
			else
            {
				draw_dummy_frame();
			    //update_icon_window();
            }
        }
        else if (wParam == IDT_TIMER_CLICK)
        {
            g_frtc_mgr->mouse_event_callback(WM_LBUTTONUP);
			::KillTimer(*this, IDT_TIMER_CLICK);
        }
        else if (wParam == IDT_TIMER_MOUSEENTER_VID)
        {
			g_frtc_mgr->mouse_event_callback(WM_MOUSEHOVER);
			::KillTimer(*this, IDT_TIMER_MOUSEENTER_VID);
        }
        else if (wParam == IDT_TIMER_MOUSELEAVE_VID)
        {
			g_frtc_mgr->mouse_event_callback(WM_MOUSELEAVE);
			::KillTimer(*this, IDT_TIMER_MOUSELEAVE_VID);
        }

		return 1;
    }
    else if (message == WM_CLOSE)
    {
        return 0;
    }
    else
    {
        return DefWindowProc(hWnd, message, wParam, lParam);
    }

	return 0;
}

FRTCGDIRender::FRTCGDIRender() 
    : hwnd_(NULL),
      lastFrameHasBorder_(false), 
      pArgbBuffer_(NULL), 
      bufferWidth_(0), 
      bufferHeight_(0)
{

}

FRTCGDIRender::~FRTCGDIRender()
{
	if (pArgbBuffer_)
	{
		delete[] pArgbBuffer_;
		pArgbBuffer_ = NULL;
	}
}

void FRTCGDIRender::SetHWND(HWND hwnd)
{
	hwnd_ = hwnd;
}

void FRTCGDIRender::Render(HDC hdc, 
                           void* frameBuffer, 
                           int width, 
                           int height, 
                           bool hasBorder, 
                           int borderWidth, 
                           bool isContent, 
                           const std::string& water_mark_str)
{
	HRESULT hr;
	int resizeYuvW = width;
	int resizeYuvH = height;
	BYTE* newImage = NULL;
	bool needResize = false;

	//for debug
	if (width <= 0 || height <= 0 || width > 1920 || height > 1080)
	{
		return;
	}

	if (frameBuffer == NULL)
	{
		return;
	}

	int interval = time(0) - g_frtc_mgr->get_name_card_begin_time();

	if (isContent)
	{
		resizeYuvW = 1920;
		resizeYuvH = 1080;
		if (newImage == NULL)
			newImage = new BYTE[resizeYuvW * resizeYuvH * 3 / 2];
		int retResize = g_frtc_mgr->resize_yuv420((unsigned char*)frameBuffer, 
                                                  width, 
                                                  height, 
                                                  newImage, 
                                                  resizeYuvW, 
                                                  resizeYuvH);

		if (0 != retResize)
		{
			delete[] newImage;
			return;
		}
	}
	else
	{
		newImage = (BYTE*)frameBuffer;
	}

	if (isContent && (water_mark_str.length() > 0))
	{
		int new_width = 0;
		int w = resizeYuvW;
		int h = resizeYuvH;
		BYTE* waterMarkImage = g_frtc_mgr->get_watermark_yuv_data(water_mark_str, 
                                                                  w, 
                                                                  h, 
                                                                  new_width);
		if (new_width == 320)
		{
			FRTCSDK::FRTCSdkUtil::merge_yuv420(waterMarkImage, 
                                               320, 
                                               180, 
                                               newImage, 
                                               resizeYuvW, 
                                               resizeYuvH);
		}
		else
		{
			FRTCSDK::FRTCSdkUtil::merge_yuv420(waterMarkImage, 
                                               w, 
                                               h, 
                                               newImage, 
                                               resizeYuvW, 
                                               resizeYuvH);
		}
	}

	RECT r;
	GetClientRect(hwnd_, &r);
	int wndWidth = r.right - r.left;
	int wndHeight = r.bottom - r.top;

	if (bufferWidth_ != resizeYuvW || bufferHeight_ != resizeYuvH)
	{
		if (pArgbBuffer_)
		{
			delete[] pArgbBuffer_;
			pArgbBuffer_ = NULL;
		}
	}
	if (!pArgbBuffer_)
	{
		pArgbBuffer_ = new unsigned char[resizeYuvW * resizeYuvH * 4];
		bufferWidth_ = resizeYuvW;
		bufferHeight_ = resizeYuvH;
	}

	FRTCSDK::FRTCSdkUtil::yuv420_to_argb(newImage, 
                                         newImage + resizeYuvW * resizeYuvH, 
                                         newImage + resizeYuvW * resizeYuvH * 5 / 4, 
                                         (BYTE*)pArgbBuffer_, 
                                         resizeYuvW, 
                                         resizeYuvH);

	if (isContent && newImage != NULL)
	{
		delete[] newImage;
	}

	RECT srcRect, destRect;
	::SetRect(&srcRect, 0, 0, resizeYuvW, resizeYuvH);
	if (hasBorder)
	{
		if (!lastFrameHasBorder_)
		{
			//draw 4 rectangles to represent a border
			HBRUSH borderBrush = CreateSolidBrush(RGB(60, 210, 60));

			RECT rTop{ 0 };
			rTop.left = r.left;
			rTop.right = r.right;
			rTop.top = r.top;
			rTop.bottom = r.top + borderWidth;
			FillRect(hdc, &rTop, borderBrush);

			RECT rLeft{ 0 };
			rLeft.left = r.left;
			rLeft.right = r.left + borderWidth;
			rLeft.top = r.top;
			rLeft.bottom = r.bottom;
			FillRect(hdc, &rLeft, borderBrush);

			RECT rBottom{ 0 };
			rBottom.left = r.left;
			rBottom.right = r.right;
			rBottom.top = r.bottom - borderWidth;
			rBottom.bottom = r.bottom;
			FillRect(hdc, &rBottom, borderBrush);

			RECT rRight{ 0 };
			rRight.left = r.right - borderWidth;
			rRight.right = r.right;
			rRight.top = r.top;
			rRight.bottom = r.bottom;
			FillRect(hdc, &rRight, borderBrush);

			DeleteObject(borderBrush);
		}

		::SetRect(&destRect, 
                  borderWidth, 
                  borderWidth, 
                  wndWidth - borderWidth, 
                  wndHeight - borderWidth);
	}
	else
	{
		if (lastFrameHasBorder_)
		{
			HBRUSH borderBrush = CreateSolidBrush(RGB(0, 0, 0));
			FillRect(hdc, &r, borderBrush);
			DeleteObject(borderBrush);
		}

		::SetRect(&destRect, 0, 0, wndWidth, wndHeight);
	}
	lastFrameHasBorder_ = hasBorder;

	//BMP Header
	BITMAPINFO m_bmphdr = { 0 };
	DWORD dwBmpHdr = sizeof(BITMAPINFO);
	//24bit
	m_bmphdr.bmiHeader.biBitCount = 32;
	m_bmphdr.bmiHeader.biClrImportant = 0;
	m_bmphdr.bmiHeader.biSize = dwBmpHdr;
	m_bmphdr.bmiHeader.biSizeImage = 0;
	m_bmphdr.bmiHeader.biWidth = resizeYuvW;
	//Notice: BMP storage pixel data in opposite direction of Y-axis (from bottom to top).
	//So we must set reverse biHeight to show image correctly.
	//m_bmphdr.bmiHeader.biHeight = -pixel_h;
	m_bmphdr.bmiHeader.biHeight = -resizeYuvH;
	m_bmphdr.bmiHeader.biXPelsPerMeter = 0;
	m_bmphdr.bmiHeader.biYPelsPerMeter = 0;
	m_bmphdr.bmiHeader.biClrUsed = 0;
	m_bmphdr.bmiHeader.biPlanes = 1;
	m_bmphdr.bmiHeader.biCompression = BI_RGB;

	//Draw data
	//DebugLog("Draw video pic gdi, window width=%d, height=%d, pic width=%d, height=%d", 
    //          wndWidth, wndHeight, resizeYuvW, resizeYuvH);
	int nResult = StretchDIBits(hdc,
		hasBorder ? borderWidth : 0, hasBorder ? borderWidth : 0,
		destRect.right - destRect.left, destRect.bottom - destRect.top,
		0, 0,
		resizeYuvW, resizeYuvH,
		pArgbBuffer_,
		&m_bmphdr,
		DIB_RGB_COLORS,
		SRCCOPY);

	//if (hasBorder)
	//{
	//	// Color fill the back buffer.
	//	HBRUSH borderBrush = CreateSolidBrush(RGB(60, 210, 60));
	//	FrameRect(hdc, &r, borderBrush);
	//	DeleteObject(borderBrush);
	//}
	//else
	//{
	//	if (lastFrameHasBorder_)
	//	{
	//		HBRUSH borderBrush = CreateSolidBrush(RGB(0, 0, 0));
	//		FillRect(hdc, &r, borderBrush);
	//		DeleteObject(borderBrush);
	//	}
	//	::SetRect(&destRect, 0, 0, wndWidth, wndHeight);
	//}
	//lastFrameHasBorder_ = hasBorder;
}
