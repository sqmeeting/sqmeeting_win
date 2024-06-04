#include "webrtc_desktop_capture.h"

#include "modules/desktop_capture/desktop_capture_options.h"
#include "modules/desktop_capture/win/screen_capturer_win_directx.h"

#include <algorithm>
#include <condition_variable>

#include "psapi.h"
#include <cctype>

#include "log.h"

extern HMODULE g_hInstance;


bool IsWindowResponding(HWND window);
bool IsWindowResponding(HWND window) {
	// 50ms is chosen in case the system is under heavy load, but it's also not
	// too long to delay window enumeration considerably.
	const UINT uTimeoutMs = 50;
	return SendMessageTimeout(window, WM_NULL, 0, 0, SMTO_ABORTIFHUNG, uTimeoutMs,
		nullptr);
}

bool IsWindowOwnedByCurrentProcess(HWND hwnd);
bool IsWindowOwnedByCurrentProcess(HWND hwnd) {
	DWORD process_id;
	GetWindowThreadProcessId(hwnd, &process_id);
	return process_id == GetCurrentProcessId();
}

bool IsWindowZeroSize(HWND hwnd);
bool IsWindowZeroSize(HWND hwnd) {
	RECT r{ 0 };
	if (GetWindowRect(hwnd, &r))
	{
		return r.right == r.left || r.top == r.bottom;
	}
	return false;
}

std::string ToUtf8(const wchar_t* wide, size_t len);
std::string ToUtf8(const wchar_t* wide, size_t len) {
	if (len == 0)
		return std::string();
	int len8 = ::WideCharToMultiByte(CP_UTF8, 0, wide, static_cast<int>(len),
		nullptr, 0, nullptr, nullptr);
	std::string ns(len8, 0);
	::WideCharToMultiByte(CP_UTF8, 0, wide, static_cast<int>(len), &*ns.begin(),
		len8, nullptr, nullptr);
	return ns;
}

webrtc_desktop_capture::webrtc_desktop_capture()
	:
	frame_(nullptr),
	capture_wnd_hwnd_(NULL),
	callback_(NULL),
	capture_type_(webrtc_capture_type::window_capture)
{

}

webrtc_desktop_capture::~webrtc_desktop_capture()
{
	if (capture_started_)
		Stop();
	frame_.reset();
	capturer_.reset();
	capturer_ = nullptr;
	callback_ = nullptr;
}

void webrtc_desktop_capture::Init(webrtc_capture_type type)
{

	if (capturer_ == nullptr)
	{
		if (type == webrtc_capture_type::window_capture)
		{
			webrtc::DesktopCaptureOptions options(webrtc::DesktopCaptureOptions::CreateDefault());
			options.set_detect_updated_region(true);
			options.set_allow_directx_capturer(true);
			options.set_enumerate_current_process_windows(false);
			options.set_allow_cropping_window_capturer(true);
			//options.set_allow_wgc_capturer(true);
			capturer_ = webrtc::DesktopCapturer::CreateWindowCapturer(options);
			if (capturer_ == nullptr)
			{
				ErrorLog("webrtc::DesktopCapturer::CreateWindowCapturer failed");
			}
		}
		else
		{
			if (webrtc::ScreenCapturerWinDirectx::IsSupported())
			{
				webrtc::DesktopCaptureOptions options(webrtc::DesktopCaptureOptions::CreateDefault());
				options.set_allow_directx_capturer(true);
				capturer_ = webrtc::DesktopCapturer::CreateScreenCapturer(options);
			}
			else
				capturer_ = webrtc::DesktopCapturer::CreateScreenCapturer(webrtc::DesktopCaptureOptions::CreateDefault());
		}
	}
}

void webrtc_desktop_capture::SetInterval(int interval)
{
	interval_ = interval;
}

void webrtc_desktop_capture::SetCaptureCallback(IWebRTCCaptureCallback* callback)
{
	callback_ = callback;
}

void webrtc_desktop_capture::OnCaptureResult(webrtc::DesktopCapturer::Result result,
	std::unique_ptr<webrtc::DesktopFrame> frame)
{
	if (capture_wnd_hwnd_ && IsIconic(capture_wnd_hwnd_))//minimized window, send NULL data
	{
		if (callback_)
		{
			callback_->OnCapture(NULL, 0, 0);
		}
	}
	else
	{
		if (result != webrtc::DesktopCapturer::Result::SUCCESS)
		{
			if (result == webrtc::DesktopCapturer::Result::ERROR_PERMANENT || !IsWindow(capture_wnd_hwnd_))
			{
				if (callback_)
					callback_->OnCaptureError();
				Stop();
				ErrorLog("OnCaptureResult ERROR_PERMANENT or selected window closed, stop capture");
			}
			else
				return;
		}
		else if (frame)
		{
			frame_ = std::move(frame);
			if (callback_ && !frame_->size().is_empty())
			{
				callback_->OnCapture(frame_->data(), frame_->size().width(), frame_->size().height());
			}
		}
	}
}

void webrtc_desktop_capture::SetCaptureSource(HWND hWnd)
{
	if (capturer_ != nullptr)
	{
		webrtc::DesktopCapturer::SourceList sourceList;
		this->GetCaptureSource(sourceList);

		webrtc::DesktopCapturer::SourceList::const_iterator ret =
			std::find_if(sourceList.begin(), sourceList.end(),
				[hWnd](webrtc::DesktopCapturer::Source& s) { return s.id == (intptr_t)hWnd; });
		if (ret != sourceList.end())
		{
			bring_window_to_front(hWnd);
			if (capturer_->SelectSource(ret->id))
			{
				capturer_->FocusOnSelectedSource();
				capture_wnd_hwnd_ = hWnd;
				start_wnd_close_timer();
			}
			else
			{
				ErrorLog("SetCaptureSource failed, capturer_ select source failed");
			}
		}
		else
		{
			capture_wnd_hwnd_ = NULL;
			ErrorLog("SetCaptureSource failed, specified hwnd not found");
		}
	}
	else
	{
		capture_wnd_hwnd_ = NULL;
		ErrorLog("SetCaptureSource failed, capturer is null");
	}
}
void webrtc_desktop_capture::SetCaptureSource(int MonitorIndex)
{
	if (capturer_ != nullptr)
	{
		webrtc::DesktopCapturer::SourceList sourceList;
		capturer_->GetSourceList(&sourceList);
		webrtc::DesktopCapturer::SourceList::const_iterator ret =
			std::find_if(sourceList.begin(), sourceList.end(),
				[MonitorIndex](webrtc::DesktopCapturer::Source& s) { return s.id == (intptr_t)(MonitorIndex - 1); });
		if (ret != sourceList.end())
		{
			capturer_->SelectSource(ret->id);
		}
	}
}

void webrtc_desktop_capture::GetCaptureSource(webrtc::DesktopCapturer::SourceList& sourceList)
{
	if (capturer_ != nullptr)
	{
		if (capture_type_ == webrtc_capture_type::window_capture)
		{
			//webrtc::DesktopCapturer::SourceList list;
			::EnumWindows(&GetWindowListHandler,
				reinterpret_cast<LPARAM>(&sourceList));
			//sourceList = list;
		}
		else
			capturer_->GetSourceList(&sourceList);
	}
}

void webrtc_desktop_capture::Start()
{
	if (capturer_ != nullptr && !capture_started_)
	{
		capturer_->Start(this);
		frame_.reset();
		stop_capture_ = false;
		capture_timer_thread_.reset();
		capture_timer_thread_ = std::make_unique<std::thread>(capture_timer_proc, this);
		capture_timer_thread_->detach();
		capture_started_ = true;
	}
}

void webrtc_desktop_capture::capture_frame()
{
	frame_.reset();
	capturer_->CaptureFrame();
}

void webrtc_desktop_capture::Stop()
{
	if (stop_capture_)
		return;

	stop_capture_ = true;
	stop_wnd_close_timer();
	std::this_thread::sleep_for(std::chrono::milliseconds(500));
	if (capture_timer_thread_ && capture_timer_thread_->native_handle())
		TerminateThread(capture_timer_thread_->native_handle(), 0);
	capture_started_ = false;
	if(frame_)
		frame_.reset();
}

void webrtc_desktop_capture::start_wnd_close_timer()
{
	wnd_close_monitor_timer_ = CreateThreadpoolTimer(webrtc_desktop_capture::wnd_close_timer_proc, this, NULL);
	if (wnd_close_monitor_timer_)
	{
		ULARGE_INTEGER due;
		due.QuadPart = (1000 * 10000LL) * -1;

		FILETIME ft;
		ft.dwHighDateTime = due.HighPart;
		ft.dwLowDateTime = due.LowPart;

		SetThreadpoolTimer(wnd_close_monitor_timer_, &ft, 1000/*msPeriod*/, 0 /*msWindowLength*/);
	}
}

void webrtc_desktop_capture::stop_wnd_close_timer()
{
	if (wnd_close_monitor_timer_)
	{
		SetThreadpoolTimer(wnd_close_monitor_timer_, 0, 0, 0);
		WaitForThreadpoolTimerCallbacks(wnd_close_monitor_timer_, TRUE);
		CloseThreadpoolTimer(wnd_close_monitor_timer_);
		wnd_close_monitor_timer_ = 0;
	}
}

void webrtc_desktop_capture::capture_timer_proc(webrtc_desktop_capture* pCapture)
{
	std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	pCapture->frame_.reset();
	while (!pCapture->stop_capture_)
	{
		pCapture->capture_frame();
		std::this_thread::sleep_for(std::chrono::milliseconds(1000 / pCapture->interval_));
	}
	pCapture->frame_.reset();
	pCapture->capture_timer_thread_.release();
}

BOOL CALLBACK webrtc_desktop_capture::GetWindowListHandler(HWND hwnd, LPARAM param) {
	webrtc::DesktopCapturer::SourceList* list = reinterpret_cast<webrtc::DesktopCapturer::SourceList*>(param);

	// Skip invisible /*and minimized*/ windows
	if (!IsWindowVisible(hwnd) /* || IsIconic(hwnd)*/) {
		return TRUE;
	}

	// Skip windows which are not presented in the taskbar,
	// namely owned window if they don't have the app window style set
	HWND owner = GetWindow(hwnd, GW_OWNER);
	LONG exstyle = GetWindowLong(hwnd, GWL_EXSTYLE);
	if (owner && !(exstyle & WS_EX_APPWINDOW)) {
		return TRUE;
	}

	if (!IsWindowResponding(hwnd)) {
		return TRUE;
	}

	if (IsWindowZeroSize(hwnd))
	{
		return TRUE;
	}

	webrtc::DesktopCapturer::Source window;
	window.id = reinterpret_cast<webrtc::DesktopCapturer::SourceId>(hwnd);

	// GetWindowText* are potentially blocking operations if `hwnd` is
	// owned by the current process. The APIs will send messages to the window's
	// message loop, and if the message loop is waiting on this operation we will
	// enter a deadlock.
	// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowtexta#remarks
	//
	// To help consumers avoid this, there is a DesktopCaptureOption to ignore
	// windows owned by the current process. Consumers should either ensure that
	// the thread running their message loop never waits on this operation, or use
	// the option to exclude these windows from the source list.
	bool owned_by_current_process = IsWindowOwnedByCurrentProcess(hwnd);
	if (owned_by_current_process) {
		return TRUE;
	}

	DWORD process_id;
	GetWindowThreadProcessId(hwnd, &process_id);

	if (process_id)
	{
		HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, process_id);
		if (hProcess)
		{
			CHAR buffer[MAX_PATH];
			if (GetModuleFileNameExA(hProcess, 0, buffer, MAX_PATH))
			{
				CHAR bufferLower[MAX_PATH]{ 0 };
				size_t len = strlen(buffer);
				for (int i = 0; i < len; i++)
				{
					bufferLower[i] = std::tolower(buffer[i]);
				}
				std::string name(bufferLower);
				name = name.substr(name.find_last_of("\\"));
				if (name == "\\shellexperiencehost.exe" || name == "\\textinputhost.exe")
				{
					return TRUE;
				}
			}
			else
			{
				DWORD err = GetLastError();
				printf("%d", err);
			}
			CloseHandle(hProcess);
		}
	}

	// Even if consumers request to enumerate windows owned by the current
	// process, we should not call GetWindowText* on unresponsive windows owned by
	// the current process because we will hang. Unfortunately, we could still
	// hang if the window becomes unresponsive after this check, hence the option
	// to avoid these completely.
	if (IsWindowResponding(hwnd)) {
		const size_t kTitleLength = 500;
		WCHAR window_title[kTitleLength] = L"";
		if (GetWindowTextLength(hwnd) != 0 &&
			GetWindowTextW(hwnd, window_title, kTitleLength) > 0) {
			window.title = ToUtf8(window_title, wcslen(window_title));
		}
	}

	// Skip windows when we failed to convert the title or it is empty.
	if (window.title.empty())
		return TRUE;

	// Capture the window class name, to allow specific window classes to be
	// skipped.
	//
	// https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-wndclassa
	// says lpszClassName field in WNDCLASS is limited by 256 symbols, so we don't
	// need to have a buffer bigger than that.
	const size_t kMaxClassNameLength = 256;
	WCHAR class_name[kMaxClassNameLength] = L"";
	const int class_name_length =
		GetClassNameW(hwnd, class_name, kMaxClassNameLength);
	if (class_name_length < 1)
		return TRUE;

	// Skip Program Manager window.
	if (wcscmp(class_name, L"Progman") == 0)
		return TRUE;

	// Skip Start button window on Windows Vista, Windows 7.
	// On Windows 8, Windows 8.1, Windows 10 Start button is not a top level
	// window, so it will not be examined here.
	if (wcscmp(class_name, L"Button") == 0)
		return TRUE;

	if (wcscmp(class_name, L"Windows.UI.Core.CoreWindow") == 0)
		return TRUE;

	list->push_back(window);

	return TRUE;
}

void NTAPI webrtc_desktop_capture::wnd_close_timer_proc(PTP_CALLBACK_INSTANCE Instance, PVOID Context, PTP_TIMER Timer)
{
	if (Instance)
	{
		webrtc_desktop_capture* pThis = (webrtc_desktop_capture*)Context;
		if (pThis && !pThis->stop_capture_ && (!pThis->capture_wnd_hwnd_ || !IsWindow(pThis->capture_wnd_hwnd_)))
		{
			if (pThis->wnd_close_monitor_timer_ && Timer == pThis->wnd_close_monitor_timer_ && pThis->callback_)
			{
				pThis->callback_->OnCaptureError();
			}
			pThis->Stop();
		}
	}
}

BOOL webrtc_desktop_capture::bring_window_to_front(HWND hwnd)
{
	BOOL ret = FALSE;
	HWND hCurWnd = ::GetForegroundWindow();
	DWORD dwMyID = ::GetCurrentThreadId();
	DWORD dwCurID = ::GetWindowThreadProcessId(hCurWnd, NULL);
	::AttachThreadInput(dwCurID, dwMyID, TRUE);
	ret = ::SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
	ret = ::SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_SHOWWINDOW | SWP_NOSIZE | SWP_NOMOVE);
	ret = ::SetForegroundWindow(hwnd);
	::SetFocus(hwnd);
	::SetActiveWindow(hwnd);
	::AttachThreadInput(dwCurID, dwMyID, FALSE);
	if (IsIconic(hwnd))
	{
		ret = ShowWindow(hwnd, SW_RESTORE);
	}
	return ret;
}