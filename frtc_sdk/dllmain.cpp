// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"

#include <windows.h>
#include <time.h>

HINSTANCE g_hInstance;

void write_minidump(struct _EXCEPTION_POINTERS* apExceptionInfo);
void write_minidump(struct _EXCEPTION_POINTERS* apExceptionInfo)
{
	/*
	https://stackoverflow.com/questions/9020353/create-a-dump-file-for-an-application-whenever-it-crashes
	*/
	HMODULE mhLib = ::LoadLibrary(_T("dbghelp.dll"));
	MINIDUMPWRITEDUMP pDump = (MINIDUMPWRITEDUMP)::GetProcAddress(mhLib, "MiniDumpWriteDump");

	WCHAR fileName[200];

	time_t currentTime;
	WCHAR timeStr[100];
	wcsftime(timeStr, 100, _T("%Y%m%d%H%M%S"), localtime(&currentTime));

	swprintf(fileName, _T("dump_%s.dmp"), timeStr);

	HANDLE hFile = ::CreateFile(fileName, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL, NULL);

	_MINIDUMP_EXCEPTION_INFORMATION ExInfo;
	ExInfo.ThreadId = ::GetCurrentThreadId();
	ExInfo.ExceptionPointers = apExceptionInfo;
	ExInfo.ClientPointers = FALSE;

	/*
	https://docs.microsoft.com/en-us/windows/win32/api/minidumpapiset/ne-minidumpapiset-minidump_type
	*/
	DWORD Flags = MiniDumpWithFullMemory |
		MiniDumpWithFullMemoryInfo |
		MiniDumpWithHandleData |
		MiniDumpWithUnloadedModules |
		MiniDumpWithThreadInfo;

	pDump(GetCurrentProcess(), GetCurrentProcessId(), hFile, (MINIDUMP_TYPE)Flags, &ExInfo, NULL, NULL);
	::CloseHandle(hFile);
}

LONG WINAPI HandleException(struct _EXCEPTION_POINTERS* apExceptionInfo);
LONG WINAPI HandleException(struct _EXCEPTION_POINTERS* apExceptionInfo)
{
	write_minidump(apExceptionInfo);
	return EXCEPTION_CONTINUE_SEARCH;
}


BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	g_hInstance = hModule;

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	SetUnhandledExceptionFilter(HandleException);
	return TRUE;
}

