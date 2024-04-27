#include "audio_device_monitor.h"
#include "audio_capture.h"

PeopleAudioMonitor::PeopleAudioMonitor() 
    : _initialized(false), 
      _audio_sink(nullptr){
}

PeopleAudioMonitor::~PeopleAudioMonitor() {
	StopMonitor();
}

HRESULT PeopleAudioMonitor::QueryInterface(REFIID riid, void** ppvObject) {
	if (IID_IUnknown == riid) {
		AddRef();
		*ppvObject = (IUnknown*)this;
	}
	else if (__uuidof(IMMNotificationClient) == riid) {
		AddRef();
		*ppvObject = (IMMNotificationClient*)this;
	}
	else {
		*ppvObject = NULL;
		return E_NOINTERFACE;
	}

	return S_OK;
}

ULONG PeopleAudioMonitor::AddRef(void) {
	return InterlockedIncrement(&_ref);
}

ULONG PeopleAudioMonitor::Release(void) {
	DWORD ref = InterlockedDecrement(&_ref);
	if (ref == 0) {
		delete this;
	}
	return ref;
}

HRESULT PeopleAudioMonitor::OnDeviceStateChanged(LPCWSTR pwstrDeviceId, 
                                                 DWORD dwNewState) {
	if (_audio_sink) {
		_audio_sink->OnAudioDeviceStateChanged();
    }

	return S_OK;
}

HRESULT PeopleAudioMonitor::OnDefaultDeviceChanged(EDataFlow flow, 
                                                   ERole role, 
                                                   LPCWSTR pwstrDefautDeviceId) {
	if (_audio_sink) {
		_audio_sink->OnDefaultAudioChanged();
    }

	return S_OK;
}

void PeopleAudioMonitor::SetAudioSink(IAudioDataSink* audio_sink) {
	_audio_sink = audio_sink;
}

HRESULT PeopleAudioMonitor::PeopleAudioMonitor::StartMonitor() {
	HRESULT ret = S_FALSE;
	if (!_initialized) {
		ret = _device_enumerator.CoCreateInstance(__uuidof(MMDeviceEnumerator));
		_initialized = SUCCEEDED(ret);
		if (!_initialized) {
			return ret;
		}
	}

	ret = _device_enumerator->RegisterEndpointNotificationCallback(this);
	
	return ret;
}

void PeopleAudioMonitor::PeopleAudioMonitor::StopMonitor() {
	if (_initialized) {
		_device_enumerator->UnregisterEndpointNotificationCallback(this);
    }
}

ContentAudioMonitor::ContentAudioMonitor()
    : _ref(1),
    _audio_capture(nullptr),
    _observer(false) {
}

ContentAudioMonitor::ContentAudioMonitor(ContentAudioCapture* audio_capture)
    : _ref(1),
    _audio_capture(audio_capture),
    _observer(true) {
}

ContentAudioMonitor::~ContentAudioMonitor() {
}

HRESULT ContentAudioMonitor::QueryInterface(REFIID riid, void** ppvObject) {
    if (IID_IUnknown == riid) {
        AddRef();
        *ppvObject = (IUnknown*)this;
    }
    else if (__uuidof(IMMNotificationClient) == riid) {
        AddRef();
        *ppvObject = (IMMNotificationClient*)this;
    }
    else {
        *ppvObject = NULL;
        return E_NOINTERFACE;
    }

    return S_OK;
}

ULONG ContentAudioMonitor::AddRef(void) {
    return InterlockedIncrement(&_ref);
};

ULONG ContentAudioMonitor::Release(void) {
    DWORD ref = InterlockedDecrement(&_ref);
    if (ref == 0) {
        delete this;
    }
    return ref;
};

HRESULT ContentAudioMonitor::OnDefaultDeviceChanged(
    _In_  EDataFlow flow,
    _In_  ERole role,
    _In_  LPCWSTR pwstrDefaultDeviceId) {
    if (flow == eRender && role == eConsole && _observer) {
        _audio_capture->OnDefaultDeviceChanged();
    }

    return S_OK;
}
