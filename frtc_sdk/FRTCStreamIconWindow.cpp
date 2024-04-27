#include "FRTCStreamIconWindow.h"
#include "frtc_sdk_util.h"
#include "frtccall_manager.h"
#include "resource.h"

#include <memory>

extern std::shared_ptr<FrtcManager> g_frtc_mgr;
extern HINSTANCE g_hInstance;

static FRTCStreamIconWindow* g_state_wind;

Gdiplus::Image* FRTCStreamIconWindow::_audio_muted_icon_img = NULL;
Gdiplus::Image* FRTCStreamIconWindow::_audio_unmuted_icon_img = NULL;
Gdiplus::Image* FRTCStreamIconWindow::_video_pinned_icon_img = NULL;

FRTCStreamIconWindow::FRTCStreamIconWindow(ICON_TYPE type) :FRTCBaseWnd(false)
{
	g_state_wind = this;
	_icon_type = type;
	if (_audio_muted_icon_img == NULL)
	{
		_audio_muted_icon_img = FRTCSDK::FRTCSdkUtil::load_image_from_resource(
                                g_hInstance, MAKEINTRESOURCE(IDB_PNG2), L"PNG");
	}

	if (_audio_unmuted_icon_img == NULL)
	{
		_audio_unmuted_icon_img = FRTCSDK::FRTCSdkUtil::load_image_from_resource(
                                  g_hInstance, MAKEINTRESOURCE(IDB_PNG3), L"PNG");
	}

	if (_video_pinned_icon_img == NULL)
	{
		_video_pinned_icon_img = FRTCSDK::FRTCSdkUtil::load_image_from_resource(
                                 g_hInstance, MAKEINTRESOURCE(IDB_PNG5), L"PNG");
	}
}

FRTCStreamIconWindow::~FRTCStreamIconWindow()
{
}

void FRTCStreamIconWindow::set_user_name(std::string name)
{
	if (_user_name_str != name)
	{
		_user_name_str = name;
		::UpdateWindow(*this);
	}
}

float FRTCStreamIconWindow::get_user_name_text_width()
{
	return _user_name_text_width;
}

void FRTCStreamIconWindow::set_audio_mute_status(bool mute)
{
	_is_audio_muted = mute;
	PostMessage((HWND)*this, UPDATE_THIS_WND, 0, 0);
}

LRESULT FRTCStreamIconWindow::WndProc(HWND hWnd, 
                                      UINT message, 
                                      WPARAM wParam, 
                                      LPARAM lParam)
{
	switch (message)
	{
	case WM_PAINT:
	{
		RECT rc1;
		PAINTSTRUCT ps1;
		HDC hdc1 = ::BeginPaint(hWnd, &ps1);

		if (_icon_type == AUDIO_MUTE_STATUS)
		{
			int w = 24;
			int h = 24;
			Gdiplus::Graphics graphics(hdc1);
			if (_is_audio_muted)
				graphics.DrawImage(_audio_muted_icon_img, 4, 4, 16, 16);
			else
				graphics.DrawImage(_audio_unmuted_icon_img, 4, 4, 16, 16);

		}
		else if (_icon_type == PINNED_STATUS)
		{
			int w = 24;
			int h = 24;
			Gdiplus::Graphics graphics(hdc1);
			graphics.DrawImage(_video_pinned_icon_img, 4, 4, 16, 16);
		}
		else if (_icon_type == USER_NAME)
		{
			Gdiplus::Graphics graphics(hdc1);
			Gdiplus::SolidBrush bkColor((Gdiplus::Color(00, 00, 00)));


			Gdiplus::FontFamily  fontFamily(L"Arial");
			float                fontSize = 14;
			Gdiplus::PointF      pointF(2, 1);
			Gdiplus::SolidBrush  whiteBrush(Gdiplus::Color(255, 255, 255));

			Gdiplus::Font font(&fontFamily, 
                               fontSize, 
                               Gdiplus::FontStyleRegular, 
                               Gdiplus::UnitPixel);
			
			std::string strTmp = FRTCSDK::FRTCSdkUtil::get_ansi_string(_user_name_str);
			std::string display_name_str;
			if (strTmp.size() > 20)
			{
				display_name_str = strTmp.substr(0, 12) + "...";
			}
			else
			{
				display_name_str = strTmp;
			}

			std::wstring display_name_w = FRTCSDK::FRTCSdkUtil::string_to_wstring(display_name_str);

			std::wstring fitted_str = L"";
            int wv = ICON_WND_WIDTH - 24 - 24;
			fitted_str = FRTCSDK::FRTCSdkUtil::get_fitted_string(&graphics, 
                                                                 &font, 
                                                                 display_name_w, 
                                                                 wv);

			Gdiplus::RectF gRect(0, 0, 0, 0);
			graphics.MeasureString(fitted_str.c_str(), -1, &font, pointF, &gRect);
			this->_user_name_text_width = gRect.Width;
			graphics.FillRectangle(&bkColor, 0, 0, (int)gRect.Width + 8, 24);
			pointF.X += 4;
			pointF.Y += 4;
			graphics.DrawString(fitted_str.c_str(), -1, &font, pointF, &whiteBrush);
		}

		DeleteObject(hdc1);
		::EndPaint(hWnd, &ps1);
	}
	break;
	case UPDATE_THIS_WND:
	{
		::InvalidateRect(hWnd, NULL, TRUE);
		::UpdateWindow(hWnd);
	}
	break;
	}
    
	return DefWindowProc(hWnd, message, wParam, lParam);
}
