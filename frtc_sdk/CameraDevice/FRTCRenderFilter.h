#ifndef DSHOW_FRTC_RENDER_FILTER
#define DSHOW_FRTC_RENDER_FILTER

#include <streams.h>
#include "mtype.h"

typedef UINT (*DShowCaptureSampleReadyCallback)(IMediaSample *pSample, void *context, CMediaType *pmt);

class FRTCRenderFilter : public CBaseRenderer
{
protected:
    DShowCaptureSampleReadyCallback _sample_ready_callback;
    void *                          _sample_ready_context;
    CMediaType                      _media_type;

    //override CBaseRenderer
protected:
	HRESULT DoRenderSample(IMediaSample* pSample);
    HRESULT CheckMediaType(const CMediaType* pmt);
    HRESULT SetMediaType(const CMediaType *pmt);
public:
	FRTCRenderFilter(TCHAR* pName, LPUNKNOWN pUnk, HRESULT* phr);
    void Initialize(DShowCaptureSampleReadyCallback callback, void *context);
};


#endif

