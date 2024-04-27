#include "stdafx.h"
#include "FRTCBaseWnd.h"

std::map<HWND, FRTCBaseWnd*> FRTCBaseWnd::_wnd_map;
CritSec FRTCBaseWnd::_wnd_map_lock;
ATOM FRTCBaseWnd::_wnd_class;
TCHAR FRTCBaseWnd::_wnd_class_name[] = L"frtc_base_wnd_class";

FRTCBaseWnd::FRTCBaseWnd(BOOL bMainWnd) 
    : _wnd(NULL), 
      _main_wnd(false)
{
}

FRTCBaseWnd::~FRTCBaseWnd() 
{
    if (_wnd && IsWindow(_wnd)) 
    {
        ::DestroyWindow(_wnd);
    }
}

FRTCBaseWnd::operator HWND () 
{
    return _wnd;
}

BOOL FRTCBaseWnd::Create(__in DWORD dwExStyle,
                         __in_opt LPCWSTR lpWindowName,
                         __in DWORD dwStyle,	
                         __in int X,	
                         __in int Y,	
                         __in int nWidth,
                         __in int nHeight,
                         __in_opt HWND hWndParent, 
                         __in_opt HMENU hMenu, 
                         __in_opt HINSTANCE hInstance, 
                         BOOL showNow)
{
    if ( _wnd_class == 0) 
    {
        WNDCLASSEX wnd_class;
        wnd_class.cbSize = sizeof(WNDCLASSEX);
        wnd_class.style			= CS_DBLCLKS;
        wnd_class.lpfnWndProc	= WndProcThunk;
        wnd_class.cbClsExtra		= 0;
        wnd_class.cbWndExtra		= 0;
        wnd_class.hInstance		= hInstance;
        wnd_class.hIcon			= LoadIcon(hInstance, MAKEINTRESOURCE(1));
        wnd_class.hCursor		= LoadCursor(NULL, IDC_ARROW);
        wnd_class.hbrBackground = (HBRUSH)CreateSolidBrush(RGB(0,0,0));
        wnd_class.lpszMenuName	= NULL;
        wnd_class.lpszClassName	= _wnd_class_name;
        wnd_class.hIconSm		= LoadIcon(wnd_class.hInstance, MAKEINTRESOURCE(1));

        _wnd_class = RegisterClassEx(&wnd_class);
    }

	_wnd = ::CreateWindowEx(dwExStyle, 
                            _wnd_class_name, 
                            lpWindowName, 
                            dwStyle,
		                    X, 
                            Y, 
                            nWidth, 
                            nHeight, 
                            hWndParent, 
                            hMenu, 
                            hInstance, 
                            (LPVOID)this);
    if (!_wnd)
    {
        return FALSE;
    }

    if (showNow)
    {
        ShowWindow(_wnd, SW_SHOW);
        UpdateWindow(_wnd);
    }

    return TRUE;
}

LRESULT FRTCBaseWnd::WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (msg == WM_CLOSE && _main_wnd )
    {
        ::PostQuitMessage (0);
        return 0;
    }
    else
    {
        return ::DefWindowProc (hWnd, msg, wParam, lParam);
    }
}

LRESULT CALLBACK FRTCBaseWnd::WndProcThunk(HWND hWnd, 
                                           UINT msg, 
                                           WPARAM wParam, 
                                           LPARAM lParam)
{
    if ( msg == WM_CREATE ) 
    {
        LPCREATESTRUCT lpcs = (LPCREATESTRUCT)lParam;
        FRTCBaseWnd * pThis = (FRTCBaseWnd *) lpcs->lpCreateParams;
        AutoLock autolock (_wnd_map_lock);
        _wnd_map[hWnd] = pThis;
        return pThis->WndProc( hWnd,  msg,  wParam,  lParam );
    }
    else if ( msg == WM_DESTROY ) 
    {
        AutoLock autolock (_wnd_map_lock);
        FRTCBaseWnd * pThis = _wnd_map[hWnd];
        _wnd_map.erase(hWnd);
        if (pThis)
        {
            pThis->_wnd = NULL;
            return pThis->WndProc(hWnd, msg, wParam, lParam);
        }
    }
    else
    {
        std::map<HWND, FRTCBaseWnd*>::iterator it = _wnd_map.find(hWnd);
        if (it != _wnd_map.end()) 
        {
            return (it->second)->WndProc( hWnd,  msg,  wParam,  lParam );
        } 
        else
        {
            return ::DefWindowProc( hWnd,  msg,  wParam,  lParam);
        }
    }
    
    return 0;
}
