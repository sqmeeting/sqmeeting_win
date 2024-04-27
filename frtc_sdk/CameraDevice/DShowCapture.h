#pragma once

#include "FRTCRenderFilter.h"
#include "../frtc_typedef.h"

#define VIDEO_QUALITY_PREFERENCE UINT
#define VIDEO_QUALITY_MOTION 0
#define VIDEO_QUALITY_SHARPNESS 1

#define MAX_PEOPLE_VIDEO_WIDTH	1920    
#define MAX_PEOPLE_VIDEO_HEIGHT	1080
#define MAX_PEOPLE_VIDEO_SIZE	(MAX_PEOPLE_VIDEO_WIDTH * MAX_PEOPLE_VIDEO_HEIGHT * 15/10)
#define MIN_PEOPLE_VIDEO_SIZE   ((MAX_PEOPLE_VIDEO_WIDTH * MAX_PEOPLE_VIDEO_HEIGHT) / 9 )

/* for now, below is default value macro */
#define	WIN_VIDEO_INPUT_DEAULT_FRAMERATE	30
#define WIN_VIDEO_INPUT_NONCAMERA_FRAMERATE 5
#define	NO_CAMERA_DATA_WIDTH    1280
#define	NO_CAMERA_DATA_HEIGHT   720

typedef struct _VIDEO_CAPS_STRUCT
{
    UINT	width;
    UINT	height;
    INT		framerate;
} VIDEO_CAPS_STRUCT, *PVIDEO_CAPS_STRUCT;


#define CAPTURE_NAME_MAX_LEN                 32
typedef struct _CAPTURE_NAME
{
    char name[CAPTURE_NAME_MAX_LEN +1];
} CAPTURE_NAME;

typedef enum _CAPTURE_STATE
{
    RUNNING = 0x00000001,
    PAUSED = 0x00000002,
    STOPPED = 0x00000004,
    PAUSED_TO_RUNNING = 0x00000008,
    CLOSING = 0x00000010,
    CLOSED = 0x00000020,
    ABOUT_TO_RUN = 0x00000040,

	NOT_RUNNING = (PAUSED | STOPPED | PAUSED_TO_RUNNING | CLOSING | CLOSED | ABOUT_TO_RUN),
} CAPTURE_STATE;

typedef struct _CAMERA_CAPTURE_STATE
{
    CAPTURE_NAME _capture_name;
    volatile CAPTURE_STATE _state;
} CAMERA_CAPTURE_STATE  ;

typedef struct _MediaTypeParam
{
	int width;
	int height;
	int framerate;
	GUID subType;
	int index;
} MediaTypeParam, *PMediaTypeParam;

typedef enum _CAMERA_STATUS
{
    NO_CAMERA,
    CAMERA_STARTED,
	CAMERA_STOPPED
}CAMERA_STATUS;

typedef enum _FILTERGRAPH_STATUS
{
    NOT_INITIALIZED,   
	DEVICE_CHANGED, 
    NOT_CONNECTED,   
    INITIALIZED,
}FILTERGRAPH_STATUS;

typedef struct __VIDEO_FILTER_GRAPH_STRUCT
{
    CMediaType *pMtFromDevice;
    IBaseFilter *pCapFilter;
    IPin *pCapPin;
    IAMStreamConfig *pCapConfig;
    FRTCRenderFilter *pRenderFilter;
    IPin *pRenderPin;
    IGraphBuilder *pGBuilder;
    IMediaControl *pMediaControl;
}VIDEO_FILTER_GRAPH_STRUCT, *PVIDEO_FILTER_GRAPH_STRUCT;

const IID MEDIASUBTYPE_I420 = { 0x30323449, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71 };

class FrtcManager;
class DShowCapture
{
public:
    int set_camera_device(const video_device *pDev);

    std::wstring get_current_camera_device_id();

    int start_preview();

    int stop_preview ();

    const PVIDEO_CAPS_STRUCT get_video_capabilities();
	const PVIDEO_CAPS_STRUCT get_cpu_capabilities();
    void set_video_capabilities(const VIDEO_CAPS_STRUCT& newCap);
	void set_cpu_level(int cpulevel);

    DShowCapture(void);
    ~DShowCapture(void);

    BITMAPINFO info_;

private:
	int _cpu_level;
    VIDEO_QUALITY_PREFERENCE        _camera_preference;
    CAMERA_CAPTURE_STATE	        _capture_state;
    VIDEO_FILTER_GRAPH_STRUCT       _video_filter_graph;
    VIDEO_CAPS_STRUCT	            _video_capabilities;
    VIDEO_CAPS_STRUCT               _current_video_capability;
	VIDEO_CAPS_STRUCT               _cpu_capabilities;
    video_device                    *pDevice;
    FILTERGRAPH_STATUS              _filter_graph_status;
    unsigned char                   *_jpg_data_ptr;
    unsigned int                    _frame_count;

	void set_capture_cap_by_cpu_level();

    static void release_video_device(video_device * dev);
    static video_device * create_video_device();
    static video_device * copy_video_device(video_device *pDesDevice, const video_device * pSrcDevice);

    void set_filter_status(FILTERGRAPH_STATUS status);
    int load_capture_config();
    int init_capture_graph_builder();
    int find_pin_by_index(IBaseFilter *pFilter, IPin **ppPin, int iPinNbr);
    int FindPin(IBaseFilter *pFilter, const char *szName, IPin **ppPin,int iDefaultPin);
    void dump_camera_media_type();
    int search_media_subtype_ratio_first();
    int search_media_subtype_sameness_first();
    void get_media_subtype(std::list<MediaTypeParam *>& list, bool bIgnoreResDiff, bool bHalfFrameRate);
    bool is_microsoft_surface();
    int search_media_subtype();
    int config_media_subtype(int index);
    int set_media_subtype();
    int load_camera_capture_filter();
    void release_camera_capture_filter();
    int load_capture_filter();
    int load_render_filter();
    int connect_filter_graph();
    void release_filter_graph();
    int disconnect_video_filter_graph();
    int create_base_video_filter_graph();
    int create_video_filter_graph();
    int stop_camera_video();
    int start_camera_video();

    void set_camera_quality_preference(VIDEO_QUALITY_PREFERENCE preferred)
    {
        _camera_preference = preferred;
    }

    friend class FrtcManager;
};
