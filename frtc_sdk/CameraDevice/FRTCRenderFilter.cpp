#include "../stdafx.h"

#include "FRTCRenderFilter.h"
#include <initguid.h>


// {C847AB18-E325-4CA5-AE8D-1AC8CE2800F1}
DEFINE_GUID(CLSID_FRTC_RENDER_FILTER,
    0xc847ab18, 0xe325, 0x4ca5, 0xae, 0x8d, 0x1a, 0xc8, 0xce, 0x28, 0x0, 0xf1);


FRTCRenderFilter::FRTCRenderFilter(TCHAR* pName, LPUNKNOWN pUnk, HRESULT* phr)
: CBaseRenderer(CLSID_FRTC_RENDER_FILTER, pName, pUnk, phr),
    _sample_ready_callback(0)
{
    AddRef();
}

HRESULT FRTCRenderFilter::DoRenderSample(IMediaSample* pSample) 
{
    if(_sample_ready_callback){
		unsigned char* buffer = NULL;
		pSample->GetPointer(&buffer);
        _sample_ready_callback(pSample, _sample_ready_context, &_media_type);
    }
    return S_OK;
}

HRESULT FRTCRenderFilter::SetMediaType(const CMediaType *pmt)
{
    if(!pmt)
        return E_POINTER;
    _media_type = *pmt;
    return S_OK;
}

HRESULT FRTCRenderFilter::CheckMediaType(const CMediaType* pmt)
{
    return S_OK;
}

void FRTCRenderFilter::Initialize(DShowCaptureSampleReadyCallback callback, void *context)
{
    _sample_ready_callback = callback;
    _sample_ready_context = context;
}
