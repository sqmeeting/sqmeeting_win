#pragma once

#include <list>
#include <string>
#include <utility>
#include "guiddef.h"


typedef struct _video_device
{
	TCHAR	*id;
	TCHAR	*name;
	INT		inputIndex;
} video_device;

typedef std::list<video_device *> video_device_list;


typedef std::pair<std::wstring, GUID> DevInfo;

class IAudioDataSink
{
public:
	virtual DWORD OnWriteData(LPVOID buff, DWORD len, DWORD sampleRate) = 0;
	virtual DWORD OnWriteDataContent(LPVOID buff, DWORD len, DWORD sampleRate) = 0;
	virtual DWORD OnDefaultAudioChanged() = 0;
	virtual DWORD OnAudioDeviceStateChanged() = 0;
	virtual DWORD OnPeakValue(float peak) = 0;
};
class IAudioDataSource
{
public:
	virtual DWORD OnReadData(LPVOID buff, DWORD len, DWORD sampleRate) = 0;
	virtual void OnError(DWORD err) = 0;
};

