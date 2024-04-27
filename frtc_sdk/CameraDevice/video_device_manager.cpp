#include <streams.h>
#include "video_device_manager.h"
#include "../stdafx.h"
#include "../log.h"

VideoDeviceManager::VideoDeviceManager() 
    : _video_prefer_input(NULL) {
    fetch_video_device_list();
}

VideoDeviceManager::~VideoDeviceManager() {
    clean_video_device_list();
}

void VideoDeviceManager::set_preferred_video_input(const TCHAR* device_id) {
    if (device_id == NULL) {
        ErrorLog("set_preferred_video_input failed, device id is NULL");
        return;
    }

    if (_video_device_list.empty()) {
        fetch_video_device_list();
    }

    if (_video_device_list.empty()) {
        ErrorLog("set_preferred_video_input failed, video_device_list is empty");
        return;
    }

    video_device_list::const_iterator it = _video_device_list.begin();
    while (it != _video_device_list.end()) {
        if (wcscmp(device_id, (*it)->id) == 0) {
            _video_prefer_input = *it;
            break;
        }
        it++;
    }
}

const video_device* VideoDeviceManager::get_preferred_video_input() {
    if (_video_device_list.empty()) {
        ErrorLog("video_device_list is empty, return...");
        return NULL;
    }

    if (_video_prefer_input) {
        if (_video_prefer_input->id == NULL) {
            ErrorLog("GetVideoPreferInput failed, preferred device id is NULL");
            return NULL;
        }

        return _video_prefer_input;
    }
    else {
        video_device* def = _video_device_list.front();
        if (def == NULL) {
            ErrorLog("GetVideoPreferInput failed, first device is NULL");
        }
        else if (def->id == NULL) {
            ErrorLog("GetVideoPreferInput failed, first device id is NULL");
        }

        return def;
    }
}

const video_device_list& VideoDeviceManager::get_video_device_list(BOOL update) {
    if (_video_device_list.empty() || update == TRUE) {
        fetch_video_device_list();
    }

    return _video_device_list;
}

void VideoDeviceManager::release_device(video_device* dev) {
    if (!dev) {
        return;
    }
	
    if (dev->id) {
        free(dev->id);
		dev->id = NULL;
    }
	
    if (dev->name) {
        free(dev->name);
		dev->name = NULL;
    }
	
    free (dev);
    dev = NULL;
}

video_device * VideoDeviceManager::create_video_device() {
    video_device *dev;
    dev = (video_device *)malloc (sizeof (video_device));
    memset(dev, 0, sizeof(video_device));
	
    return dev;
}

void VideoDeviceManager::clean_video_device_list() {
    if (!_video_device_list.empty()) {
        std::list<video_device*>::iterator iter = _video_device_list.begin();
        for (; iter != _video_device_list.end();) {
            release_device(*iter);
            iter = _video_device_list.erase(iter);
        }
        _video_prefer_input = NULL;
    }
}

void VideoDeviceManager::fetch_video_device_list() {
    TCHAR* last_prefer_input = NULL;
    if (_video_prefer_input && _video_prefer_input->id) {
        size_t len = _tcslen(_video_prefer_input->id) * sizeof(TCHAR);
        last_prefer_input = new TCHAR[len + sizeof(TCHAR)];
        memset(last_prefer_input, 0, len + sizeof(TCHAR));
        memcpy(last_prefer_input, _video_prefer_input->id, len);

        DebugLog("save last preferred video input device");
    }
    clean_video_device_list();

    ICreateDevEnum* dev_enumerator = NULL;
    HRESULT ret = CoCreateInstance(CLSID_SystemDeviceEnum, 
                                   NULL, 
                                   CLSCTX_INPROC_SERVER, 
                                   IID_ICreateDevEnum, 
                                   (PVOID*)&dev_enumerator);
    if (FAILED(ret)) {
        if (last_prefer_input) {
            delete[] last_prefer_input;
        }
        return;
    }

    IEnumMoniker* enum_moniker = NULL;
    ret = dev_enumerator->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, 
                                                &enum_moniker, 0);
    if (FAILED(ret) || ret == S_FALSE) {
        if (last_prefer_input) {
            delete[] last_prefer_input;
        }
        return;
    }

    enum_moniker->Reset();

    int index = 0;
    ULONG fetched = 0;
    IMoniker* moniker = NULL;
    while (enum_moniker->Next(1, &moniker, &fetched) == S_OK) {
        video_device* dev;
        dev = create_video_device();

        TCHAR* display_name;
        ret = moniker->GetDisplayName(NULL, NULL, &display_name);
        if (FAILED(ret)) {
            release_device(dev);
            moniker->Release();
            continue;
        }

        size_t name_size = (_tcslen(display_name) + 1) * sizeof(TCHAR);
        dev->id = (TCHAR*)malloc(name_size);
        memset(dev->id, 0, name_size);
        memcpy(dev->id, display_name, name_size);

        IPropertyBag* property_bag;
        ret = moniker->BindToStorage(0, 0, IID_IPropertyBag, (void**)&property_bag);
        if (FAILED(ret)) {
            release_device(dev);
            moniker->Release();
            continue;
        }

        VARIANT var_name;
        VariantInit(&var_name);
        var_name.vt = VT_BSTR;
        ret = property_bag->Read(L"FriendlyName", &var_name, 0);
        if (FAILED(ret)) {
            release_device(dev);
            moniker->Release();
            property_bag->Release();
            continue;
        }
        
        name_size = (_tcslen(var_name.bstrVal) + 1) * sizeof(TCHAR);
        dev->name = (TCHAR*)malloc(name_size);
        memset(dev->name, 0, name_size);
        memcpy(dev->name, var_name.bstrVal, name_size);

        dev->inputIndex = index++;

        _video_device_list.push_back(dev);
        if (last_prefer_input && 
            wcscmp(dev->id, last_prefer_input)==0) {
            _video_prefer_input = dev;
            DebugLog("last prefered video device still exists, refresh");
        }

        moniker->Release();
        property_bag->Release();
    }

    enum_moniker->Release();
    enum_moniker = NULL;
    dev_enumerator->Release();
    dev_enumerator = NULL;
    if (last_prefer_input) {
        delete[] last_prefer_input;
    }
}
