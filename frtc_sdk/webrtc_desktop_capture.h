#pragma once

#include <Windows.h>
#include <desktop_capturer.h>

#include <thread>
#include <mutex>

typedef enum webrtc_capture_type
{
	window_capture,
	desktop_capture
};

class IWebRTCCaptureCallback
{
public:
	virtual void OnCapture(void* buffer, int width, int height) = 0;
	virtual void OnCaptureError() = 0;
};

class webrtc_desktop_capture : webrtc::DesktopCapturer::Callback
{
public:
	webrtc_desktop_capture();
	~webrtc_desktop_capture();

	virtual void OnCaptureResult(webrtc::DesktopCapturer::Result result,
		std::unique_ptr<webrtc::DesktopFrame> frame);
public:
	void Init(webrtc_capture_type type);
	void SetInterval(int interval);
	void SetCaptureCallback(IWebRTCCaptureCallback* callback);
	void SetCaptureSource(HWND hWnd);
	void SetCaptureSource(int MonitorIndex);
	void GetCaptureSource(webrtc::DesktopCapturer::SourceList& sourceList);
	void Start();
	void Stop();
private:
	static void capture_timer_proc(webrtc_desktop_capture* pCapture);
	void capture_frame();
	void start_wnd_close_timer();
	void stop_wnd_close_timer();
	BOOL bring_window_to_front(HWND hwnd);
	static void NTAPI wnd_close_timer_proc(PTP_CALLBACK_INSTANCE Instance, PVOID Context, PTP_TIMER Timer);
	static BOOL CALLBACK GetWindowListHandler(HWND hwnd, LPARAM lparam);

private:
	bool capture_started_ = false;
	bool stop_capture_ = false;
	int interval_ =  1000 / 30;
	IWebRTCCaptureCallback* callback_;
	HWND capture_wnd_hwnd_;
	PTP_TIMER wnd_close_monitor_timer_ = NULL;

protected:
	webrtc_capture_type capture_type_;
	std::unique_ptr<webrtc::DesktopCapturer> capturer_;
	std::unique_ptr<webrtc::DesktopFrame> frame_;
	std::unique_ptr<std::thread> capture_timer_thread_;
	std::shared_ptr<std::mutex> mut_ = std::make_shared<std::mutex>();
	std::shared_ptr<std::condition_variable> cv_ = std::make_shared<std::condition_variable>();
};

