#ifndef AUDIO_CAPTURE_H_
#define AUDIO_CAPTURE_H_

#include <MMDeviceApi.h>
#include <Audioclient.h>
#include <EndpointVolume.h>
#include <atlcomcli.h>
#include <Avrt.h>
#include <dsound.h>
#include <vector>
#include <memory>
#include "../frtc_typedef.h"
#include "../auto_lock.h"

enum class WaveFormatType {
    kFormatUnknown = 0,
    kFormatPCM,
    kFormatI3EFloat
};

static const int kCaptureDeviceGuidLen = 40;

class ContentAudioMonitor;

class PeopleAudioCapture {
public:
    PeopleAudioCapture();
    ~PeopleAudioCapture();

    void StartCapture();
    void StopCapture();
    void SetDeviceId(const GUID& id);
    void GetDeviceId(LPGUID id);
    void ResetDevice(const GUID& id);
    void GetDeviceGUID(TCHAR guid_str[kCaptureDeviceGuidLen]);
    std::vector<DevInfo> GetDeviceList();
    HRESULT UpdateDeviceList(bool set_default = true);
    int GetDeviceCount();
    bool CurrentDeviceExist();
    void SetAudioSink(IAudioDataSink* audio_sink);
    HRESULT GetOSDefaultDevice(std::wstring& guid_str);
    void SetDeviceShareMode(bool share_mode);
    bool IsDeviceShareMode();
    bool IsCapturing();
    HRESULT SyncWithOS();
    float GetDevicePeakValue();

    HRESULT Init(const TCHAR* device_guid);
    HRESULT ReInit(const TCHAR* device_guid);

private:
    static BOOL LoadMmThreadSupport();
    BOOL IncreaseMmThreadPri();
    static BOOL CALLBACK EnumerateDevice (LPGUID device_guid,  
                                          LPCTSTR device_desc, 
                                          LPCTSTR drv_name, 
                                          LPVOID context);
    static DWORD WINAPI CaptureThreadFunc(LPVOID context);
    DWORD CaptureFunc(); 
    
    bool ParseAudioData(char* audio_data, int frame_num, OUT float* meter);
    void PutAudioData(LPVOID buff, DWORD len, DWORD sample_rate);
    int GetAvailableSampleNum();
    bool FindAGC(LPWSTR part_name);
    IAudioAutoGainControl* SearchAGC(IPart* part, bool by_name);
    IAudioAutoGainControl* GetAGC(IMMDevice* device);
    HRESULT CheckFormat();

private:
    GUID _device_id = {0};
    TCHAR _device_guid[kCaptureDeviceGuidLen] = {0};
    UINT _device_count; 
    UINT _device_idx;
    std::string _device_name;
    std::vector<DevInfo> _device_list;
    bool _exclusive_mode;
    
    volatile bool _output_mono;
    IAudioDataSink* _audio_sink;
    std::unique_ptr<short[]> _audio_buf;
    std::unique_ptr<short[]> _data_buf;
    int _idx_in_frame;
    int _idx_in_sample;
    int _frame_size_in_sample;
    size_t _frame_size;
    int _buf_size_in_sample;
    int _sample_rate;
    int _channel_num;
    
    HANDLE _capture_thread;
    bool _quit_thread;
    HANDLE _quit_event;
    HANDLE _ready_event;

    CComPtr<IMMDevice> _capture_device;
    CComPtr<IAudioClient> _audio_client;
    CComPtr<IAudioCaptureClient> _audio_capture;
    CComPtr<IAudioMeterInformation> _audio_meter;
    CComPtr<IAudioAutoGainControl> _agc_control;
    
    PWAVEFORMATEX _wave_format_ex;
    WaveFormatType _wave_format_type;

    typedef HANDLE (WINAPI* SetMMThreadCharacteristicsFunc)(__in LPCWSTR task_name, 
                                                            __inout LPDWORD task_idx);
    typedef BOOL (WINAPI* RevertMMThreadCharacteristicsFunc)(__in HANDLE avrt_thread);
    typedef BOOL (WINAPI* SetMMThreadPriFunc)(__in HANDLE avrt_thread, 
                                              __in AVRT_PRIORITY pri);
    
    bool _mmthread_pri_increased;
    static HMODULE _avrt_dll;
    static SetMMThreadCharacteristicsFunc _set_mmthread_characteristics;
    static RevertMMThreadCharacteristicsFunc _revert_mmthread_characteristics;
    static SetMMThreadPriFunc _set_mmthread_pri;

    CritSec _capture_lock;
    CritSec _device_lock;
};

class ContentAudioCapture {
public:
    ContentAudioCapture();
    ~ContentAudioCapture();

    void StartCapture();
    void StopCapture();
    void SetDeviceId(const GUID& id);
    void GetDeviceId(LPGUID id);
    void ResetDevice(const GUID& id);
    void SetAudioSink(IAudioDataSink* audio_sink);
    void OnDefaultDeviceChanged();

    HRESULT Init(TCHAR* device_guid);

private:
    static DWORD WINAPI CaptureThreadFunc(LPVOID context);
    DWORD CaptureFunc();

    bool ProcessAudioData(CComPtr<IAudioClient>& audio_client,
                          CComPtr<IAudioCaptureClient>& audio_capture);

    void PutAudioData(LPVOID buff, DWORD len, DWORD sample_rate);
    void ResampleToOneChannel(short* out_data,
                              unsigned char* in_data,
                              int channel_num,
                              unsigned int data_size);

    HRESULT CheckFormat(bool exclusive_mode);

private:
    GUID _device_id = {0};
    TCHAR _device_guid[kCaptureDeviceGuidLen] = {0};
    UINT _device_count;
    UINT _device_idx;
    std::string _device_name;
    bool _exclusive_mode;
    int _sample_rate;
    int _channel_count;
    IAudioDataSink* _audio_sink;
    ContentAudioMonitor* _audio_monitor;
    HANDLE _capture_thread;
    bool _quit_thread;
    PWAVEFORMATEX       _wave_format_ex;
    CComPtr<IMMDevice> _capture_device;
    CComPtr<IMMDeviceEnumerator> _device_enumerator;
};

#endif
