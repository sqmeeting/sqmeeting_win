#ifndef AUDIO_DEVICE_MONITOR_H_
#define AUDIO_DEVICE_MONITOR_H_

#include <MMDeviceApi.h>
#include <atlbase.h>

#include "../frtc_typedef.h"

class ContentAudioCapture;

class PeopleAudioMonitor : public IMMNotificationClient {
public:
	PeopleAudioMonitor();
	~PeopleAudioMonitor();

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
    virtual ULONG STDMETHODCALLTYPE AddRef(void);
    virtual ULONG STDMETHODCALLTYPE Release(void);

    virtual HRESULT STDMETHODCALLTYPE OnDeviceStateChanged(
        _In_  LPCWSTR pwstrDeviceId,
        _In_  DWORD dwNewState);

    virtual HRESULT STDMETHODCALLTYPE OnDeviceAdded(_In_ LPCWSTR pwstrDeviceId) {
        return S_OK;
    }

    virtual HRESULT STDMETHODCALLTYPE OnDeviceRemoved(_In_ LPCWSTR pwstrDeviceId) {
        return S_OK;
    }

    virtual HRESULT STDMETHODCALLTYPE OnDefaultDeviceChanged(
        _In_  EDataFlow flow,
        _In_  ERole role,
        _In_  LPCWSTR pwstrDefaultDeviceId);

    virtual HRESULT STDMETHODCALLTYPE OnPropertyValueChanged(
        _In_  LPCWSTR pwstrDeviceId,
        _In_  const PROPERTYKEY key) {
        return S_OK;
    }

	void SetAudioSink(IAudioDataSink* audio_sink);
	HRESULT StartMonitor();
	void StopMonitor();

private:
    LONG _ref;
	bool _initialized;
	IAudioDataSink* _audio_sink;
    CComPtr<IMMDeviceEnumerator> _device_enumerator;
};

class ContentAudioMonitor : public IMMNotificationClient {
public:
    ContentAudioMonitor();
    ContentAudioMonitor(ContentAudioCapture* audio_capture);
    ~ContentAudioMonitor();

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
    virtual ULONG STDMETHODCALLTYPE AddRef(void);
    virtual ULONG STDMETHODCALLTYPE Release(void);

    virtual HRESULT STDMETHODCALLTYPE OnDeviceStateChanged(
        _In_ LPCWSTR pwstrDeviceId,
        _In_ DWORD dwNewState) {
        return S_OK;
    }

    virtual HRESULT STDMETHODCALLTYPE OnDeviceAdded(_In_ LPCWSTR pwstrDeviceId) { 
        return S_OK; 
    }

    virtual HRESULT STDMETHODCALLTYPE OnDeviceRemoved(_In_ LPCWSTR pwstrDeviceId) { 
        return S_OK; 
    }

    virtual HRESULT STDMETHODCALLTYPE OnDefaultDeviceChanged(
        _In_ EDataFlow flow,
        _In_ ERole role,
        _In_ LPCWSTR pwstrDefaultDeviceId);

    virtual HRESULT STDMETHODCALLTYPE OnPropertyValueChanged(
        _In_ LPCWSTR pwstrDeviceId,
        _In_ const PROPERTYKEY key) {
        return S_OK;
    }

private:
    LONG _ref;
    ContentAudioCapture* _audio_capture;
    bool _observer;
};

#endif
