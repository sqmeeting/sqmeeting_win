// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#endif
// Windows Header Files:
#include <windows.h>

// C RunTime Header Files
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>
#include <CommCtrl.h>
#include <objbase.h>
#include <gdiplus.h>
#include <string>
#include <Vfw.h>

typedef struct {
	bool video_state;
	bool audio_state;
	HWND ui_host_hwnd;
}CREATE_OPTIONS, *PCREATE_OPTIONS;

typedef enum {
	kContentShareCpuLimit = 0,
	kContentShareNoPermission,
	kUplinkBitRateLimit,
	kAudioMuteDetect,
	kCameraOpenFail
}FRTC_TIP_TYPE;


#define ID_FRTC_TIMER									8
#define IDT_QUERY_STATISTICS							(ID_FRTC_TIMER + 1)
#define IDT_RESTART_CAMERA								(ID_FRTC_TIMER + 2)
#define IDT_UPDATE_DEVICE_SETTING						(ID_FRTC_TIMER + 3)
#define IDT_DB_CLICK									(ID_FRTC_TIMER + 4)
#define IDT_UPDATE_AUDIO_DEVICE							(ID_FRTC_TIMER + 5)

#define IDT_UPDATE_DEFAULT_AUDIO_DEVICE					(ID_FRTC_TIMER + 7)

#define IDT_MOUSEENTER									(ID_FRTC_TIMER + 8)
#define IDT_MOUSELEAVE									(ID_FRTC_TIMER + 9)


#define FRTC_WEAK_NETWORK_THRESHOLD						0
#define VIDEO_LOSS_THRESHOLD_1							(FRTC_WEAK_NETWORK_THRESHOLD + 3)
#define VIDEO_LOSS_THRESHOLD_2							(FRTC_WEAK_NETWORK_THRESHOLD + 8)
#define AUDIO_LOSS_THRESHOLD_1							(FRTC_WEAK_NETWORK_THRESHOLD + 15)
#define AUDIO_LOSS_THRESHOLD_2							(FRTC_WEAK_NETWORK_THRESHOLD + 30)

#define FRTC_SIGNAL_INTENSITY_LEVEL						0
#define SIGNAL_INTENSITY_LOW							(FRTC_SIGNAL_INTENSITY_LEVEL + 1)
#define SIGNAL_INTENSITY_MEDIAN							(FRTC_SIGNAL_INTENSITY_LEVEL + 3)
#define SIGNAL_INTENSITY_HIGH							(FRTC_SIGNAL_INTENSITY_LEVEL + 5)


#include <Dbghelp.h>
void create_minidump(struct _EXCEPTION_POINTERS* apExceptionInfo);
typedef BOOL(WINAPI* MINIDUMPWRITEDUMP)(HANDLE hProcess, DWORD dwPid, HANDLE hFile, MINIDUMP_TYPE DumpType, CONST PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam, CONST PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam, CONST PMINIDUMP_CALLBACK_INFORMATION CallbackParam);
