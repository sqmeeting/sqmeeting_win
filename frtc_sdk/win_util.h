#ifndef WIN_UTIL_H_
#define WIN_UTIL_H_

#include <Windows.h>

typedef void(__stdcall* GetWinVersionFunc)(LPDWORD major, 
                                           LPDWORD minor, 
                                           LPDWORD build);

static GetWinVersionFunc GetWinVersion = 
    (GetWinVersionFunc)GetProcAddress(LoadLibrary(L"ntdll.dll"), 
                                      "RtlGetNtVersionNumbers");

static bool IsWin8OrLater();

bool IsWin8OrLater()
{
    DWORD major, minor, build;
    GetWinVersion(&major, &minor, &build);
    return ((major > 6) || (major == 6 && minor >= 2));
}

#endif