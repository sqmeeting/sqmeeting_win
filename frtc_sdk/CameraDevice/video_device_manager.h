#pragma once

#include "../frtc_typedef.h"

class VideoDeviceManager {
public:
    VideoDeviceManager();
    ~VideoDeviceManager();

    void set_preferred_video_input(const TCHAR* device_id);
    const video_device* get_preferred_video_input();
    const video_device_list& get_video_device_list(BOOL update = FALSE);

private:
    static void release_device(video_device * dev);
    static video_device* create_video_device();

    void clean_video_device_list();
    void fetch_video_device_list();

private:
    video_device_list _video_device_list;
    video_device* _video_prefer_input;
};

