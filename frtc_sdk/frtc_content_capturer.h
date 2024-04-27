#pragma once

#ifndef _FRTCCONTENTCAPTURER__
#define __FRTCCONTENTCAPTURER__

#include <Windows.h>

#include "auto_release_object.h"
#include "webrtc_desktop_capture.h"
#include "DXGI_DesktopDuplicator.h"

#include <memory>
#include <string>
#include <thread>


#define RESOLUTION_ADAPTER_16x9(w, h)	\
    if(w * 9 > h * 16)	{				\
        h = w * 9 / 16;					\
        h = (h + 1)/2 * 2;              \
    } else {				     		\
        w = h * 16 / 9;                 \
        w = (w + 1) / 2 * 2; }


enum frtc_capture_type
{
	window,
	monitor,
	none,
};


class interface_frtc_capture_content_callback
{
public:
	virtual void on_capture(void* buffer, int width, int height) = 0;
	virtual void on_capture_error() = 0;
};

class gdi_desktop_capturer
{
public:
	gdi_desktop_capturer();
	~gdi_desktop_capturer();

	void init(int monitor_index, LPCWSTR monitor_id);
	void set_capture_callback(interface_frtc_capture_content_callback* callback);

	HRESULT capture_to_buf();
private:
	HRESULT capture_to_bitmap(HBITMAP& bitmap);
	HRESULT capture_rect_to_bitmap(HBITMAP& bitmap, RECT rect);
	HRESULT capture_cursor(HDC hDC, const RECT& rect);
	HRESULT allocate_bitmap(SIZE rectSize, HDC compatiableHDC, HBITMAP& hBitmap);
	BOOL do_bit_blt(HDC hdc, int destX, int destY, int width, int height, HDC src_dc, int srcX, int srcY);
	BOOL cursor_point_adapter(RECT& deskRect);

	//void capture_callback(void* buffer, int width, int height);
	//void capture_error_callback(void* buffer, int width, int height);

private:
	int _monitor_index;
	std::wstring _monitor_id;
	AutoReleaseHDC _auto_hdc;
	AutoReleaseHDC _auto_desktop_dc;
	AutoReleaseHGDI _auto_bitmap_cache;

	void* _capture_buffer;
	DWORD _capture_buffer_len;
	int _capture_monitor_w;
	int _capture_monitor_h;

	interface_frtc_capture_content_callback* _capture_callback;
};

class frtc_content_capturer
{
public:
	frtc_content_capturer();
	~frtc_content_capturer();

	bool init(frtc_capture_type type, int content_source_id, const wchar_t* content_source_name);
	void set_capture_callback(interface_frtc_capture_content_callback* callback);
	void set_webrtc_capture_callback(IWebRTCCaptureCallback* webrtc_callback);

	void get_content_source_windows_list(std::string& list_json_str, HWND excluded_hwnd);

	void start(bool use_minimize_rate = false);
	void stop();

private:
	static void monitor_capture_timer_proc(frtc_content_capturer* capture);

private:
	frtc_capture_type _type;
	int _source_id;
	bool _use_minimize_rate = false;
	bool _use_dxgi_desktop_capturer = false;
	std::unique_ptr<webrtc_desktop_capture> _webrtc_capturer;
	std::unique_ptr<DXGI_DesktopDuplicator> _dxgi_desktop_capturer;
	std::unique_ptr<gdi_desktop_capturer> _gdi_desktop_capturer;

	interface_frtc_capture_content_callback* _callback;
	IWebRTCCaptureCallback* _webrtc_capture_callback;

	bool _monitor_capture_started = false;
	bool _stop_monitor_capture = false;

	std::unique_ptr<std::thread> _monitor_capture_timer_thread;
};


#endif