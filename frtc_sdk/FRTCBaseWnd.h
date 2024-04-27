#pragma once
#include <map>
#include "auto_lock.h"

extern HINSTANCE hInst; // defined in main file

class FRTCBaseWnd
{
public:
	FRTCBaseWnd(BOOL bMainWnd);
    ~FRTCBaseWnd();

    operator HWND ();

	BOOL Create (__in DWORD dwExStyle,
                 __in_opt LPCWSTR lpWindowName,
		         __in DWORD dwStyle,
                 __in int X,
                 __in int Y,
                 __in int nWidth,
                 __in int nHeight,
		         __in_opt HWND hWndParent, 
                 __in_opt HMENU hMenu, 
                 __in_opt HINSTANCE hInstance, 
                 __in_opt BOOL showNow = TRUE);
	
	virtual LRESULT WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

	static	LRESULT CALLBACK WndProcThunk(HWND hWnd, 
                                          UINT msg, 
                                          WPARAM wParam, 
                                          LPARAM lParam);

public:
	static std::map<HWND, FRTCBaseWnd*> _wnd_map;
	static CritSec _wnd_map_lock;
	static ATOM  _wnd_class;
	static TCHAR _wnd_class_name [] ;

private:
	HWND _wnd;
	bool _main_wnd;
};

