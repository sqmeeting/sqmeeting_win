#pragma once
#include "stdafx.h"
#include "FRTCBaseWnd.h"

#define ICON_WND_WIDTH  254
#define UPDATE_THIS_WND (WM_USER + 1229)

enum ICON_TYPE
{
	AUDIO_MUTE_STATUS,
	PINNED_STATUS,
	USER_NAME
};

class FRTCStreamIconWindow : public FRTCBaseWnd
{
public:
	FRTCStreamIconWindow(ICON_TYPE type);
	~FRTCStreamIconWindow();

	void set_user_name(std::string name);
	float get_user_name_text_width();
	void set_audio_mute_status(bool mute);

	virtual LRESULT WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);

private:
	std::string _user_name_str;
	ICON_TYPE _icon_type;
	float _user_name_text_width = 0.0;
	bool _is_audio_muted = false;

	static Gdiplus::Image* _audio_muted_icon_img;
	static Gdiplus::Image* _audio_unmuted_icon_img;
	static Gdiplus::Image* _video_pinned_icon_img;
};

