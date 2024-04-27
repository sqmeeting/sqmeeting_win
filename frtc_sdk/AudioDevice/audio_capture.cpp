#include <AudioEngineEndPoint.h>
#include <functiondiscoverykeys.h>
#include <Shlobj.h>
#include <atlstr.h>
#include "audio_capture.h"
#include "audio_device_monitor.h"
#include "../stdafx.h"
#include "../log.h"
#include "../frtc_sdk_util.h"

HMODULE PeopleAudioCapture::_avrt_dll = NULL;
PeopleAudioCapture::SetMMThreadCharacteristicsFunc PeopleAudioCapture::_set_mmthread_characteristics = NULL;
PeopleAudioCapture::RevertMMThreadCharacteristicsFunc PeopleAudioCapture::_revert_mmthread_characteristics = NULL;
PeopleAudioCapture::SetMMThreadPriFunc PeopleAudioCapture::_set_mmthread_pri = NULL;

PeopleAudioCapture::PeopleAudioCapture()
    : _exclusive_mode(true),
      _audio_sink(nullptr),
      _audio_buf(nullptr),
      _data_buf(nullptr),
      _capture_thread(NULL),
      _quit_thread(false),
      _quit_event(NULL),
      _ready_event(NULL),
      _wave_format_ex((PWAVEFORMATEX) new char[sizeof(WAVEFORMATEXTENSIBLE)]) {
}

PeopleAudioCapture::~PeopleAudioCapture() {
    if (_capture_thread) {
        StopCapture();
    }

    if (_avrt_dll) {
        FreeLibrary(_avrt_dll);
        _avrt_dll = NULL;
    }

    if (_ready_event) {
        CloseHandle(_ready_event);
        _ready_event = NULL;
    }

    if (_quit_event) {
        CloseHandle(_quit_event);
        _quit_event = NULL;
    }

    _capture_device = NULL;
    _audio_client = NULL;
    _agc_control = NULL;
    _audio_capture = NULL;
    _audio_meter = NULL;

    if (_wave_format_ex) {
        delete[] _wave_format_ex;
    }

    CoUninitialize();
}

void PeopleAudioCapture::StartCapture() {
    DebugLog("start people audio capture");

    AutoLock lock(_capture_lock);

    if (_capture_thread != NULL) {
        return;
    }

    _quit_thread = false;
    _capture_thread = ::CreateThread(NULL,
                                     0,
                                     PeopleAudioCapture::CaptureThreadFunc,
                                     this,
                                     0,
                                     NULL);

    if (_capture_thread) {
        BOOL ret = SetThreadPriority(_capture_thread, 
                                     THREAD_PRIORITY_TIME_CRITICAL);
        if (!ret) {
            WarnLog("SetThreadPriority failed");
        }
    }
    else
    {
        ErrorLog("people audio capture create thread failed. error=%d", GetLastError());
    }
}

void PeopleAudioCapture::StopCapture() {
    AutoLock lock(_capture_lock);
    _quit_thread = true;

    if (_quit_event) {
        BOOL ret = SetEvent(_quit_event);
        if (!ret) {
            ErrorLog("people audio capture set quit event failed. error=%d", 
                      GetLastError());
        }
    }

    if (_capture_thread) {
        DWORD ret = WaitForSingleObject(_capture_thread, INFINITE);
        if (ret == WAIT_OBJECT_0) {
            CloseHandle(_capture_thread);
        }
        else {
            WarnLog("StopCapture timeout");
            TerminateThread(_capture_thread, 0);
        }

        _capture_thread = NULL;
    }

    ResetEvent(_quit_event);
}

void PeopleAudioCapture::SetDeviceId(const GUID& id) {
    _device_id = id;
}

void PeopleAudioCapture::GetDeviceId(LPGUID id) {
    *id = _device_id;
}

void PeopleAudioCapture::ResetDevice(const GUID& id) {
    StopCapture();
    SetDeviceId(id);

    std::string uuid_str;
    FRTCSDK::FRTCSdkUtil::guid_to_string(id, uuid_str);
    std::wstring uuid_wstr = FRTCSDK::FRTCSdkUtil::string_to_wstring(uuid_str);
    Init((TCHAR*)uuid_wstr.c_str());
    StartCapture();
}

void PeopleAudioCapture::GetDeviceGUID(TCHAR guid_str[kCaptureDeviceGuidLen]) {
    wcsncpy_s(guid_str, kCaptureDeviceGuidLen - 1, _device_guid, _TRUNCATE);
}

std::vector<DevInfo> PeopleAudioCapture::GetDeviceList() {
    AutoLock lock(_device_lock);
    return _device_list;
}

HRESULT PeopleAudioCapture::UpdateDeviceList(bool set_default) {
    DebugLog("update people audio device list, set default=%d", set_default);

    AutoLock lock(_device_lock);
    std::vector <DevInfo> back_list = _device_list;
    _device_list.clear();
    HRESULT ret = ::DirectSoundCaptureEnumerate(
                        PeopleAudioCapture::EnumerateDevice, this);
    if (FAILED(ret)) {
        _device_list = back_list;
    }

    if (set_default && !_device_list.empty()) {
        _device_id = _device_list[0].second;

        std::wstring guid_str;
        FRTCSDK::FRTCSdkUtil::guid_to_wstring(_device_id, guid_str);
        DebugLog("default people audio device guid=%ws", guid_str.c_str());
    }

    return ret;
}

int PeopleAudioCapture::GetDeviceCount() {
    return _device_count;
}

bool PeopleAudioCapture::CurrentDeviceExist() {
    UpdateDeviceList(false);

    AutoLock lock(_device_lock);
    for (const auto& dev_info : _device_list) {
        if (memcmp(&dev_info.second, &_device_id, sizeof(GUID)) == 0) {
            InfoLog("people audio device exists");
            return true;
        }
    }

    return false;
}

void PeopleAudioCapture::SetAudioSink(IAudioDataSink* audio_sink) {
    _audio_sink = audio_sink;
}

HRESULT PeopleAudioCapture::GetOSDefaultDevice(std::wstring& guid_str) {
    CComPtr<IMMDeviceEnumerator> device_enumerator;
    CComPtr<IPropertyStore> property_store;
    CComPtr<IMMDevice> capture_device;

    HRESULT ret = device_enumerator.CoCreateInstance(__uuidof(MMDeviceEnumerator));
    if (FAILED(ret)) {
        ErrorLog("create MMDeviceEnumerator failed. ret=0x%x", ret);
        return ret;
    }

    ret = device_enumerator->GetDefaultAudioEndpoint(eCapture, 
                                                     eConsole, 
                                                     &capture_device);
    if (FAILED(ret)) {
        ErrorLog("GetDefaultAudioEndpoint failed. ret=0x%x", ret);
        return ret;
    }

    ret = capture_device->OpenPropertyStore(STGM_READ, &property_store);
    if (FAILED(ret)) {
        ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
        capture_device = NULL;
        return ret;
    }

    TCHAR* device_guid = NULL;
    TCHAR device_guid_str[kCaptureDeviceGuidLen];
    PROPVARIANT prop_variant;

    PropVariantInit(&prop_variant);
    prop_variant.vt = VT_LPWSTR;
    ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID,
                                   &prop_variant);
    if (FAILED(ret)) {
        ErrorLog("GetValue PKEY_AudioEndpoint_GUID failed, ret=0x%x", ret);
        capture_device = NULL;
        return ret;
    }
    wcsncpy_s(device_guid_str, 
              kCaptureDeviceGuidLen - 1, 
              prop_variant.bstrVal, 
              _TRUNCATE);
    PropVariantClear(&prop_variant);

    device_guid = &device_guid_str[0];

    InfoLog("default device found, guid=%ws", device_guid);

    property_store = NULL;
    capture_device = NULL;
    guid_str = device_guid;

    return ret;
}

void PeopleAudioCapture::SetDeviceShareMode(bool share_mode) {
    _exclusive_mode = !share_mode;
}

bool PeopleAudioCapture::IsDeviceShareMode() {
    return !_exclusive_mode;
}

bool PeopleAudioCapture::IsCapturing() {
    return _capture_thread != NULL && !_quit_thread;
}

HRESULT PeopleAudioCapture::SyncWithOS() {
    std::wstring guid_str;
    HRESULT ret = GetOSDefaultDevice(guid_str);
    if (FAILED(ret)) {
        ErrorLog("get os default mic failed, ret=0x%08x", ret);
        return ret;
    }
    
    GUID default_guid;
    FRTCSDK::FRTCSdkUtil::get_guid_from_wstring(
        static_cast<const TCHAR*>(guid_str.c_str()), &default_guid);
    if (memcmp(&default_guid, &_device_id, sizeof(GUID)) != 0) {
        ResetDevice(default_guid);
    }

    return S_OK;
}

float PeopleAudioCapture::GetDevicePeakValue() {
    float peak_val = 0.0f;
    if (!_audio_meter) {
        if (_capture_device) {
            HRESULT ret = _capture_device->Activate(
                              __uuidof(IAudioMeterInformation),
                              CLSCTX_ALL,
                              NULL,
                              (void**)&_audio_meter);
        }
    }

    if (_audio_meter) {
        _audio_meter->GetPeakValue(&peak_val);
    }

    return peak_val;
}

HRESULT PeopleAudioCapture::Init(const TCHAR* device_guid) {

    DebugLog("people audio device guid=%ws", device_guid);
    CoInitialize(NULL);

    _mmthread_pri_increased = false;
    if (PeopleAudioCapture::_avrt_dll == NULL) {
        PeopleAudioCapture::LoadMmThreadSupport();
    }

    _device_idx = 0;
    _device_count = 0;
    _capture_device = NULL;
    _channel_num = 0;
    _sample_rate = 0;

    _frame_size_in_sample = 0;
    _buf_size_in_sample = 0;
    _idx_in_frame = 0;
    _idx_in_sample = 0;
    _wave_format_type = WaveFormatType::kFormatUnknown;
    _output_mono = true;

    PROPVARIANT prop_variant;
    TCHAR device_guid_str[kCaptureDeviceGuidLen];
    CComPtr<IPropertyStore> property_store;
    CComPtr<IMMDeviceCollection> device_collection;
    CComPtr<IMMDeviceEnumerator> device_enumerator;

    HRESULT ret = device_enumerator.CoCreateInstance(__uuidof(MMDeviceEnumerator));
    if (FAILED(ret)) {
        ErrorLog("create MMDeviceEnumerator failed. ret=0x%x", ret);
        goto __Exit;
    }

    if (device_guid == NULL) {
        ret = device_enumerator->GetDefaultAudioEndpoint(eCapture,
                                                   eCommunications,
                                                   &_capture_device);
        if (FAILED(ret)) {
            ErrorLog("GetDefaultAudioEndpoint failed. ret=0x%x", ret);
            goto __Exit;
        }

        ret = _capture_device->OpenPropertyStore(STGM_READ, &property_store);
        if (FAILED(ret)) {
            ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
            goto __Exit;
        }

        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_LPWSTR;
        ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID,
                                    &prop_variant);
        if (FAILED(ret)) {
            InfoLog("GetValue PKEY_AudioEndpoint_GUID failed, ret=0x%x", ret);
            goto __Exit;
        }

        wcsncpy_s(device_guid_str, 
                  kCaptureDeviceGuidLen - 1, 
                  prop_variant.bstrVal, 
                  _TRUNCATE);

        PropVariantClear(&prop_variant);

        device_guid = &device_guid_str[0];
        InfoLog("default people audio device found: %ws", device_guid);

        property_store = NULL;
        _capture_device = NULL;
    }

    wcsncpy_s(_device_guid, kCaptureDeviceGuidLen - 1, device_guid, _TRUNCATE);
    InfoLog("people audio device found, guid=%ws, len=%d",
             _device_guid, wcslen(_device_guid));

    ret = device_enumerator->EnumAudioEndpoints(eCapture,
                                          DEVICE_STATE_ACTIVE,
                                          &device_collection);
    if (FAILED(ret)) {
        ErrorLog("EnumAudioEndpoints failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = device_collection->GetCount(&_device_count);
    if (FAILED(ret)) {
        ErrorLog("GetCount failed. ret=0x%x", ret);
        goto __Exit;
    }

    if (_device_count == 0) {
        ErrorLog("no people audio device found");
        ret = E_FAIL;
        goto __Exit;
    }

    DebugLog("people audio device found. count=%d", _device_count);

    bool device_found = false;
    UINT idx = 0;
    for (idx = 0; idx < _device_count; idx++) {
        _capture_device = NULL;
        ret = device_collection->Item(idx, &_capture_device);
        if (FAILED(ret)) {
            ErrorLog("Item failed. ret=0x%x", ret);
            goto __Exit;
        }

        ret = _capture_device->OpenPropertyStore(STGM_READ, &property_store);
        if (FAILED(ret)) {
            ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
            goto __Exit;
        }

        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_LPWSTR;
        ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID,
                                       &prop_variant);
        if (FAILED(ret)) {
            ErrorLog("people audio device GetValue failed, ret=0x%x", ret);
        }
        else if (_wcsicmp(_device_guid, prop_variant.pwszVal) == 0) {
            _device_idx = idx;
            device_found = true;

            InfoLog("people audio device found. index=%d GUID=%ws, len=%d",
                 idx, prop_variant.pwszVal, wcslen(prop_variant.pwszVal));

            break;
        }

        PropVariantClear(&prop_variant);
        property_store = NULL;
    }

    if (!device_found) {
        ErrorLog("no people audio device found, exit");
        ret = E_FAIL;
        goto __Exit;
    }

    FRTCSDK::FRTCSdkUtil::get_guid_from_wstring(device_guid, &_device_id);

    PropVariantInit(&prop_variant);
    prop_variant.vt = VT_LPWSTR;
    ret = property_store->GetValue(PKEY_Device_FriendlyName, &prop_variant);
    if (FAILED(ret)) {
        ErrorLog("GetValue PKEY_Device_FriendlyName failed. ret=0x%x", ret);
        goto __Exit;
    }

    _device_name = CW2A(prop_variant.pwszVal);

    InfoLog("Init, people audio device found, index=%d, name=%s", 
             idx, _device_name.c_str());

    PropVariantClear(&prop_variant);

    _agc_control = GetAGC(_capture_device);
    if (_agc_control) {
        if (S_OK == _agc_control->SetEnabled(FALSE, NULL)) {
            InfoLog("disable AGC success");
        }
        else {
            ErrorLog("disable AGC failed");
        }
    }

    _ready_event = CreateEvent(NULL, FALSE, FALSE, NULL);
    if (_ready_event == NULL) {
        ret = E_FAIL;
        goto __Exit;
    }

    _quit_event = CreateEvent(NULL, FALSE, FALSE, NULL);
    if (_quit_event == NULL) {
        ret = E_FAIL;
        goto __Exit;
    }

    return S_OK;

__Exit:
    _capture_device = NULL;
    _audio_client = NULL;
    _agc_control = NULL;
    _audio_capture = NULL;
    _audio_meter = NULL;

    if (_ready_event) {
        CloseHandle(_ready_event);
        _ready_event = NULL;
    }

    if (_quit_event) {
        CloseHandle(_quit_event);
        _quit_event = NULL;
    }

    CoUninitialize();

    return ret;
}

HRESULT PeopleAudioCapture::ReInit(const TCHAR* device_guid) {
    if (_capture_thread) {
        StopCapture();
    }

    _audio_meter = NULL;
    _audio_capture = NULL;
    _agc_control = NULL;
    _audio_client = NULL;
    _capture_device = NULL;

    if (_ready_event) {
        CloseHandle(_ready_event);
        _ready_event = NULL;
    }

    if (_quit_event) {
        CloseHandle(_quit_event);
        _quit_event = NULL;
    }

    return Init(device_guid);
}

BOOL PeopleAudioCapture::LoadMmThreadSupport() {
    TCHAR sys_folder[MAX_PATH];

    if (SHGetFolderPath(NULL, CSIDL_SYSTEM, NULL, 0, sys_folder) == S_OK) {
        int len = _tcslen(sys_folder);
        wsprintf(&sys_folder[len], _T("%s"), _T("\\Avrt.dll"));
        _avrt_dll = LoadLibrary(sys_folder);
        if (_avrt_dll) {
            _set_mmthread_characteristics = (SetMMThreadCharacteristicsFunc)(
                GetProcAddress(_avrt_dll, "AvSetMmThreadCharacteristicsW"));
            _revert_mmthread_characteristics = (RevertMMThreadCharacteristicsFunc)(
                GetProcAddress(_avrt_dll, "AvRevertMmThreadCharacteristics"));
            _set_mmthread_pri = (SetMMThreadPriFunc)(
                GetProcAddress(_avrt_dll, "AvSetMmThreadPriority"));
            return TRUE;
        }
    }

    return FALSE;
}

BOOL PeopleAudioCapture::IncreaseMmThreadPri() {
    BOOL ret = FALSE;
    if (!_mmthread_pri_increased && _avrt_dll != NULL) {
        HANDLE hAvrtHandle = NULL;
        if (_set_mmthread_characteristics) {
            DWORD taskIndex = 0;
            hAvrtHandle = (*_set_mmthread_characteristics)(_T("Pro Audio"), 
                                                           &taskIndex);
            if (hAvrtHandle != NULL) {
                if (_set_mmthread_pri) {
                    ret = (*_set_mmthread_pri)(hAvrtHandle, 
                                               AVRT_PRIORITY_CRITICAL);
                    if (!ret) {
                        ErrorLog("set_mmthread_pri AVRT_PRIORITY_CRITICAL failed");
                    }
                }
            }
        }
        _mmthread_pri_increased = true;
    }

    return ret;
}

BOOL CALLBACK  PeopleAudioCapture::EnumerateDevice(LPGUID device_guid,
                                           LPCTSTR device_desc,
                                           LPCTSTR drv_name,
                                           LPVOID context) {
    PeopleAudioCapture* capture = (PeopleAudioCapture*)context;
    if (device_guid != NULL) {
        capture->_device_list.push_back(std::pair<std::wstring, GUID>(
            std::wstring(device_desc), *device_guid));
    }

    return TRUE;
}

DWORD WINAPI PeopleAudioCapture::CaptureThreadFunc(LPVOID context) {
    HRESULT ret = ((PeopleAudioCapture*)context)->CaptureFunc();
    if (FAILED(ret)) {
        WarnLog("CaptureFunc exit abnormally, ret=0x%x");
    }

    return ret;
}

DWORD PeopleAudioCapture::CaptureFunc() {
    CoInitialize(NULL);

    HRESULT ret;
    if (!_output_mono) {
        ErrorLog("only support mono, exit");
        ret = E_FAIL;
        goto __Exit;
    }

    if (!_capture_device) {
        ErrorLog("capture_device is null, exit");
        ret = E_FAIL;
        goto __Exit;
    }

    ret = _capture_device->Activate(__uuidof(IAudioClient),
                                    CLSCTX_INPROC_SERVER,
                                    NULL,
                                    reinterpret_cast<void**>(&_audio_client));
    if (FAILED(ret)) {
        ErrorLog("Activate failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = CheckFormat();
    if (FAILED(ret)) {
        ErrorLog("CheckFormat failed. ret=0x%x", ret);
        goto __Exit;
    }

    _frame_size = (_wave_format_ex->wBitsPerSample / 8) * _wave_format_ex->nChannels;
    _channel_num = _wave_format_ex->nChannels;
    _sample_rate = _wave_format_ex->nSamplesPerSec;

    if (!_mmthread_pri_increased) {
        IncreaseMmThreadPri();
    }
    
    const int ref_time_per_ms = 10000;
    REFERENCE_TIME request_time = 20 * ref_time_per_ms;
    REFERENCE_TIME default_period = 0;
    REFERENCE_TIME min_period = 0;
    ret = _audio_client->GetDevicePeriod(&default_period,
                                          &min_period);
    if (FAILED(ret)) {
        WarnLog("get audio device period failed, ret=0x%x", ret);
    }
    else {
        request_time = default_period;
        InfoLog("get audio device period. default_period=%d ms, min_period=%d ms",
                 (int)default_period, (int)min_period);
    }

    bool init_ret = true;
    ret = _audio_client->Initialize(
        _exclusive_mode ? AUDCLNT_SHAREMODE_EXCLUSIVE : AUDCLNT_SHAREMODE_SHARED,
        _exclusive_mode ? AUDCLNT_STREAMFLAGS_EVENTCALLBACK : AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_NOPERSIST,
        request_time,
        _exclusive_mode ? request_time : 0,
        _wave_format_ex,
        NULL);

    if (FAILED(ret)) {
        init_ret = false;
        WarnLog("Initialize people audio device failed, ret=0x%x", ret);

        if (ret == AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED) {
            UINT32 buffer_frame_num = 0;
            WarnLog("Initialize people audio device failed"
                   "(AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED), ret=0x%x", ret);

            ret = _audio_client->GetBufferSize(&buffer_frame_num);
            if (SUCCEEDED(ret)) {
                request_time = (REFERENCE_TIME)((
                    10000.0 * 1000 / _wave_format_ex->nSamplesPerSec * buffer_frame_num) + 0.5);

                _audio_client = NULL;
                ret = _capture_device->Activate(__uuidof(IAudioClient),
                                          CLSCTX_ALL,
                                          NULL,
                                          (void**)&_audio_client);
                if (SUCCEEDED(ret)) {
                    ret = _audio_client->Initialize(
                        _exclusive_mode ? AUDCLNT_SHAREMODE_EXCLUSIVE : AUDCLNT_SHAREMODE_SHARED,
                        _exclusive_mode ? AUDCLNT_STREAMFLAGS_EVENTCALLBACK : AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_NOPERSIST,
                        request_time,
                        _exclusive_mode ? request_time : 0,
                        _wave_format_ex,
                        NULL);
                    if (SUCCEEDED(ret)) {
                        init_ret = true;
                    }
                }
            }
        }
    }

    if (!init_ret) {
        WarnLog("Initialize people audio device failed. ret=0x%x", ret);
        goto __Exit;
    }

    UINT32 buffer_frame_count;
    ret = _audio_client->GetBufferSize(&buffer_frame_count);
    if (FAILED(ret)) {
        ErrorLog("GetBufferSize failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = _audio_client->SetEventHandle(_ready_event);
    if (FAILED(ret)) {
        ErrorLog("SetEventHandle ready_event failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = _audio_client->GetService(__uuidof(IAudioCaptureClient),
                                     reinterpret_cast<void**>(&_audio_capture));
    if (FAILED(ret)) {
        ErrorLog("GetService failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = _capture_device->Activate(__uuidof(IAudioMeterInformation),
                              CLSCTX_ALL,
                              NULL,
                              (void**)&_audio_meter);
    if (FAILED(ret)) {
        ErrorLog("Activate people audio device failed. ret=0x%x", ret);
        goto __Exit;
    }

    int frame_size_in_sample = ((_sample_rate / 10) * _channel_num);
    const int buf_size_in_frames = 5;

    _data_buf = std::make_unique<short[]>(frame_size_in_sample);
    memset(_data_buf.get(), 0x00, sizeof(short)*frame_size_in_sample);

    _frame_size_in_sample = ((_sample_rate / 100) * 2);
    _buf_size_in_sample = buf_size_in_frames * _frame_size_in_sample;
    _audio_buf = std::make_unique<short[]>(_buf_size_in_sample);
    memset(_audio_buf.get(), 0x00, sizeof(short)*_buf_size_in_sample);

    DebugLog("people audio CaptureFunc allocate buf success, audio buf size=%d, "
             "data buf size=%d", 
              _buf_size_in_sample, frame_size_in_sample);
    
    const double ref_time_per_sec = 10000000.0; 
    long actual_time = (long)(ref_time_per_sec * buffer_frame_count / 
                              _wave_format_ex->nSamplesPerSec);
    int time_out_ms = (int)(3 * actual_time / ref_time_per_ms);

    ret = _audio_client->Start();
    if (FAILED(ret)) {
        ErrorLog("Start failed. ret=0x%x", ret);
        goto __Exit;
    }

    DWORD flags = 0;
    BYTE* audio_data = nullptr;
    UINT32 data_size = 0;
    bool in_working = true;
    HANDLE events[2];
    events[0] = _quit_event;
    events[1] = _ready_event;
    while (in_working) {
        DWORD retval = WaitForMultipleObjects(2, events, FALSE, time_out_ms);
        if (retval == WAIT_OBJECT_0 + 0) {
            in_working = false;
        }
        else if (WAIT_OBJECT_0 + 1) {
            ret = _audio_capture->GetBuffer(&audio_data,
                                              &data_size,
                                              &flags,
                                              NULL,
                                              NULL);
            if (FAILED(ret)) {
                ErrorLog("GetBuffer failed. ret=0x%x", ret);
                goto __Exit;
            }

            if (flags & AUDCLNT_BUFFERFLAGS_SILENT) {
                memset(_data_buf.get(), 0x00, sizeof(short) * _buf_size_in_sample);
            }
            else {
                float energy = 0.0f;
                bool rc = ParseAudioData((char*)audio_data,
                                          (int)data_size,
                                          &energy);
                if (!rc) {
                    ErrorLog("ParseAudioData failed");
                    ret = E_FAIL;
                    goto __Exit;
                }

                for (UINT32 i = 0; i < data_size; i++) {
                    _audio_buf[_idx_in_sample] = _data_buf[i];
                    _idx_in_sample++;
                    if (_idx_in_sample >= _buf_size_in_sample) {
                        _idx_in_sample = 0;
                    }
                }

                if (GetAvailableSampleNum() >= _frame_size_in_sample) {
                    char* buf = (char*)(&_audio_buf[_idx_in_frame]);
                    int len = _frame_size_in_sample * 2;
                    PutAudioData(buf, len, _sample_rate);

                    _idx_in_frame += _frame_size_in_sample;
                    if (_idx_in_frame >= _buf_size_in_sample) {
                        _idx_in_frame = 0;
                    }

                    if (_audio_meter) {
                        float peak_val = energy;

                        if (_audio_sink) {
                            _audio_sink->OnPeakValue(peak_val);
                        }
                    }
                }
            }

            ret = _audio_capture->ReleaseBuffer(data_size);
            if (FAILED(ret)) {
                ErrorLog("ReleaseBuffer failed. ret=0x%x", ret);
                goto __Exit;
            }
        }
    }

__Exit:
    if (_audio_client) {
        _audio_client->Stop();
    }

    _audio_meter = NULL;
    _audio_capture = NULL;
    _agc_control = NULL;
    _audio_client = NULL;

    CoUninitialize();
    return ret;
}

bool PeopleAudioCapture::ParseAudioData(char* audio_data, 
                                        int frame_num, 
                                        float* meter) {
    if (_wave_format_type != WaveFormatType::kFormatPCM &&
        _wave_format_type != WaveFormatType::kFormatI3EFloat) {
        ErrorLog("ParseAudioData failed, format type is unknown");
        return false;
    }

    int bits_per_sample = (int)_wave_format_ex->wBitsPerSample;
    int start_byte_offset = 0;
    int chan_offset = 2;
    if (bits_per_sample == 8) {
        start_byte_offset = 0;
        chan_offset = 1;  
    }
    else if (bits_per_sample == 16) {
        start_byte_offset = 0;
        chan_offset = 2;
    }
    else if (bits_per_sample == 24) {
        start_byte_offset = 1;
        chan_offset = 3;
    }
    else if (bits_per_sample == 32) {
        start_byte_offset = 2;
        chan_offset = 4;
    }
    else {
        ErrorLog("ParseAudioData failed. bits_per_sample=%d", bits_per_sample);
        return false;
    }

    memset(_data_buf.get(), 0x00, sizeof(short)*_buf_size_in_sample);

    const int kMonoMaxChannelNum = 4;
    int chan_num = _channel_num;
    if (chan_num > kMonoMaxChannelNum) {
        chan_num = kMonoMaxChannelNum;
    }

    const float kScaleFactorChannel2To1 = 0.707f;
    const float kScaleFactorChannel3To1 = 0.577f;
    const float kScaleFactorChannel4To1 = 0.5f;
    float scale_factor = 1.0f;
    if (chan_num == 2) {
        scale_factor = kScaleFactorChannel2To1;
    }
    else if (chan_num == 3) {
        scale_factor = kScaleFactorChannel3To1;
    }
    else if (chan_num == 4) {
        scale_factor = kScaleFactorChannel4To1;
    }
    
    float temp_energy = 0.0f;
    if (_wave_format_type == WaveFormatType::kFormatPCM) {
        if (_channel_num == 1 && bits_per_sample == 16) {
            short* data = (short*)audio_data;
            for (int i = 0; i < frame_num; ++i) {
                _data_buf[i] = data[i];

                float e = (float)data[i];
                if (e < 0) e = -e;
                if (e > 32767) e = 32767;
                temp_energy += e;
            }
        }
        else if (_channel_num == 2 && bits_per_sample == 16) {
            short* data = (short*)audio_data;
            for (int i = 0; i < frame_num; i++) {
                float f = ((float)data[0] + (float)data[1]) * scale_factor;
                if (f > 32767.0f) f = 32767.0f;
                else if (f < -32768.0f) f = -32768.0f;
                _data_buf[i] = (short)(f * scale_factor);

                float e = (float)_data_buf[i];
                if (e < 0) e = -e;
                if (e > 32767) e = 32767;
                temp_energy += e;
                data += 2;
            }
        }
        else if (bits_per_sample == 8) {
            char* data = (char*)audio_data;
            for (int i = 0; i < frame_num; i++) {
                float f = 0.f;
                for (int j = 0; j < _channel_num; j++) {
                    if (j < chan_num) {
                        short s = (short)(data[0] << 8);
                        f += (float)s;
                    }
                    data += chan_offset;
                }

                if (f > 32767.0f) f = 32767.0f;
                else if (f < -32768.0f) f = -32768.0f;
                _data_buf[i] = (short)(f * scale_factor);

                float e = (float)_data_buf[i];
                if (e < 0) e = -e;
                if (e > 32767) e = 32767;
                temp_energy += e;
            }
        }
        else {
            unsigned char* data = (unsigned char*)audio_data;
            for (int i = 0; i < frame_num; i++) {
                float f = 0.f;
                for (int j = 0; j < _channel_num; j++) {
                    if (j < chan_num) {
                        short s = ((((short)data[1 + start_byte_offset]) << 8) |
                                    ((short)data[0 + start_byte_offset]));
                        f += (float)s;
                    }

                    data += chan_offset;
                }

                if (f > 32767.0f) f = 32767.0f;
                else if (f < -32768.0f) f = -32768.0f;
                _data_buf[i] = (short)(f * scale_factor);

                float e = (float)_data_buf[i];
                if (e < 0) e = -e;
                if (e > 32767) e = 32767;
                temp_energy += e;
            }
        }
    }
    else if (_wave_format_type == WaveFormatType::kFormatI3EFloat) {
        if (_channel_num == 1) {
            float* data = (float*)audio_data;
            for (int i = 0; i < frame_num; i++) {
                _data_buf[i] = (short)(((*data) * 32767.0f));

                float e = (float)_data_buf[i];
                if (e < 0) e = -e;
                if (e > 32767) e = 32767;
                temp_energy += e;

                data++;
            }
        }
        else {
            float* data = (float*)audio_data;
            for (int i = 0; i < frame_num; i++) {
                float f = 0.f;
                for (int j = 0; j < _channel_num; j++) {
                    f += ((*data) * 32767.0f);
                    data++;
                }

                if (f > 32767.0f) f = 32767.0f;
                else if (f < -32768.0f) f = -32768.0f;
                _data_buf[i] = (short)(f * scale_factor);

                float e = (float)_data_buf[i];
                if (e < 0) e = -e;
                if (e > 32767) e = 32767;
                temp_energy += e;
            }
        }
    }

    temp_energy *= (1.0f / frame_num);
    temp_energy /= 32767.0f;

    if (meter) {
        *meter = temp_energy;
    }

    return true;
}

void PeopleAudioCapture::PutAudioData(LPVOID buff, DWORD len, DWORD sample_rate) {
    if (_audio_sink) {
        _audio_sink->OnWriteData(buff, len, sample_rate);    
    }
}

int PeopleAudioCapture::GetAvailableSampleNum() {
    int sample_num = 0;
    if (_idx_in_sample >= _idx_in_frame) {
        sample_num = _idx_in_sample - _idx_in_frame;
    }
    else {
        sample_num = (_buf_size_in_sample + _idx_in_sample) - _idx_in_frame;
    }

    return sample_num;
}

bool PeopleAudioCapture::FindAGC(LPWSTR part_name) {
    int i = 0;
    bool agc1 = false;
    bool agc2 = false;
    int len = _tcslen(part_name);
    for (i = 0; i < len; i++) {
        int wlen = wcslen(_T("AGC"));
        if (_tcsnccmp(&part_name[i], _T("AGC"), wlen) == 0) {
            agc1 = true;
            break;
        }
    }

    if (!agc1) {
        int wlen = wcslen(_T("Automatic Gain Control"));
        for (i = 0; i < len; i++) {
            if (_tcsnccmp(&part_name[i], _T("Automatic Gain Control"), wlen) == 0) {
                agc2 = true;
                break;
            }
        }
    }

    return(agc1 || agc2);
}

IAudioAutoGainControl* PeopleAudioCapture::SearchAGC(IPart* part, bool by_name) {
    HRESULT ret = S_OK;
    IAudioAutoGainControl* agc = NULL;

    if (by_name) {
        LPWSTR part_name = NULL;
        ret = part->GetName(&part_name);
        if (FAILED(ret)) {
            ErrorLog("Could not get part name: ret = 0x%08x", ret);
            return NULL;
        }

        std::string name_str = CW2A(part_name);
        DebugLog("SearchAGC, part->GetName: %s", name_str.c_str());

        bool found = FindAGC(part_name);
        CoTaskMemFree(part_name);
        if (found) {
            ret = part->Activate(CLSCTX_ALL,
                                 __uuidof(IAudioAutoGainControl),
                                 (void**)&agc);
            if (FAILED(ret)) {
                ErrorLog("Activate failed. ret=0x%x", ret);
                return NULL;
            }

            InfoLog("Found AGC interface");
            return agc;
        }
    }
    else {
        GUID sub_type;
        memset(&sub_type, 0x00, sizeof(GUID));
        ret = part->GetSubType(&sub_type);
        if (FAILED(ret)) {
            ErrorLog("GetSubType failed. ret = 0x%08x", ret);
            return NULL;
        }

        if (IsEqualGUID(sub_type, KSNODETYPE_AGC)) {
            ret = part->Activate(CLSCTX_ALL,
                                 __uuidof(IAudioAutoGainControl),
                                 (void**)&agc);
            if (FAILED(ret)) {
                ErrorLog("Activate failed. ret = 0x%08x", ret);
                return NULL;
            }

            InfoLog("Found AGC interface");
            return agc;
        }
    }

    IPartsList* outgoing_part_list = NULL;
    ret = part->EnumPartsOutgoing(&outgoing_part_list);
    if (ret == E_NOTFOUND) {
        ErrorLog("no outgoing parts at this part");
        return NULL;
    }
    else if (FAILED(ret)) {
        ErrorLog("enumrate outgoing parts failed. ret = 0x%08x", ret);
        return NULL;
    }

    UINT part_count = 0;
    ret = outgoing_part_list->GetCount(&part_count);
    if (FAILED(ret)) {
        ErrorLog("GetCount failed, ret = 0x%08x", ret);
        outgoing_part_list->Release();
        return NULL;
    }

    UINT n = 0;
    for (n = 0; n < part_count; n++) {
        IPart* outgoing_part = NULL;
        ret = outgoing_part_list->GetPart(n, &outgoing_part);
        if (FAILED(ret)) {
            ErrorLog("GetPart failed. ret = 0x%08x", ret);
            outgoing_part_list->Release();
            return NULL;
        }

        agc = SearchAGC(outgoing_part, by_name);
        outgoing_part->Release();
        if (agc) {
            break;
        }
    }

    outgoing_part_list->Release();

    return agc;
}

IAudioAutoGainControl* PeopleAudioCapture::GetAGC(IMMDevice* device) {
    CComPtr<IDeviceTopology> device_topology = NULL;
    HRESULT ret = device->Activate(__uuidof(IDeviceTopology),
                                      CLSCTX_ALL,
                                      NULL,
                                      (void**)&device_topology);
    if (FAILED(ret)) {
        ErrorLog("Activate IDeviceTopology failed. ret=0x%x", ret);
        return NULL;
    }

    CComPtr<IConnector> connector = NULL;
    ret = device_topology->GetConnector(0, &connector);
    if (FAILED(ret)) {
        ErrorLog("GetConnector failed. ret=0x%x", ret);
        return NULL;
    }

    CComPtr<IConnector> connected_to = NULL;
    ret = connector->GetConnectedTo(&connected_to);
    if (FAILED(ret)) {
        ErrorLog("GetConnectedTo failed. ret=0x%x", ret);
        return NULL;
    }

    CComPtr<IPart> part = NULL;
    ret = connected_to->QueryInterface(__uuidof(IPart), (void**)&part);
    if (FAILED(ret)) {
        ErrorLog("QueryInterface failed, ret=0x%08x", ret);
        return NULL;
    }

    CComPtr<IAudioAutoGainControl> agc = SearchAGC(part, false);
    if (!agc) {
        InfoLog("find AGC by subtype failed, search again by name");
        agc = SearchAGC(part, true);
        if (!agc) {
            InfoLog("no AGC found");
        }
    }

    return agc;
}

HRESULT PeopleAudioCapture::CheckFormat() {
    CComPtr<IPropertyStore> property_store;

    HRESULT ret = _capture_device->OpenPropertyStore(STGM_READ, &property_store);
    if (FAILED(ret)) {
        ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
        return ret;
    }

    PWAVEFORMATEX wave_format_ex = NULL;
    ret = _audio_client->GetMixFormat(&wave_format_ex);
    if (FAILED(ret)) {
        ErrorLog("GetMixFormat failed. ret=0x%x", ret);
        return ret;
    }
    else {
        InfoLog("get audio format success. FormatTag=%d, Channels=%d, SamplesPerSec=%d, "
                "BlockAlign=%d, BitsPerSample=%d",
                 wave_format_ex->wFormatTag, wave_format_ex->nChannels,
                 wave_format_ex->nSamplesPerSec, wave_format_ex->nBlockAlign,
                 wave_format_ex->wBitsPerSample);
    }

    _wave_format_type = WaveFormatType::kFormatUnknown;
    PROPVARIANT prop_variant;
    if (_exclusive_mode) {
        CoTaskMemFree(wave_format_ex);
        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_BLOB;
        ret = property_store->GetValue(PKEY_AudioEngine_DeviceFormat, &prop_variant);
        if (FAILED(ret)) {
            PropVariantClear(&prop_variant);
            ErrorLog("GetValue PKEY_AudioEngine_DeviceFormat failed. ret=0x%x", ret);
            return ret;
        }

        wave_format_ex = (PWAVEFORMATEX)prop_variant.blob.pBlobData;
        InfoLog("get format for Exclusive mode. FormatTag=%d, Channels=%d, "
                "SamplesPerSec=%d, BlockAlign=%d, BitsPerSample=%d",
                 wave_format_ex->wFormatTag, wave_format_ex->nChannels,
                 wave_format_ex->nSamplesPerSec, wave_format_ex->nBlockAlign,
                 wave_format_ex->wBitsPerSample);
    }

    if (wave_format_ex->wFormatTag == WAVE_FORMAT_PCM) {
        DebugLog("audio FormatTag=WAVE_FORMAT_PCM");

        _wave_format_type = WaveFormatType::kFormatPCM;
        memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEX));
        _wave_format_ex->cbSize = 0;
    }
    else if (wave_format_ex->wFormatTag == WAVE_FORMAT_IEEE_FLOAT) {
        DebugLog("audio FormatTag=WAVE_FORMAT_IEEE_FLOAT");

        _wave_format_type = WaveFormatType::kFormatI3EFloat;
        memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEX));
        _wave_format_ex->cbSize = 0;
    }
    else if (wave_format_ex->wFormatTag == WAVE_FORMAT_EXTENSIBLE) {
        PWAVEFORMATEXTENSIBLE format_ex = (PWAVEFORMATEXTENSIBLE)wave_format_ex;
        GUID guid_pcm;
        CLSIDFromString(_T("{00000001-0000-0010-8000-00aa00389b71}"), &guid_pcm);
        BOOL rc = IsEqualGUID(format_ex->SubFormat, guid_pcm);
        if (rc) {
            DebugLog("audio FormatTag=WAVE_FORMAT_EXTENSIBLE, subtype=PCM");

            _wave_format_type = WaveFormatType::kFormatPCM;
            memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEXTENSIBLE));
        }
        else {
            GUID guid_float;
            CLSIDFromString(_T("{00000003-0000-0010-8000-00aa00389b71}"), &guid_float);
            rc = IsEqualGUID(format_ex->SubFormat, guid_float);
            if (rc) {
                DebugLog("audio FormatTag=WAVE_FORMAT_EXTENSIBLE, subtype=FLOAT");

                _wave_format_type = WaveFormatType::kFormatI3EFloat;
                memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEXTENSIBLE));
            }
            else {
                ErrorLog("audio FormatTag=WAVE_FORMAT_EXTENSIBLE, subtype is unknown");
            }
        }
    }
    else {
        ErrorLog("unsupported FormatTag(0x%x)", wave_format_ex->wFormatTag);
    }

    if (_exclusive_mode) {
        PropVariantClear(&prop_variant);
    }
    else {
        CoTaskMemFree(wave_format_ex);
    }

    if (_wave_format_type == WaveFormatType::kFormatUnknown) {
        ErrorLog("CheckFormat failed");
        return E_FAIL;
    }

    if (_wave_format_ex->wBitsPerSample != 8 &&
        _wave_format_ex->wBitsPerSample != 16 &&
        _wave_format_ex->wBitsPerSample != 24 &&
        _wave_format_ex->wBitsPerSample != 32) {
        ErrorLog("CheckFormat failed, unsupported BitsPerSample %d",
                  _wave_format_ex->wBitsPerSample);
        return E_FAIL;
    }

    bool found = false;
    wave_format_ex = (PWAVEFORMATEX) new char[sizeof(WAVEFORMATEXTENSIBLE)];
    memcpy(wave_format_ex, _wave_format_ex, sizeof(WAVEFORMATEXTENSIBLE));
    const int kSampleRateCount = 5;
    const int supported_sample_rates[kSampleRateCount] = { 48000, 
                                                           44100, 
                                                           32000, 
                                                           16000, 
                                                           8000 };
    for (int i = 0; i < kSampleRateCount; i++) {
        WAVEFORMATEX* closest_match = NULL;
        wave_format_ex->nSamplesPerSec = supported_sample_rates[i];
        wave_format_ex->nAvgBytesPerSec = wave_format_ex->nBlockAlign * 
                                          wave_format_ex->nSamplesPerSec;
        ret = _audio_client->IsFormatSupported(
            _exclusive_mode ? AUDCLNT_SHAREMODE_EXCLUSIVE : AUDCLNT_SHAREMODE_SHARED,
            wave_format_ex,
            _exclusive_mode ? NULL : &closest_match);
        if (ret == S_OK) {
            DebugLog("people audio samplesPerSec=%d", wave_format_ex->nSamplesPerSec);
            memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEXTENSIBLE));

            if (closest_match) {
                CoTaskMemFree(closest_match);
            }

            closest_match = NULL;
            found = true;
            break;
        }

        if (closest_match) {
            CoTaskMemFree(closest_match);
        }
    }

    if (wave_format_ex) {
        delete[] wave_format_ex;
    }

    if (_exclusive_mode) {
        InfoLog("check audio EXCLUSIVE mode success. sample rate=%d",
                 _wave_format_ex->nSamplesPerSec);
    }
    else {
        InfoLog("check audio SHARED mode success. sample rate=%d",
                 _wave_format_ex->nSamplesPerSec);
    }

    if (!found) {
        return E_FAIL;
    }

    return S_OK;
}

ContentAudioCapture::ContentAudioCapture()
    : _exclusive_mode(false),
    _audio_sink(nullptr),
    _audio_monitor(nullptr),
    _capture_thread(NULL),
    _wave_format_ex((PWAVEFORMATEX) new char[sizeof(WAVEFORMATEXTENSIBLE)]) {
}

ContentAudioCapture::~ContentAudioCapture() {
    if (_capture_thread) {
        StopCapture();
    }

    if (_wave_format_ex) {
        delete[] _wave_format_ex;
    }

    if (_audio_monitor) {
        _device_enumerator->UnregisterEndpointNotificationCallback(_audio_monitor);
        _audio_monitor->Release();
        _audio_monitor = NULL;
    }

    CoUninitialize();
}

void ContentAudioCapture::StartCapture() {
    if (_capture_thread != NULL) {
        ErrorLog("content audio capture thread exist, return...");
        return;
    }

    _quit_thread = false;
    _capture_thread = ::CreateThread(NULL, 
                                     0, 
                                     ContentAudioCapture::CaptureThreadFunc, 
                                     this, 
                                     0, 
                                     NULL);
}

void ContentAudioCapture::StopCapture() {
    _quit_thread = true;

    if (_capture_thread) {
        DWORD ret = WaitForSingleObject(_capture_thread, 2000);
        if (ret == WAIT_OBJECT_0) {
            CloseHandle(_capture_thread);
            _capture_thread = NULL;
        }
        else {
            WarnLog("WaitForSingleObject failed. ret = 0x%x, error=0x%x",
                     ret, GetLastError());
        }
    }
}

void ContentAudioCapture::SetDeviceId(const GUID& id) {
    _device_id = id;
}

void ContentAudioCapture::GetDeviceId(LPGUID id) {
    *id = _device_id;
}

void ContentAudioCapture::ResetDevice(const GUID& id) {
    StopCapture();
    SetDeviceId(id);
    std::string uuid_str;
    FRTCSDK::FRTCSdkUtil::guid_to_string(id, uuid_str);
    TCHAR* uuid_cstr = (TCHAR*)FRTCSDK::FRTCSdkUtil::string_to_wstring(uuid_str).c_str();
    InfoLog("reset device: %ws", uuid_cstr);

    Init(uuid_cstr);
    StartCapture();
}

void ContentAudioCapture::SetAudioSink(IAudioDataSink* audio_sink) {
    _audio_sink = audio_sink;

    if (_audio_monitor) {
        _device_enumerator->UnregisterEndpointNotificationCallback(_audio_monitor);
        _audio_monitor->Release();
        _audio_monitor = NULL;
    }

    _audio_monitor = new ContentAudioMonitor(this);
}

void ContentAudioCapture::OnDefaultDeviceChanged() {
    if (_audio_sink) {
        _audio_sink->OnDefaultAudioChanged();
    }
}

HRESULT ContentAudioCapture::Init(TCHAR* device_guid) {
    CoInitialize(NULL);

    _device_idx = 0;
    _device_count = 0;
    _capture_device = NULL;

    CComPtr<IPropertyStore> property_store;
    CComPtr<IMMDeviceCollection> device_collection;
    TCHAR device_guid_str[kCaptureDeviceGuidLen];
    PROPVARIANT prop_variant;
    HRESULT ret;
    ret = _device_enumerator.CoCreateInstance(__uuidof(MMDeviceEnumerator));
    if (FAILED(ret)) {
        ErrorLog("create MMDeviceEnumerator failed. ret=0x%x", ret);
        return ret;
    }

    if (device_guid == NULL) {
        ret = _device_enumerator->GetDefaultAudioEndpoint(eRender,
                                                         eConsole,
                                                         &_capture_device);
        if (FAILED(ret)) {
            ErrorLog("GetDefaultAudioEndpoint failed. ret=0x%x", ret);
            return ret;
        }

        ret = _capture_device->OpenPropertyStore(STGM_READ, &property_store);
        if (FAILED(ret)) {
            ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
            _capture_device = NULL;
            return ret;
        }

        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_LPWSTR;
        ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID, 
                                       &prop_variant);
        if (FAILED(ret)) {
            ErrorLog("GetValue PKEY_AudioEndpoint_GUID failed. ret=0x%x", ret);
            _capture_device = NULL;
            return ret;
        }
        wcsncpy_s(device_guid_str, 
                  kCaptureDeviceGuidLen - 1, 
                  prop_variant.bstrVal, 
                  _TRUNCATE);
        PropVariantClear(&prop_variant);

        device_guid = &device_guid_str[0];
        InfoLog("default content audio device found, guid=%ws", device_guid);

        property_store = NULL;
        _capture_device = NULL;
    }
    wcsncpy_s(_device_guid, kCaptureDeviceGuidLen - 1, device_guid, _TRUNCATE);

    InfoLog("content audio device found, guid=%ws, len=%d", 
             _device_guid, wcslen(_device_guid));

    GUID guid_tmp = { 0 };
    CLSIDFromString(_device_guid, &guid_tmp);
    SetDeviceId(guid_tmp);

    ret = _device_enumerator->EnumAudioEndpoints(eRender,
                                                 DEVICE_STATE_ACTIVE,
                                                 &device_collection);
    if (FAILED(ret)) {
        ErrorLog("EnumAudioEndpoints failed, ret=0x%x", ret);
        return ret;
    }

    ret = device_collection->GetCount(&_device_count);
    if (FAILED(ret)) {
        ErrorLog("GetCount failed. ret=0x%x", ret);
        return ret;
    }

    if (_device_count == 0) {
        ErrorLog("no content audio device found, exit");
        return ret;
    }
    InfoLog("content audio device found, count=%d", _device_count);

    ret = _device_enumerator->RegisterEndpointNotificationCallback(_audio_monitor);

    bool found = false;
    UINT idx = 0;
    for (idx = 0; idx < _device_count; idx++) {
        _capture_device = NULL;
        ret = device_collection->Item(idx, &_capture_device);
        if (FAILED(ret)) {
            ErrorLog("Item failed. ret=0x%x", ret);
            return ret;
        }

        ret = _capture_device->OpenPropertyStore(STGM_READ, &property_store);
        if (FAILED(ret)) {
            ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
            _capture_device = NULL;
            return ret;
        }

        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_LPWSTR;
        ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID, 
                                        &prop_variant);
        InfoLog("content audio device found. index=%d, guid=%ws, len=%d",
                 idx, prop_variant.pwszVal, wcslen(prop_variant.pwszVal));

        if (SUCCEEDED(ret) && _wcsicmp(_device_guid, prop_variant.pwszVal) == 0) {
            _device_idx = idx;
            found = true;
            break;
        }
        PropVariantClear(&prop_variant);
        property_store = NULL;
    }

    if (!found) {
        _capture_device = NULL;
        ErrorLog("content audio device not found");
        return E_FAIL;
    }

    PropVariantInit(&prop_variant);
    prop_variant.vt = VT_LPWSTR;
    ret = property_store->GetValue(PKEY_Device_FriendlyName, &prop_variant);
    if (SUCCEEDED(ret)) {
        _device_name = CW2A(prop_variant.pwszVal);
        
        InfoLog("Init, content audio device found, index=%d, name=%s", 
                 idx, _device_name.c_str());
    }
    PropVariantClear(&prop_variant);

    ret = CheckFormat(_exclusive_mode);
    if (FAILED(ret)) {
        ErrorLog("CheckFormat %s failed. ret=0x%x",
                 _exclusive_mode ? "EXCLUSIVE MODE" : "SHARED MODE", ret);
        if (_exclusive_mode) {
            InfoLog("Check format for SHARED MODE");
            ret = CheckFormat(false);
            if (FAILED(ret)) {
                ErrorLog("CheckFormat SHARED MODE failed. ret=0x%x", ret);
                return ret;
            }
            _exclusive_mode = false;
        }
    }

    if (_wave_format_ex) {
        _channel_count = _wave_format_ex->nChannels;
        _sample_rate = _wave_format_ex->nSamplesPerSec;
    }
    else {
        ErrorLog("wave_format_ex is NULL, init failed!");
        return S_FALSE;
    }

    return S_OK;
}

DWORD WINAPI ContentAudioCapture::CaptureThreadFunc(LPVOID context) {
    SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL);
    return ((ContentAudioCapture*)context)->CaptureFunc();
}

DWORD ContentAudioCapture::CaptureFunc() {
    CoInitialize(NULL);

    CComPtr<IAudioClient> audio_client;
    CComPtr<IAudioCaptureClient> audio_capture;
    HRESULT ret;
    if (_capture_device == NULL) {
        ErrorLog("capture_device is NULL, exit");
        ret = E_FAIL;
        goto __Exit;
    }

    ret = _capture_device->Activate(__uuidof(IAudioClient),
                                    CLSCTX_ALL,
                                    NULL,
                                    (void**)&audio_client);
    if (FAILED(ret)) {
        ErrorLog("Activate failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = audio_client->Initialize(
              AUDCLNT_SHAREMODE_SHARED,
              AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_LOOPBACK,
              5 * 10000000,
              0,
              _wave_format_ex,
              NULL);
    if (FAILED(ret)) {
        ErrorLog("Initialize content audio device failed. ret=0x%x", ret);
        goto __Exit;
    }

    ret = audio_client->GetService(__uuidof(IAudioCaptureClient),
                                   (void**)&audio_capture);
    if (FAILED(ret)) {
        ErrorLog("GetService failed, ret=0x%x", ret);
        goto __Exit;
    }

    HANDLE capture_event = CreateEvent(NULL, FALSE, FALSE, NULL);
    if (capture_event == NULL) {
        ErrorLog("CreateEvent failed");
        goto __Exit;
    }

    ret = audio_client->SetEventHandle(capture_event);
    if (FAILED(ret)) {
        ErrorLog("SetEventHandle failed, ret=0x%x", ret);
        goto __Exit;
    }

    ret = audio_client->Start();
    if (FAILED(ret)) {
        ErrorLog("Start content audio failed, ret=0x%x", ret);
        goto __Exit;
    }

    while (!_quit_thread) {
        DWORD retval = WaitForSingleObject(capture_event, 10);
        if (retval == WAIT_OBJECT_0 || retval == WAIT_TIMEOUT) {
            ProcessAudioData(audio_client, audio_capture);
        }
        else {
            continue;
        }
    }

    ret = audio_client->Stop();
    if (FAILED(ret)) {
        ErrorLog("Stop content audio failed, ret=0x%x", ret);
        goto __Exit;
    }

__Exit:

    if (capture_event) CloseHandle(capture_event);
    audio_capture = NULL;
    audio_client = NULL;

    CoUninitialize();
    if (FAILED(ret)) {
        ErrorLog("ContentAudioCapture thread exit, error=0x%08x", ret);
        _capture_thread = NULL;
        return ret;
    }

    return 0;
}

bool ContentAudioCapture::ProcessAudioData(CComPtr<IAudioClient>& audio_client,
                                    CComPtr<IAudioCaptureClient>& audio_capture) {
    if (!audio_capture) {
        return false;
    }

    unsigned int last_ts_us = FRTCSDK::FRTCSdkUtil::timestamp();
    unsigned char* audio_data = NULL;
    UINT32 data_size = 0;
    short pcm_48k[4096];

    do {
        DWORD flags;
        UINT32 pkt_size = 0;
        UINT64 time_stamp = 0;
        HRESULT ret = audio_capture->GetNextPacketSize(&pkt_size);
        if (FAILED(ret)) {
            ErrorLog("GetNextPacketSize failed. ret=0x%1X, size=%d",
                      ret, pkt_size);
            break;
        }

        ret = audio_capture->GetBuffer(&audio_data,
                                       &data_size,
                                       &flags,
                                       NULL,
                                       &time_stamp);
        if (FAILED(ret)) {
            ErrorLog("GetBuffer failed. ret=0x%1X", ret);
            break;
        }

        if (data_size * _wave_format_ex->nBlockAlign != 0 &&
            _wave_format_ex->nSamplesPerSec != 0) {
            if (data_size > 480) {
                WarnLog("content audio capture, get too many samples(%d), block "
                        "align=%d, samples per sec=%d",
                          data_size, _wave_format_ex->nBlockAlign,
                          _wave_format_ex->nSamplesPerSec);
            }

            memset(pcm_48k, 0, 4096 * sizeof(short));
            ResampleToOneChannel(pcm_48k,
                                 audio_data,
                                 _wave_format_ex->nChannels,
                                 data_size);

            unsigned int current_ts_us = FRTCSDK::FRTCSdkUtil::timestamp();
            unsigned int gap = current_ts_us - last_ts_us;
            if (gap > 20000) {
                WarnLog("sample time inteval too long(%u us)", gap);
            }
            last_ts_us = current_ts_us;

            PutAudioData(pcm_48k,
                         data_size * sizeof(short),
                         _wave_format_ex->nSamplesPerSec);
        }
        else {
            audio_capture->ReleaseBuffer(data_size);
            break;
        }

        audio_capture->ReleaseBuffer(data_size);

    } while (true);

    return true;
}

void ContentAudioCapture::PutAudioData(LPVOID buff, DWORD len, DWORD sample_rate) {
    if (_audio_sink) {
        _audio_sink->OnWriteDataContent(buff, len, sample_rate);
    }
}

void ContentAudioCapture::ResampleToOneChannel(short* out_data,
                                        unsigned char* in_data,
                                        int channel_num,
                                        unsigned int data_size) {
    if (!out_data || !in_data) {
        return;
    }

    if (channel_num < 1 || channel_num > 2) {
        return;
    }

    unsigned int i = 0;
    if (channel_num == 2) {
        for (i = 0; i < data_size; i++) {
            out_data[i] = (((short*)in_data)[2 * i]);
        }
    }
    else {
        for (i = 0; i < data_size * channel_num; i++) {
            out_data[i] = (((short*)in_data)[i]);
        }
    }
}

HRESULT ContentAudioCapture::CheckFormat(bool exclusive_mode) {
    CComPtr<IPropertyStore> property_store;
    CComPtr<IAudioClient>   audio_client;

    HRESULT ret = _capture_device->OpenPropertyStore(STGM_READ, &property_store);
    if (FAILED(ret)) {
        ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
        return ret;
    }

    ret = _capture_device->Activate(__uuidof(IAudioClient),
                                    CLSCTX_INPROC_SERVER,
                                    NULL,
                                    (void**)&audio_client);
    if (FAILED(ret)) {
        ErrorLog("Activate failed. ret=0x%x", ret);
        return ret;
    }

    PWAVEFORMATEX wave_format_ex = NULL;
    ret = audio_client->GetMixFormat(&wave_format_ex);
    if (FAILED(ret)) {
        ErrorLog("GetMixFormat failed. ret=0x%x", ret);
        return ret;
    }
    else {
        DebugLog("content audio getMixFormat success. FormatTag=%d, Channels=%d, "
                 "SamplesPerSec=%d, BlockAlign=%d, BitsPerSample=%d",
                 wave_format_ex->wFormatTag, wave_format_ex->nChannels,
                 wave_format_ex->nSamplesPerSec, wave_format_ex->nBlockAlign,
                 wave_format_ex->wBitsPerSample);
    }

    PROPVARIANT prop_variant;
    if (exclusive_mode) {
        CoTaskMemFree(wave_format_ex);
        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_BLOB;
        ret = property_store->GetValue(PKEY_AudioEngine_DeviceFormat, &prop_variant);
        if (FAILED(ret)) {
            PropVariantClear(&prop_variant);
            ErrorLog("conent audio getValue PKEY_AudioEngine_DeviceFormat failed. "
                     "ret=0x%x", 
                     ret);
            return ret;
        }
        wave_format_ex = (PWAVEFORMATEX)prop_variant.blob.pBlobData;
    }

    if (wave_format_ex->wFormatTag == WAVE_FORMAT_IEEE_FLOAT) {
        wave_format_ex->wFormatTag = WAVE_FORMAT_PCM;
        wave_format_ex->wBitsPerSample = 16;
        wave_format_ex->nBlockAlign = (wave_format_ex->wBitsPerSample / 8) * 
                                      wave_format_ex->nChannels;
        wave_format_ex->nAvgBytesPerSec = wave_format_ex->nSamplesPerSec * 
                                          wave_format_ex->nBlockAlign;

        memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEX));
        DebugLog("content audio WAVE_FORMAT_IEEE_FLOAT, BlockAlign=%d, "
                 "AvgBytesPerSec=%d",
                  wave_format_ex->nBlockAlign, 
                  wave_format_ex->nAvgBytesPerSec);
    }
    else if (wave_format_ex->wFormatTag == WAVE_FORMAT_EXTENSIBLE) {
        PWAVEFORMATEXTENSIBLE fmt_ex = reinterpret_cast<PWAVEFORMATEXTENSIBLE>(
                                           wave_format_ex);
        fmt_ex->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
        fmt_ex->Format.wBitsPerSample = 16;
        fmt_ex->Format.nBlockAlign = (wave_format_ex->wBitsPerSample / 8) * 
                                     wave_format_ex->nChannels;
        fmt_ex->Format.nAvgBytesPerSec = fmt_ex->Format.nSamplesPerSec * 
                                         fmt_ex->Format.nBlockAlign;
        fmt_ex->Samples.wValidBitsPerSample = 16;

        memcpy(_wave_format_ex, wave_format_ex, sizeof(WAVEFORMATEXTENSIBLE));
        DebugLog("content audio WAVE_FORMAT_EXTENSIBLE, BlockAlign=%d, "
                 "AvgBytesPerSec=%d",
                  fmt_ex->Format.nBlockAlign, 
                  fmt_ex->Format.nAvgBytesPerSec);
    }

    WAVEFORMATEX* closest_match = NULL;
    ret = audio_client->IsFormatSupported(
              exclusive_mode ? AUDCLNT_SHAREMODE_EXCLUSIVE : AUDCLNT_SHAREMODE_SHARED,
              wave_format_ex,
              exclusive_mode ? NULL : &closest_match);
    if (FAILED(ret)) {
        ErrorLog("IsFormatSupported failed. ret=0x%x", ret);
    }

    if (closest_match) {
        CoTaskMemFree(closest_match);
    }

    closest_match = NULL;

    if (exclusive_mode) {
        PropVariantClear(&prop_variant);
    }
    else {
        CoTaskMemFree(wave_format_ex);
    }

    return S_OK;
}

