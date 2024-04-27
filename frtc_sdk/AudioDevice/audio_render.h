#ifndef AUDIO_RENDER_H_
#define AUDIO_RENDER_H_

#include <MMDeviceApi.h>
#include <atlcomcli.h>
#include <vector>
#include <memory>
#include "../auto_lock.h"
#include "../frtc_typedef.h"

static const int kRenderDeviceGuidLen = 40;
static const int kMaxRenderDeviceNameLen = 255;

class AudioRender {
public:
    AudioRender();
    ~AudioRender();

    void StartRender();
    void StopRender();
    void SetDeviceId(const GUID& id);
    void GetDeviceId(LPGUID id);
    void ResetDevice(const GUID& id);
    void SetAudioSource(IAudioDataSource* audio_source);

    HRESULT Init(TCHAR* device_guid);
    HRESULT UpdateDeviceList(bool set_default = true);
    std::vector <DevInfo> GetDeviceList();
    HRESULT GetDefaultDeviceId(std::wstring& id);
    HRESULT SyncWithOS();
    bool CheckDeviceExist();

private:
    static BOOL CALLBACK EnumerateDevice(LPGUID device_guid, 
                                         LPCTSTR device_desc, 
                                         LPCTSTR drv_name, 
                                         LPVOID context);

    static DWORD WINAPI RenderThreadFunc(LPVOID context);

    DWORD RenderFunc(); 
    bool GetAudioData(LPVOID buff, DWORD len, DWORD sample_rate);
    HRESULT CheckFormat(bool exclusive_mode);

private:
    GUID _device_id = {0};
    TCHAR _device_guid[kRenderDeviceGuidLen] = {0};
    UINT _device_count; 
    UINT _device_index;
    TCHAR _device_name[kMaxRenderDeviceNameLen+1] = {0};
    std::vector<DevInfo> _device_list;
    bool _exclusive_mode;

    CComPtr<IMMDevice> _render_device;
    IAudioDataSource* _audio_source;
    UINT32 _sample_rate;
    int _channel_count;
    std::unique_ptr<short[]> _audio_buf;
    std::unique_ptr<short[]> _data_buf;
    int _buf_size_in_samples;

    HANDLE _render_thread;
    bool _quit_thread;
    PWAVEFORMATEX _wave_format_ex;
    CritSec _lock;
};

#endif

