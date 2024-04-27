#define INITGUID
#include <audioclient.h>
#include <functiondiscoverykeys.h>
#include <dsound.h>
#include "audio_render.h"
#include "../stdafx.h"
#include "../log.h"
#include "../frtc_sdk_util.h"

AudioRender::AudioRender(void)
    : _exclusive_mode(false),
      _audio_source(nullptr),
      _audio_buf(nullptr),
      _data_buf(nullptr),
      _render_thread(NULL),
      _wave_format_ex((PWAVEFORMATEX) new char[sizeof(WAVEFORMATEXTENSIBLE)]) {
}

AudioRender::~AudioRender() {
    if (_render_thread) {
        StopRender();
    }

    if (_wave_format_ex) {
        delete[] _wave_format_ex;
    }

    CoUninitialize();
}

void AudioRender::StartRender() {
    if (_render_thread != NULL) {
        WarnLog("audio render thread has not stopped, no need to start");
        return;
    }

    _quit_thread = false;
    _render_thread = ::CreateThread(NULL,
                                    0,
                                    AudioRender::RenderThreadFunc,
                                    this,
                                    0,
                                    NULL);
}

void AudioRender::StopRender() {
    _quit_thread = true;
    if (_render_thread) {
        DWORD rc = WaitForSingleObject(_render_thread, 3000);
        if (rc == WAIT_OBJECT_0) {
            CloseHandle(_render_thread);
            _render_thread = NULL;
        }
        else {
            WarnLog("WaitForSingleObject failed. rc=0x%x, error=0x%x",
                     rc, GetLastError());
            _render_thread = NULL;
        }
    }
}

void AudioRender::SetDeviceId(const GUID& id) {
    AutoLock lock(_lock);
    _device_id = id;
}

void AudioRender::GetDeviceId(LPGUID id) {
    AutoLock lock(_lock);
    *id = _device_id;
}

void AudioRender::ResetDevice(const GUID& id) {
    AutoLock lock(_lock);
    StopRender();
    SetDeviceId(id);
    std::string uuid_str;
    FRTCSDK::FRTCSdkUtil::guid_to_string(id, uuid_str);
    std::wstring uuid_wstr = FRTCSDK::FRTCSdkUtil::string_to_wstring(uuid_str);

    InfoLog("reset audio device, guid=%ws", (TCHAR*)uuid_wstr.c_str());
    HRESULT ret = Init((TCHAR*)uuid_wstr.c_str());
    if (SUCCEEDED(ret)) {
        StartRender();
    }
}

void AudioRender::SetAudioSource(IAudioDataSource* audio_source) {
    _audio_source = audio_source;
}

HRESULT AudioRender::Init(TCHAR* device_guid) {
    InfoLog("audio render init. device=%ws", device_guid);

    CoInitialize(NULL);

    _device_index = 0;
    _device_count = 0;
    _render_device = NULL;

    CComPtr<IPropertyStore> property_store;
    CComPtr<IMMDeviceCollection> device_collection;
    CComPtr<IMMDeviceEnumerator> device_enumerator;

    PROPVARIANT prop_variant;
    TCHAR device_guid_str[kRenderDeviceGuidLen];
    HRESULT ret;
    ret = device_enumerator.CoCreateInstance(__uuidof(MMDeviceEnumerator));
    if (FAILED(ret)) {
        ErrorLog("create MMDeviceEnumerator failed. ret=0x%x", ret);
        return ret;
    }

    if (device_guid == NULL) {
        ret = device_enumerator->GetDefaultAudioEndpoint(eRender, 
                                                         eCommunications, 
                                                         &_render_device);
        if (FAILED(ret)) {
            ErrorLog("GetDefaultAudioEndpoint failed, ret=0x%x", ret);
            return ret;
        }

        ret = _render_device->OpenPropertyStore(STGM_READ, &property_store);
        if (FAILED(ret)) {
            ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
            _render_device = NULL;
            return ret;
        }

        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_LPWSTR;
        ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID, 
                                       &prop_variant);
        if (FAILED(ret)) {
            ErrorLog("PKEY_AudioEndpoint_GUID failed, ret=0x%x", ret);
            _render_device = NULL;
            return ret;
        }

        wcsncpy_s(device_guid_str, 
                  kRenderDeviceGuidLen - 1, 
                  prop_variant.bstrVal, 
                  _TRUNCATE);
        PropVariantClear(&prop_variant);

        device_guid = &device_guid_str[0];
        InfoLog("Init, default device found: %ws", device_guid);

        property_store = NULL;
        _render_device = NULL;
    }

    wcsncpy_s(_device_guid, kRenderDeviceGuidLen - 1, device_guid, _TRUNCATE);
    InfoLog("find audio render device, guid=%ws, len=%d", 
             _device_guid, wcslen(_device_guid));

    ret = device_enumerator->EnumAudioEndpoints(eRender, 
                                                DEVICE_STATE_ACTIVE, 
                                                &device_collection);
    if (FAILED(ret)) {
        ErrorLog("EnumAudioEndpoints failed. ret=0x%x", ret);
        return ret;
    }

    ret = device_collection->GetCount(&_device_count);
    if (FAILED(ret)) {
        ErrorLog("GetCount failed. ret=0x%x", ret);
        return ret;
    }

    if (_device_count == 0) {
        ErrorLog("no device has been found");
        return ret;
    }

    InfoLog("found render device. count=%d", _device_count);

    UINT idx = 0;
    bool found = false;
    for (idx = 0; idx < _device_count; idx++) {
        _render_device = NULL;
        ret = device_collection->Item(idx, &_render_device);
        if (FAILED(ret)) {
            ErrorLog("Item failed. ret=0x%x", ret);
            return ret;
        }

        ret = _render_device->OpenPropertyStore(STGM_READ, &property_store);
        if (FAILED(ret)) {
            ErrorLog("OpenPropertyStore failed. ret=0x%x", ret);
            _render_device = NULL;
            return ret;
        }

        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_LPWSTR;
        ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID, 
                                       &prop_variant);
        if (FAILED(ret)) {
            ErrorLog("GetValue PKEY_AudioEndpoint_GUID failed. ret=0x%x", ret);
        }
        else if (_wcsicmp(_device_guid, prop_variant.pwszVal) == 0) {
            _device_index = idx;
            found = true;
            InfoLog("render device found. index=%d, guid=%ws, len=%d",
                 idx, prop_variant.pwszVal, wcslen(prop_variant.pwszVal));

            break;
        }

        PropVariantClear(&prop_variant);
        property_store = NULL;
    }

    if (!found) {
        _render_device = NULL;
        ErrorLog("failed to find render device");
        return E_FAIL;
    }

    PropVariantInit(&prop_variant);
    prop_variant.vt = VT_LPWSTR;
    ret = property_store->GetValue(PKEY_Device_FriendlyName, &prop_variant);
    if (SUCCEEDED(ret)) {
        int len = wcslen(prop_variant.pwszVal);
        if (len > kMaxRenderDeviceNameLen) {
            len = kMaxRenderDeviceNameLen;
        }

        wcscpy_s(_device_name, len + 1, prop_variant.pwszVal);

        InfoLog("render device found, index=%d, name=%ws", idx, _device_name);
    }
    PropVariantClear(&prop_variant);

    ret = CheckFormat(_exclusive_mode);
    if (FAILED(ret)) {
        ErrorLog("CheckFormat %s failed ret=0x%x",
                 _exclusive_mode ? "EXCLUSIVE MODE" : "SHARED MODE", ret);

        if (_exclusive_mode) {
            InfoLog("CheckFormat for SHARED MODE");

            ret = CheckFormat(false);
            if (FAILED(ret)) {
                ErrorLog("CheckFormat for SHARED MODE failed. ret=0x%x", ret);
                return ret;
            }
            _exclusive_mode = false;
        }
        else {
            return ret;
        }
    }

    _channel_count = _wave_format_ex->nChannels;
    _sample_rate = _wave_format_ex->nSamplesPerSec;
    _buf_size_in_samples = ((_sample_rate / 100) * 2 * _channel_count);

    _audio_buf = std::make_unique<short[]>(_buf_size_in_samples);
    memset(_audio_buf.get(), 0x00, sizeof(short) * _buf_size_in_samples);
    
    int data_buf_size = (_sample_rate / 100) * 2;
    _data_buf = std::make_unique<short[]>(data_buf_size);
    memset(_data_buf.get(), 0x00, sizeof(short) * data_buf_size);

    DebugLog("init render finish, audio_buf size=%d, data_buf size=%d", 
              _buf_size_in_samples, data_buf_size);

    return S_OK;
}

HRESULT AudioRender::UpdateDeviceList(bool set_default) {
    HRESULT ret;
    AutoLock lock(_lock);
    std::vector <DevInfo> backup = _device_list;
    _device_list.clear();

    ret = ::DirectSoundEnumerate(AudioRender::EnumerateDevice, this);
    if (FAILED(ret)) {
        WarnLog("DirectSoundEnumerate failed, ret=0x%x", ret);
        _device_list = backup;
    }
    else {
        InfoLog("update audio render device success, device count=%d",
                 _device_list.size());
    }

    if (set_default && !_device_list.empty()) {
        _device_id = _device_list[0].second;
    }

    return ret;
}

std::vector <DevInfo> AudioRender::GetDeviceList() {
    AutoLock lock(_lock);
    return _device_list;
}

HRESULT AudioRender::GetDefaultDeviceId(std::wstring& id) {
    CComPtr<IMMDeviceEnumerator> device_enumerator;
    CComPtr<IPropertyStore> property_store;
    CComPtr<IMMDevice> audio_device;

    CoInitialize(NULL);

    HRESULT ret;
    ret = device_enumerator.CoCreateInstance(__uuidof(MMDeviceEnumerator));
    if (FAILED(ret)) {
        ErrorLog("create MMDeviceEnumerator failed, ret=0x%x", ret);
        return ret;
    }

    ret = device_enumerator->GetDefaultAudioEndpoint(eRender, 
                                                     eConsole, 
                                                     &audio_device);
    if (FAILED(ret)) {
        ErrorLog("GetDefaultAudioEndpoint failed, ret=0x%x", ret);
        return ret;
    }

    ret = audio_device->OpenPropertyStore(STGM_READ, &property_store);
    if (FAILED(ret)) {
        ErrorLog("OpenPropertyStore failed, ret=0x%x", ret);
        audio_device = NULL;
        return ret;
    }

    PROPVARIANT prop_variant;
    PropVariantInit(&prop_variant);
    prop_variant.vt = VT_LPWSTR;
    ret = property_store->GetValue((PROPERTYKEY&)PKEY_AudioEndpoint_GUID, 
                                   &prop_variant);
    if (FAILED(ret)) {
        ErrorLog("PKEY_AudioEndpoint_GUID failed, ret=0x%x", ret);
        audio_device = NULL;
        return ret;
    }

    TCHAR device_guid_str[kRenderDeviceGuidLen];
    wcsncpy_s(device_guid_str, 
              kRenderDeviceGuidLen - 1, 
              prop_variant.bstrVal, 
              _TRUNCATE);
    PropVariantClear(&prop_variant);

    TCHAR* device_guid = &device_guid_str[0];
    InfoLog("default device found, guid=%ws", device_guid);

    property_store = NULL;
    audio_device = NULL;

    TCHAR guid_tmp[kRenderDeviceGuidLen] = { 0 };
    wcsncpy_s(guid_tmp, kRenderDeviceGuidLen - 1, device_guid, _TRUNCATE);
    id = guid_tmp;

    return ret;
}

HRESULT AudioRender::SyncWithOS() {
    std::wstring device_id;
    HRESULT ret = GetDefaultDeviceId(device_id);
    if (FAILED(ret)) {
        ErrorLog("get os default speaker failed. ret=0x%08x", ret);
        return ret;
    }
    else {
        GUID default_guid = { 0 };
        FRTCSDK::FRTCSdkUtil::get_guid_from_wstring(
                                  static_cast<const TCHAR*>(device_id.c_str()),
                                  &default_guid);
        if (memcmp(&default_guid, &_device_id, sizeof(GUID)) != 0) {
            ResetDevice(default_guid);
        }
    }

    return S_OK;
}

bool AudioRender::CheckDeviceExist() {
    HRESULT ret = UpdateDeviceList(false);
    if (FAILED(ret)) {
        ErrorLog("update AudioRender device list failed. ret=0x%x", ret);
        return false;
    }

    AutoLock lock(_lock);
    auto list = this->GetDeviceList();
    for (auto d : list) {
        if (IsEqualGUID(d.second, this->_device_id)) {
            return true;
        }
    }

    return false;
}

BOOL CALLBACK AudioRender::EnumerateDevice(LPGUID device_guid,
                                           LPCTSTR device_desc,
                                           LPCTSTR drv_name,
                                           LPVOID context) {
    AudioRender* audio_render = (AudioRender*)context;
    if (device_guid != NULL &&
        device_desc != NULL &&
        wcsstr(device_desc, L"Stereo") == NULL &&
        wcsstr(device_desc, L"stereo") == NULL) {
        std::wstring guid_str;
        FRTCSDK::FRTCSdkUtil::guid_to_wstring(*device_guid, guid_str);

        InfoLog("get available audio device, name=%ws, id=%ws",
                 device_desc, guid_str.c_str());

        audio_render->_device_list.push_back(std::pair<std::wstring, GUID>(
            std::wstring(device_desc), *device_guid));
    }

    return TRUE;
}

DWORD WINAPI AudioRender::RenderThreadFunc(LPVOID context) {
    if (!SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL)) {
        ErrorLog("audio render thread set priority failed. error=%d", 
                  GetLastError());
    }
    DWORD ret = ((AudioRender*)context)->RenderFunc();
    return ret;
}

DWORD AudioRender::RenderFunc() {
    CoInitialize(NULL);

    HRESULT ret;
    UINT32 buffer_frame_num = 0;
    UINT32 padding_frame_num = 0;
    UINT32 available_frame_num = 0;
    const int ref_time_per_ms = 10000;
    REFERENCE_TIME request_time = 50 * ref_time_per_ms;
    REFERENCE_TIME actual_time = 0;
    CComPtr<IAudioClient> audio_client;
    CComPtr<IAudioRenderClient> render_client;

    if (_render_device == NULL) {
        ErrorLog("render_device is null, exit");
        ret = E_FAIL;
        goto __Exit;
    }

    ret = _render_device->Activate(__uuidof(IAudioClient),
                              CLSCTX_ALL,
                              NULL,
                              (void**)&audio_client);
    if (FAILED(ret)) {
        ErrorLog("Activate failed ret=0x%x", ret);
        goto __Exit;
    }

    bool succeed = true;
    ret = audio_client->Initialize((AUDCLNT_SHAREMODE_SHARED),
                                   AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                                   request_time,
                                   0,
                                   _wave_format_ex,
                                   NULL);
    if (FAILED(ret)) {
        succeed = false;
        WarnLog("Initialize failed. ret=0x%x", ret);

        if (ret == AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED) {
            WarnLog("Initialize failed(AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED). ret=0x%x",
                     ret);

            ret = audio_client->GetBufferSize(&buffer_frame_num);
            if (SUCCEEDED(ret)) {
                request_time = (REFERENCE_TIME)(
                    (10000.0*1000/_wave_format_ex->nSamplesPerSec*buffer_frame_num)+0.5);
                audio_client = NULL;
                ret = _render_device->Activate(__uuidof(IAudioClient),
                                          CLSCTX_ALL,
                                          NULL,
                                          (void**)&audio_client);
                if (SUCCEEDED(ret)) {
                    ret = audio_client->Initialize(AUDCLNT_SHAREMODE_SHARED,
                                                   AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                                                   request_time,
                                                   0,
                                                   _wave_format_ex,
                                                   NULL);
                    if (SUCCEEDED(ret)) {
                        succeed = true;
                    }
                }
            }
        }
    }

    if (!succeed) {
        WarnLog("Initialize failed. ret=0x%x", ret);
        ret = E_FAIL;
        goto __Exit;
    }

    HANDLE render_event = CreateEvent(NULL, FALSE, FALSE, NULL);
    if (render_event == NULL) {
        ErrorLog("event create failed, ret=0x%x", ret);
        ret = E_FAIL;
        goto __Exit;
    }

    ret = audio_client->SetEventHandle(render_event);
    if (FAILED(ret)) {
        ErrorLog("event handle set failed, ret=0x%x", ret);
        goto __Exit;
    }

    ret = audio_client->GetService(__uuidof(IAudioRenderClient), 
                                   (void**)&render_client);
    if (FAILED(ret)) {
        ErrorLog("GetService failed, ret=0x%x", ret);
        goto __Exit;
    }

    ret = audio_client->GetBufferSize(&buffer_frame_num);
    if (FAILED(ret)) {
        ErrorLog("GetBufferSize failed, ret=0x%x", ret);
        goto __Exit;
    }

    if (!_wave_format_ex) {
        WarnLog("Initialize failed. wave_form_ex is NULL, ret=0x%x", ret);
        ret = E_FAIL;
        goto __Exit;
    }

    BYTE* frame_data;
    INT32 frame_bytes = buffer_frame_num * _wave_format_ex->nChannels * 
                        _wave_format_ex->wBitsPerSample / 8;
    ret = render_client->GetBuffer(buffer_frame_num, &frame_data);
    if (FAILED(ret)) {
        ErrorLog("GetBuffer failed, buffer size=%d, frame_data=%p, ret=0x%x",
                  buffer_frame_num, frame_data, ret);
        goto __Exit;
    }

    memset((char*)frame_data, 0, frame_bytes);

    ret = render_client->ReleaseBuffer(buffer_frame_num, 0);
    if (FAILED(ret)) {
        ErrorLog("ReleaseBuffer failed, buffer size=%d, ret=0x%x",
                  buffer_frame_num, ret);
        goto __Exit;
    }

    actual_time = (REFERENCE_TIME)((double)request_time *
        buffer_frame_num / _wave_format_ex->nSamplesPerSec);

    ret = audio_client->Start();
    if (FAILED(ret)) {
        ErrorLog("AudioRender Start failed. ret=0x%x", ret);
        goto __Exit;
    }

    InfoLog("audio render thread loop start");

    while (!_quit_thread) {
        DWORD retval = WaitForSingleObject(render_event, 1000);
        if (retval != WAIT_OBJECT_0) {
            continue;
        }

        ret = audio_client->GetCurrentPadding(&padding_frame_num);
        if (FAILED(ret)) {
            ErrorLog("GetCurrentPadding failed. ret=0x%x", ret);
            goto __Exit;
        }

        available_frame_num = buffer_frame_num - padding_frame_num;
        available_frame_num = (available_frame_num > _sample_rate / 50) ?
            _sample_rate / 50 : available_frame_num;

        if (available_frame_num >= _sample_rate / 100) {
            ret = render_client->GetBuffer(available_frame_num, &frame_data);
            if (FAILED(ret)) {
                ErrorLog("GetBuffer failed. ret=0x%x", ret);
                goto __Exit;
            }

            GetAudioData(frame_data,
                         available_frame_num * _wave_format_ex->nBlockAlign,
                         _wave_format_ex->nSamplesPerSec);

            ret = render_client->ReleaseBuffer(available_frame_num, 0);
            if (FAILED(ret)) {
                ErrorLog("ReleaseBuffer failed. ret=0x%x", ret);
                goto __Exit;
            }
        }
    }

    InfoLog("audio render thread loop break");

    Sleep((DWORD)(actual_time / ref_time_per_ms / 2));

    ret = audio_client->Stop();
    if (FAILED(ret)) {
        ErrorLog("Stop failed. ret=0x%x", ret);
        goto __Exit;
    }

__Exit:

    if (render_event) CloseHandle(render_event);
    render_client = NULL;
    audio_client = NULL;

    CoUninitialize();
    if (FAILED(ret)) {
        ErrorLog("AudioRender thread exit on error %08x", ret);
        _render_thread = NULL;
        return ret;
    }
    return 0;
}

bool AudioRender::GetAudioData(LPVOID buff, DWORD len, DWORD sample_rate) {
    const size_t num_samples = len / sizeof(unsigned short);
    memset(_audio_buf.get(), 0x00, sizeof(short) * num_samples);

    if (_audio_source) {
        if (_channel_count == 1) {
            _audio_source->OnReadData((char*)_audio_buf.get(), len, sample_rate);
        }
        else if (_channel_count == 2) {
            memset(_data_buf.get(), 0x00, sizeof(short) * (num_samples / 2));
            _audio_source->OnReadData((char*)_data_buf.get(), 
                                      num_samples, 
                                      sample_rate);

            for (size_t i = 0; i < num_samples / 2; i++) {
                _audio_buf[2 * i] = _data_buf[i];
                _audio_buf[2 * i + 1] = _data_buf[i];
            }
        }
    }

    memcpy((char*)buff, (char*)_audio_buf.get(), len);

    return true;
}

HRESULT AudioRender::CheckFormat(bool exclusive_mode) {
    CComPtr<IPropertyStore> property_store;
    CComPtr<IAudioClient>   audio_client;

    HRESULT ret = _render_device->OpenPropertyStore(STGM_READ, &property_store);
    if (FAILED(ret)) {
        ErrorLog("OpenPropertyStore failed ret=0x%x", ret);
        return ret;
    }

    ret = _render_device->Activate(__uuidof(IAudioClient),
                              CLSCTX_INPROC_SERVER,
                              NULL,
                              (void**)&audio_client);
    if (FAILED(ret)) {
        ErrorLog("Activate failed. ret=0x%x", ret);
        return ret;
    }

    PWAVEFORMATEX wave_format = NULL;
    ret = audio_client->GetMixFormat(&wave_format);
    if (FAILED(ret)) {
        ErrorLog("get mix format failed. ret=0x%x", ret);
        return ret;
    }
    else {
        InfoLog("get audio format success. FormatTag=%d, Channels=%d, SamplesPerSec=%d, "
                "BlockAlign=%d, BitsPerSample=%d",
                 wave_format->wFormatTag, wave_format->nChannels, 
                 wave_format->nSamplesPerSec, wave_format->nBlockAlign, 
                 wave_format->wBitsPerSample);
    }

    PROPVARIANT prop_variant;
    if (exclusive_mode) {
        CoTaskMemFree(wave_format);
        PropVariantInit(&prop_variant);
        prop_variant.vt = VT_BLOB;
        ret = property_store->GetValue(PKEY_AudioEngine_DeviceFormat, &prop_variant);
        if (FAILED(ret)) {
            PropVariantClear(&prop_variant);
            ErrorLog("GetValue PKEY_AudioEngine_DeviceFormat failed ret=0x%x", ret);

            return ret;
        }
        wave_format = (PWAVEFORMATEX)prop_variant.blob.pBlobData;
    }
    
    const int bits_per_sample_supported = 16;
    if (wave_format->wFormatTag == WAVE_FORMAT_IEEE_FLOAT) {
        wave_format->wFormatTag = WAVE_FORMAT_PCM;
        wave_format->wBitsPerSample = bits_per_sample_supported;
        wave_format->nBlockAlign = (wave_format->wBitsPerSample / 8) * 
                                   wave_format->nChannels;
        wave_format->nAvgBytesPerSec = wave_format->nSamplesPerSec * 
                                       wave_format->nBlockAlign;
        memcpy(_wave_format_ex, wave_format, sizeof(WAVEFORMATEX));
    }
    else if (wave_format->wFormatTag == WAVE_FORMAT_EXTENSIBLE) {
        PWAVEFORMATEXTENSIBLE fmt_ex = reinterpret_cast<PWAVEFORMATEXTENSIBLE>(
                                           wave_format);
        fmt_ex->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
        fmt_ex->Format.wBitsPerSample = bits_per_sample_supported;
        fmt_ex->Format.nBlockAlign = (wave_format->wBitsPerSample / 8) * 
                                     wave_format->nChannels;
        fmt_ex->Format.nAvgBytesPerSec = fmt_ex->Format.nSamplesPerSec * 
                                         fmt_ex->Format.nBlockAlign;
        fmt_ex->Samples.wValidBitsPerSample = bits_per_sample_supported;
        memcpy(_wave_format_ex, wave_format, sizeof(WAVEFORMATEXTENSIBLE));
    }

    WAVEFORMATEX* closest_match = NULL;
    ret = audio_client->IsFormatSupported(
            exclusive_mode ? AUDCLNT_SHAREMODE_EXCLUSIVE : AUDCLNT_SHAREMODE_SHARED,
            wave_format,
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
        CoTaskMemFree(wave_format);
    }

    return S_OK;
}


