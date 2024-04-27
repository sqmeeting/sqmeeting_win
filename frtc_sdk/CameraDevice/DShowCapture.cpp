#include "../stdafx.h"
#include "DShowCapture.h"
#include "Windowsx.h"
#include "../frtccall_manager.h"
#include "../log.h"
#include <comdef.h>
#include <WbemIdl.h>
#include <memory>


#define SAFE_RELEASE(x)\
    x->Release();\
    x = NULL

extern HINSTANCE hInst;  // in main.cpp
extern std::shared_ptr<FrtcManager> g_frtc_mgr;


static UINT COUNTER_PER_SECOND = 90000;
#define SUBMEDIA_TYPE_COUNT  4
GUID PreferList[SUBMEDIA_TYPE_COUNT] =
{
	MEDIASUBTYPE_I420,
	MEDIASUBTYPE_NV12,
	MEDIASUBTYPE_YUY2,
	MEDIASUBTYPE_RGB24
};
#define AREA_DIFFERENCE(x1,y1,x2,y2) ( x1 * y1 + x2 * y2 - 2 * min ( x1, x2 ) * min ( y1, y2 ))
#define IS_SAME_RATIO(x1,y1,x2,y2) (((x1) * (y2) == (x2) * (y1))? 1 : 0)

unsigned int SampleReadyCallback(IMediaSample* pSample, void* context, CMediaType* pmt)
{
	return g_frtc_mgr->camera_capture_callback(pSample, context, pmt);
}

static char* MediaTypeToChar(GUID guid)
{
	if (MEDIASUBTYPE_I420 == guid)
	{
		return "MEDIASUBTYPE_I420";
	}
	else if (MEDIASUBTYPE_YUY2 == guid)
	{
		return "MEDIASUBTYPE_YUY2";
	}
	else if (MEDIASUBTYPE_YUYV == guid)
	{
		return "MEDIASUBTYPE_YUYV";
	}
	else if (MEDIASUBTYPE_UYVY == guid)
	{
		return "MEDIASUBTYPE_UYVY";
	}
	else if (MEDIASUBTYPE_YVYU == guid)
	{
		return "MEDIASUBTYPE_YVYU";
	}
	else if (MEDIASUBTYPE_RGB24 == guid)
	{
		return "MEDIASUBTYPE_RGB24";
	}
	else if (MEDIASUBTYPE_RGB32 == guid)
	{
		return "MEDIASUBTYPE_RGB32";
	}
	else if (MEDIASUBTYPE_ARGB32 == guid)
	{
		return "MEDIASUBTYPE_ARGB32";
	}
	else if (MEDIASUBTYPE_NV12 == guid)
	{
		return "MEDIASUBTYPE_NV12";
	}
	else if (MEDIASUBTYPE_MJPG == guid)
	{
		return "MEDIASUBTYPE_MJPG";
	}
	else
	{
		return "MEDIASUBTYPE_UNKOWN";
	}
}


DShowCapture::DShowCapture() :
	pDevice(NULL),
	_camera_preference(VIDEO_QUALITY_MOTION)
{
	memset(&_video_filter_graph, 0, sizeof(_video_filter_graph));
	memset(&_video_capabilities, 0, sizeof(_video_capabilities));
	memset(&_current_video_capability, 0, sizeof(_current_video_capability));
	memset(&_filter_graph_status, 0, sizeof(_filter_graph_status));

	strcpy_s(_capture_state._capture_name.name, "local_camera");
	_cpu_capabilities.width = _current_video_capability.width = _video_capabilities.width = 1920;
	_cpu_capabilities.height = _current_video_capability.height = _video_capabilities.height = 1080;
	_cpu_capabilities.framerate = _current_video_capability.framerate = _video_capabilities.framerate = 30;
	_cpu_level = 0;
}

DShowCapture::~DShowCapture(void)
{
}

void DShowCapture::release_video_device(video_device* dev)
{
	if (!dev)
	{
		return;
	}

	if (dev->id)
	{
		free(dev->id);
		dev->id = NULL;
	}

	if (dev->name)
	{
		free(dev->name);
		dev->name = NULL;
	}

	free(dev);
}

video_device* DShowCapture::create_video_device()
{
	video_device* dev;
	dev = (video_device*)malloc(sizeof(video_device));
	memset(dev, 0, sizeof(video_device));

	return dev;
}

video_device* DShowCapture::copy_video_device(video_device* pDesDevice, const video_device* pSrcDevice)
{
	if (!pSrcDevice)
	{
		return NULL;
	}
	release_video_device(pDesDevice);

	pDesDevice = create_video_device();
	if (!pSrcDevice->id)
	{
		ErrorLog("copy_video_device::Empty SrcDevice ID");
		return NULL;
	}
	size_t idlen = (wcslen(pSrcDevice->id) + 1) * sizeof(TCHAR);
	DebugLog("copy video device, source device id length=%d", idlen);

	pDesDevice->id = (TCHAR*)malloc(idlen);
	memset(pDesDevice->id, 0, idlen);
	memcpy(pDesDevice->id, pSrcDevice->id, idlen);

	size_t namelen = (wcslen(pSrcDevice->name) + 1) * sizeof(TCHAR);
	pDesDevice->name = (TCHAR*)malloc(namelen);
	memset(pDesDevice->name, 0, namelen);
	memcpy(pDesDevice->name, pSrcDevice->name, namelen);

	pDesDevice->inputIndex = pSrcDevice->inputIndex;

	return pDesDevice;
}

void DShowCapture::set_filter_status(FILTERGRAPH_STATUS status)
{
	if (_filter_graph_status > status)
	{
		_filter_graph_status = status;
	}
}

int DShowCapture::load_capture_config()
{
	ICaptureGraphBuilder2* pCapBuilder2;
	HRESULT hr = CoCreateInstance(CLSID_CaptureGraphBuilder2, NULL, CLSCTX_INPROC_SERVER, IID_ICaptureGraphBuilder2, (void**)&pCapBuilder2);
	if (SUCCEEDED(hr))
	{
		hr = pCapBuilder2->SetFiltergraph(_video_filter_graph.pGBuilder);
	}
	else
	{
		return S_FALSE;
	}
	if (SUCCEEDED(hr))
	{
		hr = pCapBuilder2->FindInterface(&PIN_CATEGORY_CAPTURE, &MEDIATYPE_Interleaved, _video_filter_graph.pCapFilter, IID_IAMStreamConfig, (void**)&_video_filter_graph.pCapConfig);
		if (FAILED(hr))
		{
			hr = pCapBuilder2->FindInterface(&PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video, _video_filter_graph.pCapFilter, IID_IAMStreamConfig, (void**)&_video_filter_graph.pCapConfig);
			if (FAILED(hr))
			{
				pCapBuilder2->Release();
				return S_FALSE;
			}
		}
	}
	pCapBuilder2->Release();
	return S_OK;
}

int DShowCapture::init_capture_graph_builder()
{
	HRESULT hr = CoCreateInstance(CLSID_FilterGraph, 
                                  0, 
                                  CLSCTX_INPROC_SERVER, 
                                  IID_IGraphBuilder, 
                                  (void**)&_video_filter_graph.pGBuilder);

	if (SUCCEEDED(hr))
	{
		hr = _video_filter_graph.pGBuilder->QueryInterface(IID_IMediaControl, 
                                    (void**)&_video_filter_graph.pMediaControl);
	}

	if (SUCCEEDED(hr))
	{
		return S_OK;
	}

	ErrorLog("init_capture_graph_builder failed, hr=0x%x", hr);

	if (_video_filter_graph.pMediaControl)
	{
		SAFE_RELEASE(_video_filter_graph.pMediaControl);
	}

	if (_video_filter_graph.pGBuilder)
	{
		SAFE_RELEASE(_video_filter_graph.pGBuilder);
	}

	return S_FALSE;
}

int DShowCapture::find_pin_by_index(IBaseFilter* pFilter, IPin** ppPin, int iPinNbr)
{
	IEnumPins* pEnumPins = NULL;
	if (FAILED(pFilter->EnumPins(&pEnumPins)))
	{
		return S_FALSE;
	}

	IPin* pPin = NULL;
	while (pEnumPins->Next(1, &pPin, NULL) == S_OK)
	{
		if (iPinNbr-- == 0)
		{
			*ppPin = pPin;
			pEnumPins->Release();
			return S_OK;
		}

		pPin->Release();
	}
	pEnumPins->Release();
	return S_FALSE;
}

int DShowCapture::FindPin(IBaseFilter* pFilter, const char* szName, IPin** ppPin, int iDefaultPin)
{
	*ppPin = NULL;
	IEnumPins* pEnumPins = NULL;
	if (FAILED(pFilter->EnumPins(&pEnumPins)))
	{
		return S_FALSE;
	}
	WCHAR szwName[256];
	if (MultiByteToWideChar(CP_ACP, 0, szName, -1, szwName, sizeof(szwName)) == 0)
	{
		pEnumPins->Release();
		return S_FALSE;
	}
	IPin* pPin = NULL;
	while (pEnumPins->Next(1, &pPin, NULL) == S_OK)
	{
		PIN_INFO PinInfo;
		if (FAILED(pPin->QueryPinInfo(&PinInfo)))
		{
			pPin->Release();
			pEnumPins->Release();
			return S_FALSE;
		}

		if (PinInfo.pFilter != NULL)
		{
			PinInfo.pFilter->Release();
		}

		if (wcsstr(PinInfo.achName, szwName) != NULL)
		{
			*ppPin = pPin;
			pEnumPins->Release();
			return S_OK;
		}
		pPin->Release();
	}
	pEnumPins->Release();

	if (iDefaultPin != -1)
	{
		return find_pin_by_index(pFilter, ppPin, iDefaultPin);
	}
	return S_FALSE;
}

void DShowCapture::dump_camera_media_type()
{
	HRESULT hr;
	VIDEO_STREAM_CONFIG_CAPS scc;
	INT iCount = 0, iSize = 0;
	AM_MEDIA_TYPE* pmtConfig;
	hr = _video_filter_graph.pCapConfig->GetNumberOfCapabilities(&iCount, &iSize);
	for (int iFormat = 0; iFormat < iCount; iFormat++)
	{
		hr = _video_filter_graph.pCapConfig->GetStreamCaps(iFormat, &pmtConfig, (BYTE*)&scc);
		if (SUCCEEDED(hr) && pmtConfig->pbFormat != 0)
		{
			VIDEOINFO* pvi = (VIDEOINFO*)pmtConfig->pbFormat;

			InfoLog("get media, name=%s, subtype=%s(%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x), "
                    "width=%d, height=%d, FPS=%d", 
                     _capture_state._capture_name.name, 
                     MediaTypeToChar(pmtConfig->subtype), 
                     pmtConfig->subtype.Data1, pmtConfig->subtype.Data2, 
                     pmtConfig->subtype.Data3, pmtConfig->subtype.Data4[0], 
                     pmtConfig->subtype.Data4[1], pmtConfig->subtype.Data4[2], 
                     pmtConfig->subtype.Data4[3], pmtConfig->subtype.Data4[4], 
                     pmtConfig->subtype.Data4[5], pmtConfig->subtype.Data4[6], 
                     pmtConfig->subtype.Data4[7], pvi->bmiHeader.biWidth, 
                     abs(pvi->bmiHeader.biHeight), 
                     (pvi->AvgTimePerFrame == 0) ? 30 : 10000000 / pvi->AvgTimePerFrame);

			DeleteMediaType(pmtConfig);
		}
	}
}

int DShowCapture::search_media_subtype_ratio_first()
{
	HRESULT hr;
	INT iCount = 0, iSize = 0;
	hr = _video_filter_graph.pCapConfig->GetNumberOfCapabilities(&iCount, &iSize);

	INT BestIndex = -1;
	INT MinDiff = INT_MAX;
	INT Diff = -1;
	INT MAX_SIZE = 1280 * 720;
	INT bestFramerate = 0;
	INT bestWidth = 0;
	INT bestHeight = 0;

	if (SUCCEEDED(hr) && iSize == sizeof(VIDEO_STREAM_CONFIG_CAPS))
	{
		VIDEO_STREAM_CONFIG_CAPS scc;
		AM_MEDIA_TYPE* pmtConfig;
		for (int index = 0; index < SUBMEDIA_TYPE_COUNT; index++)
		{
			for (int iFormat = 0; iFormat < iCount; iFormat++)
			{
				hr = _video_filter_graph.pCapConfig->GetStreamCaps(iFormat, &pmtConfig, (BYTE*)&scc);
				if (SUCCEEDED(hr) && pmtConfig->pbFormat != 0)
				{
					VIDEOINFO* pvi = (VIDEOINFO*)pmtConfig->pbFormat;
					UINT uWidth = abs(pvi->bmiHeader.biWidth);
					UINT uHeight = abs(pvi->bmiHeader.biHeight);
					UINT curframerate = (pvi->AvgTimePerFrame == 0) ? 30 : ((UINT)(10000000 / pvi->AvgTimePerFrame));
					if ((MEDIATYPE_Video == pmtConfig->majortype) && (uWidth % 16 == 0) && (uHeight % 16 == 0))//&&(PreferList[index] == pmtConfig->subtype)
					{
						if (0 == uWidth || 0 == uHeight || MAX_SIZE < 0 || (uWidth * uHeight > (UINT)MAX_SIZE))
						{
						}
						else if (_video_capabilities.width == uWidth && _video_capabilities.height == uHeight && ((bestFramerate < 0) || ((UINT)bestFramerate < curframerate)))
						{
							bestFramerate = curframerate;
							bestWidth = uWidth;
							bestHeight = uHeight;
							BestIndex = iFormat;
							Diff = 0;
						}
						else if (uWidth * uHeight <= _video_capabilities.height * _video_capabilities.width * 2 && 3 * uWidth * uHeight >= _video_capabilities.height * _video_capabilities.width)
						{
							Diff = AREA_DIFFERENCE(_video_capabilities.width, _video_capabilities.height, uWidth, uHeight);
							if (bestFramerate < 0 || ((UINT)bestFramerate) < curframerate || (((UINT)bestFramerate) == curframerate && Diff < MinDiff
								))
							{
								bestFramerate = curframerate;
								bestWidth = uWidth;
								bestHeight = uHeight;
								MinDiff = Diff;
								BestIndex = iFormat;
							}
						}
					}

					DeleteMediaType(pmtConfig);
				}
			}
		}
	}
	return BestIndex;
}

int DShowCapture::search_media_subtype_sameness_first()
{
	HRESULT hr;
	INT iCount = 0, iSize = 0;
	hr = _video_filter_graph.pCapConfig->GetNumberOfCapabilities(&iCount, &iSize);

	INT MinDiff = INT_MAX;
	INT Diff = -1;
	INT BestIndex = -1;
	UINT MAX_SIZE = 1280 * 720;

	if (SUCCEEDED(hr) && iSize == sizeof(VIDEO_STREAM_CONFIG_CAPS))
	{
		VIDEO_STREAM_CONFIG_CAPS scc;
		AM_MEDIA_TYPE* pmtConfig;
		for (int index = 0; index < SUBMEDIA_TYPE_COUNT; index++)
		{
			for (int iFormat = 0; iFormat < iCount; iFormat++)
			{
				hr = _video_filter_graph.pCapConfig->GetStreamCaps(iFormat, &pmtConfig, (BYTE*)&scc);
				if (SUCCEEDED(hr) && pmtConfig->pbFormat != 0)
				{
					if ((MEDIATYPE_Video == pmtConfig->majortype) && (PreferList[index] == pmtConfig->subtype))
					{
						VIDEOINFO* pvi = (VIDEOINFO*)pmtConfig->pbFormat;
						UINT uWidth = abs(pvi->bmiHeader.biWidth);
						UINT uHeight = abs(pvi->bmiHeader.biHeight);
						if (0 == uWidth || 0 == uHeight || uWidth * uHeight > MAX_SIZE)
						{

						}
						else
						{
							Diff = AREA_DIFFERENCE(_video_capabilities.width, _video_capabilities.height, uWidth, uHeight);
							if (Diff < MinDiff)
							{
								MinDiff = Diff;
								BestIndex = iFormat;
							}
						}
					}
					DeleteMediaType(pmtConfig);
				}
			}
			if (-1 != BestIndex)
			{
				return BestIndex;
			}
		}
	}
	return BestIndex;
}

void DShowCapture::get_media_subtype(std::list<MediaTypeParam*>& mtList, bool bIgnoreResDiff, bool bHalfFrameRate)
{
	HRESULT hr;
	INT iCount = 0, iSize = 0;
	UINT MAX_SIZE = MAX_PEOPLE_VIDEO_WIDTH * MAX_PEOPLE_VIDEO_HEIGHT;
	if (!_video_filter_graph.pCapConfig)
	{
		return;
	}

	hr = _video_filter_graph.pCapConfig->GetNumberOfCapabilities(&iCount, &iSize);
	if (SUCCEEDED(hr) && iSize == sizeof(VIDEO_STREAM_CONFIG_CAPS))
	{
		VIDEO_STREAM_CONFIG_CAPS scc;
		AM_MEDIA_TYPE* pmtConfig;
		for (int iFormat = 0; iFormat < iCount; iFormat++)
		{
			hr = _video_filter_graph.pCapConfig->GetStreamCaps(iFormat, &pmtConfig, (BYTE*)&scc);
			if (SUCCEEDED(hr) && pmtConfig->pbFormat != 0)
			{
				VIDEOINFO* pvi = (VIDEOINFO*)pmtConfig->pbFormat;
				UINT uWidth = abs(pvi->bmiHeader.biWidth);
				UINT uHeight = abs(pvi->bmiHeader.biHeight);
				UINT curframerate = (pvi->AvgTimePerFrame == 0) ? 30 : ((UINT)(10000000 / pvi->AvgTimePerFrame));
				if ((MEDIATYPE_Video == pmtConfig->majortype))
				{
                    DebugLog("capture device caps, index=%d, width=%d, height=%d, "
                             "framerate=%d, type=%s(%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x)",
                              iFormat, uWidth, uHeight, curframerate,
                              MediaTypeToChar(pmtConfig->subtype),
                              pmtConfig->subtype.Data1,
                              pmtConfig->subtype.Data2,
                              pmtConfig->subtype.Data3,
                              pmtConfig->subtype.Data4[0],
                              pmtConfig->subtype.Data4[1],
                              pmtConfig->subtype.Data4[2],
                              pmtConfig->subtype.Data4[3],
                              pmtConfig->subtype.Data4[4],
                              pmtConfig->subtype.Data4[5],
                              pmtConfig->subtype.Data4[6],
                              pmtConfig->subtype.Data4[7]);
					
                    if (0 == uWidth || 0 == uHeight || uWidth * uHeight > MAX_SIZE)
					{
						continue;
					}

					if (bIgnoreResDiff || ((uWidth % 4 == 0) && (uHeight % 4 == 0) && !bHalfFrameRate) || ((uWidth % 16 == 0) && (uHeight % 16 == 0) && bHalfFrameRate))
					{
						BOOL bCondition = ((_video_capabilities.framerate < 0) || (curframerate * 24 >= ((UINT)_video_capabilities.framerate) * 8)) &&
							(uWidth * uHeight <= _video_capabilities.height * _video_capabilities.width * 4) && (uWidth * uHeight >= MIN_PEOPLE_VIDEO_SIZE);

						if (bHalfFrameRate)
						{
							bCondition = ((_video_capabilities.framerate < 0) || (curframerate * 48 >= ((UINT)_video_capabilities.framerate) * 8)) &&
								(uWidth * uHeight <= _video_capabilities.height * _video_capabilities.width * 4) && (4 * uWidth * uHeight >= _video_capabilities.height * _video_capabilities.width);
						}
						else if (bIgnoreResDiff)
						{
							bCondition = TRUE;
						}

						if (bCondition)
						{
							MediaTypeParam* pParam = (MediaTypeParam*)malloc(sizeof(MediaTypeParam));
							pParam->width = uWidth;
							pParam->height = uHeight;
							pParam->framerate = curframerate;
							pParam->subType = pmtConfig->subtype;
							pParam->index = iFormat;
							mtList.push_back(pParam);
						}
						else
						{
                            InfoLog("drop media type index=%d, bHalfFrameRate=%d, "
                                    "bIgnoreResDiff=%d, width=%d, height=%d, "
                                    "framerate=%d",
                                     iFormat, bHalfFrameRate, bIgnoreResDiff,
                                     uWidth, uHeight, curframerate);
						}
					}
				}

				DeleteMediaType(pmtConfig);
			}
		}
	}
}

bool DShowCapture::is_microsoft_surface()
{
	bool fRet = FALSE;
	IWbemLocator* pLoc = NULL;

	HRESULT hRes = ::CoCreateInstance(CLSID_WbemLocator, NULL, CLSCTX_INPROC_SERVER, IID_IWbemLocator, (LPVOID*)&pLoc);
	if (FAILED(hRes)) {
		return fRet;
	}

	IWbemServices* pSvc = 0;

	hRes = pLoc->ConnectServer(

		_bstr_t(L"ROOT\\CIMV2"), // WMI namespace
		NULL,                    // User name
		NULL,                    // User password
		0,                       // Locale
		NULL,                    // Security flags                 
		0,                       // Authority       
		0,                       // Context object
		&pSvc                    // IWbemServices proxy
	);

	if (FAILED(hRes)) {
		pLoc->Release();
		return fRet;
	}


	hRes = CoSetProxyBlanket(

		pSvc,                         // the proxy to set
		RPC_C_AUTHN_WINNT,            // authentication service
		RPC_C_AUTHZ_NONE,             // authorization service
		NULL,                         // Server principal name
		RPC_C_AUTHN_LEVEL_CALL,       // authentication level
		RPC_C_IMP_LEVEL_IMPERSONATE,  // impersonation level
		NULL,                         // client identity 
		EOAC_NONE                     // proxy capabilities     
	);

	if (FAILED(hRes))
	{
		pSvc->Release();
		pLoc->Release();
		return fRet;               // Program has failed.
	}

	IEnumWbemClassObject* pEnumerator = NULL;
	hRes = pSvc->ExecQuery(
		bstr_t("WQL"),
		bstr_t("SELECT * FROM Win32_BaseBoard"),
		WBEM_FLAG_FORWARD_ONLY | WBEM_FLAG_RETURN_IMMEDIATELY,
		NULL,
		&pEnumerator);

	if (FAILED(hRes))
	{
		pSvc->Release();
		pLoc->Release();
		return fRet;               // Program has failed.
	}


	IWbemClassObject* iWbemObj = NULL;
	ULONG uReturned = 0;
	pEnumerator->Next(WBEM_INFINITE, 1, &iWbemObj, &uReturned);
	if (0 != uReturned)
	{
		VARIANT vtProp;
		iWbemObj->Get(L"Product", 0, &vtProp, NULL, NULL);
		if (!lstrcmpW(vtProp.bstrVal, L"Surface Pro 4")
			|| !lstrcmpW(vtProp.bstrVal, L"Surface Book")
			|| !lstrcmpW(vtProp.bstrVal, L"WN1003")
			|| !lstrcmpW(vtProp.bstrVal, L"Cherry Trail CR")
			|| !lstrcmpW(vtProp.bstrVal, L"SM-W728NZKAKOO")) {
			hRes = TRUE;
		}

		VariantClear(&vtProp);
		iWbemObj->Release();
	}
	pSvc->Release();
	pLoc->Release();
	pEnumerator->Release();

	return hRes;
}

int DShowCapture::search_media_subtype()
{
	if (!pDevice)
	{
		return -1;
	}

	static int selectIndex = -1;
	BOOL bSurforce4OrBook = FALSE;
	BOOL bInteCamera = FALSE;
	BOOL isHuddleCamHD = FALSE;
	BOOL bCameraSupportMJPEG = FALSE;
	static int nHuddleCamHD_Idx = -1;

	std::list<MediaTypeParam*> mtList;

	get_media_subtype(mtList, FALSE, FALSE);

	bSurforce4OrBook = is_microsoft_surface();
	bInteCamera = (!_tcscmp(pDevice->name, _T("Microsoft Camera Front")) ||
		!_tcscmp(pDevice->name, _T("Microsoft Camera Rear")) ||
		!_tcscmp(pDevice->name, _T("OV2680")) ||
		!_tcscmp(pDevice->name, _T("OV2680 Rear")) ||
		!_tcscmp(pDevice->name, _T("GC2235Front")) ||
		!_tcscmp(pDevice->name, _T("GC2235Rear")) ||
		!_tcscmp(pDevice->name, _T("Samsung Camera Front")) ||
		!_tcscmp(pDevice->name, _T("Samsung Camera Rear"))
		);

	if (mtList.empty())
	{
		get_media_subtype(mtList, FALSE, TRUE);

		if (mtList.empty())
		{
			get_media_subtype(mtList, TRUE, FALSE);
		}

		if (mtList.empty() == NULL)
		{
		}
	}

	int BestIndex = -1;
	INT MinDiff = INT_MAX;
	INT Diff = -1;
	INT FrameDiff = -1;
	INT bestFrameSR = 0;
	INT bestFrameOtherW = 0;
	INT bestFrameOther = 0;
	INT MinDiffSameRatio = INT_MAX;
	INT MinFrameDiffSameRatio = INT_MAX;
	INT MinDiffOtherW = INT_MAX;
	INT MinDiffOther = INT_MAX;
	INT SameRitio = 0;
	INT bestResolutionH = -1;
	INT BestIndexSame = -1;
	INT BestIndexSameRatio = -1;
	INT BestIndexSameW = -1;
	INT BestIndexOther = -1;
	int index = 0;

	isHuddleCamHD = !_tcscmp(pDevice->name, _T("HuddleCamHD"));
	for (index = 0; index < SUBMEDIA_TYPE_COUNT; index++)
	{
		if (MEDIASUBTYPE_MJPG == PreferList[index])
		{
			if (!bCameraSupportMJPEG)
			{
				if (BestIndexSameRatio != -1 || BestIndexSameW != -1 || BestIndexOther != -1)
				{
					break;
				}
			}
			else
			{
			}

			if (isHuddleCamHD && nHuddleCamHD_Idx > -1) {
				return nHuddleCamHD_Idx;
			}
		}
		else if (isHuddleCamHD) {
			continue;
		}

		std::list<MediaTypeParam*>::iterator iter = mtList.begin();
		for (; iter != mtList.end(); iter++)
		{
			MediaTypeParam* pParam = (*iter);
			SameRitio = IS_SAME_RATIO(_video_capabilities.width, _video_capabilities.height, ((UINT)pParam->width), ((UINT)pParam->height));

			if (pParam->subType == PreferList[index] && pParam->width <= MAX_PEOPLE_VIDEO_WIDTH)
			{
				Diff = AREA_DIFFERENCE(_video_capabilities.width, _video_capabilities.height, ((UINT)pParam->width), ((UINT)pParam->height));
				FrameDiff = _video_capabilities.framerate >= pParam->framerate ? (_video_capabilities.framerate - pParam->framerate) : 0;

				if (selectIndex > -1 && bSurforce4OrBook && bInteCamera)
				{
					DebugLog("selectIndex is: %d", selectIndex);
					return selectIndex;
				}

                DebugLog("video capabilities. width=%d, height=%d, framerate=%d",
                          _video_capabilities.width, _video_capabilities.height,
                          _video_capabilities.framerate);

                DebugLog("media type params, index=%d, width=%d, height=%d, framerate=%d",
                          pParam->index, pParam->width, pParam->height,
                          pParam->framerate);

				if (pParam->width == 640 && pParam->height == 360 && pParam->framerate == 30)
				{
                    DebugLog("360p30, SameRitio=%d, Diff=%d, MinDiffSameRatio=%d, "
                             "bestFrameSR=%d",
                              SameRitio, Diff, MinDiffSameRatio, bestFrameSR);
				}

				if (_video_capabilities.width == pParam->width
					&& _video_capabilities.height == pParam->height
					&& pParam->framerate == _video_capabilities.framerate)
				{
					BestIndexSame = pParam->index;
					break;
				}
				else if (SameRitio
					&& (Diff < MinDiffSameRatio || FrameDiff < MinFrameDiffSameRatio))
				{
					MinDiffSameRatio = min(MinDiffSameRatio, Diff);
					MinFrameDiffSameRatio = min(MinFrameDiffSameRatio, FrameDiff);
					if (_camera_preference == VIDEO_QUALITY_MOTION)
					{
						if (bestFrameSR <= pParam->framerate || pParam->framerate >= _video_capabilities.framerate)
						{
							BestIndexSameRatio = pParam->index;
							bestFrameSR = pParam->framerate;
						}
					}
					else
					{
						if (bestResolutionH < pParam->height && pParam->height <= _video_capabilities.height)
						{
							BestIndexSameRatio = pParam->index;
							bestResolutionH = pParam->height;
						}
					}
				}
				else if (!SameRitio
					&& _video_capabilities.width == pParam->width
					&& _video_capabilities.height < pParam->height
					&& Diff < MinDiffOtherW
					&& (bestFrameOtherW <= pParam->framerate || pParam->framerate >= _video_capabilities.framerate))
				{
					MinDiffOtherW = Diff;
					BestIndexSameW = pParam->index;
					bestFrameOtherW = pParam->framerate;
				}
				else if (!SameRitio
					&& Diff < MinDiffOther
					&& (bestFrameOther <= pParam->framerate || pParam->framerate >= _video_capabilities.framerate))
				{
					MinDiffOther = Diff;
					BestIndexOther = pParam->index;
					bestFrameOther = pParam->framerate;
				}

                DebugLog("camera preference=%s, BestIndexSameRatio=%d, BestIndexSameW=%d,"
                         "BestIndexOther=%d, MinDiffOtherW=%d, Diff=%d, bestFrameSR=%d, "
                         "bestResolutionH=%d, bestFrameOtherW=%d, bestFrameOther=%d",
                          _camera_preference == VIDEO_QUALITY_MOTION ? "framerate" : "resolution",
                          BestIndexSameRatio, BestIndexSameW, BestIndexOther,
                          MinDiffOtherW, Diff, bestFrameSR, bestResolutionH,
                          bestFrameOtherW, bestFrameOther);
			}
		}
		if (BestIndexSame != -1)
		{
			BestIndex = BestIndexSame;
			break;
		}
	}
	if (BestIndexSame == -1)
	{
		if (_camera_preference == VIDEO_QUALITY_SHARPNESS && BestIndexSameRatio > -1 && bestResolutionH > -1)
		{
			BestIndex = BestIndexSameRatio;
		}
		else if (bestFrameSR >= bestFrameOtherW && bestFrameSR >= bestFrameOther) {
			BestIndex = BestIndexSameRatio;
		}
		else if (bestFrameSR >= _video_capabilities.framerate) {
			BestIndex = BestIndexSameRatio;
		}
		else if (bestFrameSR < bestFrameOtherW) {
			if (bestFrameOtherW >= bestFrameOther) {
				BestIndex = BestIndexSameW;
			}
			else if (bestFrameOtherW >= _video_capabilities.framerate) {
				BestIndex = BestIndexSameW;
			}
			else {
				BestIndex = BestIndexOther;
			}
		}
		else {
			BestIndex = BestIndexOther;
		}

	}

	if (bSurforce4OrBook && bInteCamera) {
		selectIndex = BestIndex;
	}

	if (isHuddleCamHD) {
		nHuddleCamHD_Idx = BestIndex;
	}

	DebugLog("search media subtype, free list size=%d", mtList.size());

	std::list<MediaTypeParam*>::iterator iter = mtList.begin();
	for (; iter != mtList.end();)
	{
		free(*iter);
		iter = mtList.erase(iter);
	}

	DebugLog("search media subtype, BestIndex=%d", BestIndex);

	return BestIndex;
}

int DShowCapture::config_media_subtype(int index)
{
	VIDEO_STREAM_CONFIG_CAPS scc;
	AM_MEDIA_TYPE* pmtConfig = NULL;
	HRESULT hr = _video_filter_graph.pCapConfig->GetStreamCaps(index, &pmtConfig, (BYTE*)&scc);
	if (hr == S_OK && pmtConfig && pmtConfig->pbFormat != 0)
	{
		VIDEOINFO* pvi = (VIDEOINFO*)pmtConfig->pbFormat;
		LONGLONG avgTimePerFrame = (10000000 / WIN_VIDEO_INPUT_DEAULT_FRAMERATE < scc.MaxFrameInterval) ? 10000000 / WIN_VIDEO_INPUT_DEAULT_FRAMERATE : scc.MaxFrameInterval;
		avgTimePerFrame = (10000000 / WIN_VIDEO_INPUT_DEAULT_FRAMERATE > scc.MinFrameInterval) ? 10000000 / WIN_VIDEO_INPUT_DEAULT_FRAMERATE : scc.MinFrameInterval;
		if (avgTimePerFrame == 0)
		{
			pvi->AvgTimePerFrame = 10000000 / 30;
		}
		else
		{
			pvi->AvgTimePerFrame = avgTimePerFrame;
		}
		hr = _video_filter_graph.pCapConfig->SetFormat(pmtConfig);
		if (SUCCEEDED(hr))
		{
			if (_video_filter_graph.pMtFromDevice)
			{
				delete _video_filter_graph.pMtFromDevice;
			}
			_video_filter_graph.pMtFromDevice = new CMediaType(*pmtConfig, &hr);
			if (MEDIASUBTYPE_MJPG == pmtConfig->subtype)
			{
				int srcsize = pvi->bmiHeader.biWidth * abs(pvi->bmiHeader.biHeight) * 4;
				if (_jpg_data_ptr)
				{
					free(_jpg_data_ptr);
					_jpg_data_ptr = NULL;
				}
				_jpg_data_ptr = (BYTE*)malloc(srcsize * sizeof(BYTE));
			}

            InfoLog("config media subtype, name=%s, subtype=%s, width=%d, "
                    "height=%d, FPS=%d",
                     _capture_state._capture_name.name,
                     MediaTypeToChar(pmtConfig->subtype),
                     pvi->bmiHeader.biWidth,
                     abs(pvi->bmiHeader.biHeight),
                     10000000 / pvi->AvgTimePerFrame);
			
            _current_video_capability.width = pvi->bmiHeader.biWidth;
			_current_video_capability.height = abs(pvi->bmiHeader.biHeight);
			_current_video_capability.framerate = (10000000 / pvi->AvgTimePerFrame);
			DeleteMediaType(pmtConfig);
			return S_OK;
		}
		DeleteMediaType(pmtConfig);
	}
	_current_video_capability.width = 0;
	_current_video_capability.height = 0;
	_current_video_capability.framerate = 0;
	return S_FALSE;
}

int DShowCapture::set_media_subtype()
{
	INT BestIndex = -1;

	BestIndex = search_media_subtype();
	if (-1 != BestIndex)
	{
		return config_media_subtype(BestIndex);
	}
	else
	{
		return S_FALSE;
	}
}

int DShowCapture::load_camera_capture_filter()
{
	ICreateDevEnum* pDevEnum = NULL;
	IEnumMoniker* pEnum = NULL;
	INT ret = S_FALSE;

	HRESULT hr = CoCreateInstance(CLSID_SystemDeviceEnum, 
                                  NULL, 
                                  CLSCTX_INPROC_SERVER, 
                                  IID_ICreateDevEnum, 
                                  reinterpret_cast<void**>(&pDevEnum));
	if (FAILED(hr))
	{
		DebugLog("%s :Fail to get create ICreateDevEnum, hr=0x%x", 
                  _capture_state._capture_name.name, hr);
		goto end;
	}

	hr = pDevEnum->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, &pEnum, 0);
	if (FAILED(hr) || NULL == pEnum)
	{
		goto releaseEnum;
	}
	IMoniker* pMoniker = NULL;
	while (pEnum->Next(1, &pMoniker, NULL) == S_OK)
	{
		TCHAR* wzDisplayName = NULL;
		hr = pMoniker->GetDisplayName(NULL, NULL, &wzDisplayName);
		if (FAILED(hr))
		{
			ErrorLog("%s: GetDisplayName failed, hr=0x%x", 
                      _capture_state._capture_name.name, hr);
			pMoniker->Release();
			continue;
		}

		if (pDevice != NULL && !_tcscmp(wzDisplayName, pDevice->id))
		{
			hr = pMoniker->BindToObject(0, 0, IID_IBaseFilter, (void**)&_video_filter_graph.pCapFilter);
			if (FAILED(hr))
			{
				CoTaskMemFree(wzDisplayName);
				pMoniker->Release();
				DebugLog("%s: Can not bind device %S with hr=0x%x", 
                          _capture_state._capture_name.name, pDevice->name, hr);
				break;
			}
			hr = _video_filter_graph.pGBuilder->AddFilter(_video_filter_graph.pCapFilter, L"FRTC_Camera_Capture");
			if (FAILED(hr))
			{
				CoTaskMemFree(wzDisplayName);
				pMoniker->Release();
				break;
			}
			if (S_FALSE == FindPin(_video_filter_graph.pCapFilter, "Capture", &_video_filter_graph.pCapPin, 0))
			{
				CoTaskMemFree(wzDisplayName);
				pMoniker->Release();
				ErrorLog("%s: can not find capture pin in : %S", _capture_state._capture_name.name, pDevice->name);
				break;
			}
			if (S_OK == load_capture_config())
			{
				CoTaskMemFree(wzDisplayName);
				pMoniker->Release();
				pEnum->Release();
				pDevEnum->Release();
				return S_OK;
			}
		}
		CoTaskMemFree(wzDisplayName);
		pMoniker->Release();
	}
	pEnum->Release();


	if (_video_filter_graph.pCapConfig)
	{
		SAFE_RELEASE(_video_filter_graph.pCapConfig);
	}
	if (_video_filter_graph.pCapPin)
	{
		SAFE_RELEASE(_video_filter_graph.pCapPin);
	}
	if (_video_filter_graph.pCapFilter)
	{
		SAFE_RELEASE(_video_filter_graph.pCapFilter);
	}
releaseEnum:
	pDevEnum->Release();
end:
	return S_FALSE;
}

void DShowCapture::release_camera_capture_filter()
{
	if (_video_filter_graph.pCapPin)
	{
		SAFE_RELEASE(_video_filter_graph.pCapPin);
	}
	if (_video_filter_graph.pCapFilter)
	{
		_video_filter_graph.pGBuilder->RemoveFilter(_video_filter_graph.pCapFilter);
		SAFE_RELEASE(_video_filter_graph.pCapFilter);
	}
	if (_video_filter_graph.pCapConfig)
	{
		SAFE_RELEASE(_video_filter_graph.pCapConfig);
	}
	if (_video_filter_graph.pMtFromDevice)
	{
		delete _video_filter_graph.pMtFromDevice;
		_video_filter_graph.pMtFromDevice = NULL;
	}
}

int DShowCapture::load_capture_filter()
{
	release_camera_capture_filter();
	if (!pDevice)
	{
		return S_FALSE;
	}
	else
	{
		if (S_FALSE == load_camera_capture_filter())
		{
			return S_FALSE;
		}
		dump_camera_media_type();
	}
	return S_OK;
}

int DShowCapture::load_render_filter()
{
	InfoLog("load render filter start");
	HRESULT hr = S_OK;

	_video_filter_graph.pRenderFilter = new FRTCRenderFilter(_T("FRTC_CAMERA_Render"), NULL, &hr);
	if (SUCCEEDED(hr))
	{
		_video_filter_graph.pRenderFilter->Initialize(SampleReadyCallback, (void*)this);
		hr = _video_filter_graph.pGBuilder->AddFilter(_video_filter_graph.pRenderFilter, L"FRTC_CAMERA_Render");
	}
	if (SUCCEEDED(hr))
	{
		_video_filter_graph.pRenderPin = _video_filter_graph.pRenderFilter->GetPin(0);
		hr = (_video_filter_graph.pRenderPin == NULL) ? E_FAIL : S_OK;
	}
	if (SUCCEEDED(hr))
	{
		return S_OK;
	}

	if (_video_filter_graph.pRenderFilter)
	{
		delete _video_filter_graph.pRenderFilter;
		_video_filter_graph.pRenderFilter = NULL;
	}

	ErrorLog("load render filter failed, name=%s, hr=0x%x", 
              _capture_state._capture_name.name, hr);

	return S_FALSE;
}

int DShowCapture::connect_filter_graph()
{
	HRESULT hr = S_OK;
	if (_video_filter_graph.pCapFilter)
	{
		if (!_video_filter_graph.pMtFromDevice)
		{
			ErrorLog("can not get submediatype for device :%S", pDevice);
			return S_FALSE;
		}

		hr = _video_filter_graph.pGBuilder->ConnectDirect(_video_filter_graph.pCapPin, 
                                                          _video_filter_graph.pRenderPin, 
                                                          _video_filter_graph.pMtFromDevice);
		int count = 0;
		if (hr == VFW_E_NOT_STOPPED || hr == VFW_E_ALREADY_CONNECTED)
		{
			ErrorLog("channel %s filters is 0x%x. retuen failure", 
                      _capture_state._capture_name.name, hr);
			return S_FALSE;
		}

		while (FAILED(hr) && count++ < 2)
		{
			hr = _video_filter_graph.pGBuilder->ConnectDirect(_video_filter_graph.pCapPin, 
                                                              _video_filter_graph.pRenderPin, 
                                                              _video_filter_graph.pMtFromDevice);
			Sleep(200);
		}

		if (SUCCEEDED(hr))
		{
			IMediaFilter* pMediaFilter = 0;
			hr = _video_filter_graph.pGBuilder->QueryInterface(IID_IMediaFilter, 
                                                             (void**)&pMediaFilter);
			if (SUCCEEDED(hr) && pMediaFilter)
			{
				pMediaFilter->SetSyncSource(NULL);
				pMediaFilter->Release();
				pMediaFilter = NULL;
			}
			return S_OK;
		}
	}
	else
	{
		return S_FALSE;
	}
	ErrorLog("channel %s to ConnectDirect filters, hr=0x%x", 
              _capture_state._capture_name.name, hr);

	return S_FALSE;
}

void DShowCapture::release_filter_graph()
{
	if (_video_filter_graph.pCapPin)
	{
		SAFE_RELEASE(_video_filter_graph.pCapPin);
	}
	if (_video_filter_graph.pCapFilter)
	{
		_video_filter_graph.pGBuilder->RemoveFilter(_video_filter_graph.pCapFilter);
		SAFE_RELEASE(_video_filter_graph.pCapFilter);
	}
	if (_video_filter_graph.pCapConfig)
	{
		SAFE_RELEASE(_video_filter_graph.pCapConfig);
	}
	if (_video_filter_graph.pMediaControl)
	{
		SAFE_RELEASE(_video_filter_graph.pMediaControl);
	}
	if (_video_filter_graph.pGBuilder)
	{
		SAFE_RELEASE(_video_filter_graph.pGBuilder);
	}
	if (_video_filter_graph.pRenderFilter)
	{
		delete _video_filter_graph.pRenderFilter;
		_video_filter_graph.pRenderFilter = NULL;
	}
	if (_video_filter_graph.pMtFromDevice)
	{
		delete _video_filter_graph.pMtFromDevice;
		_video_filter_graph.pMtFromDevice = NULL;
	}
}

int DShowCapture::disconnect_video_filter_graph()
{
	if (RUNNING == _capture_state._state)
	{
		_video_filter_graph.pMediaControl->Stop();
	}
	HRESULT hr = S_OK;
	hr = _video_filter_graph.pGBuilder->Disconnect(_video_filter_graph.pRenderPin);
	if (_video_filter_graph.pCapFilter)
	{
		hr = _video_filter_graph.pGBuilder->Disconnect(_video_filter_graph.pCapPin);
	}
	else
	{
		hr = S_FALSE;
	}

	return S_OK;
}

int DShowCapture::create_base_video_filter_graph()
{
	memset(&_video_filter_graph, 0, sizeof(VIDEO_FILTER_GRAPH_STRUCT));
	if (S_FALSE == init_capture_graph_builder())
	{
		return S_FALSE;
	}
	if (S_OK == load_render_filter())
	{
		return S_OK;
	}
	if (_video_filter_graph.pMediaControl)
	{
		SAFE_RELEASE(_video_filter_graph.pMediaControl);
	}
	if (_video_filter_graph.pGBuilder)
	{
		SAFE_RELEASE(_video_filter_graph.pGBuilder);
	}
	return S_FALSE;
}

int DShowCapture::create_video_filter_graph()
{
	BOOL bSetSubMediaType = FALSE;
	DebugLog("create video filter graph, status=%d", _filter_graph_status);

	if (NOT_INITIALIZED == _filter_graph_status)
	{
		if (S_FALSE == create_base_video_filter_graph())
		{
			ErrorLog("channel %s fail to create_base_video_filter_graph", 
                      _capture_state._capture_name.name);
			return S_FALSE;
		}
		_filter_graph_status = DEVICE_CHANGED;
	}
	if (DEVICE_CHANGED == _filter_graph_status)
	{
		if (S_FALSE == load_capture_filter())
		{
			return S_FALSE;
		}
		if (pDevice)
		{
			set_media_subtype();
			bSetSubMediaType = TRUE;
		}
		_filter_graph_status = NOT_CONNECTED;
	}
	if (NOT_CONNECTED == _filter_graph_status)
	{
		UINT iRet = connect_filter_graph();
		if (iRet != S_OK)
		{
			return iRet;
		}
		_filter_graph_status = INITIALIZED;
	}
	if (pDevice)
	{
		if (FALSE == bSetSubMediaType)
		{
			ErrorLog("channel %s set_media_subtype failed", 
                      _capture_state._capture_name.name);
			set_media_subtype();
		}
	}

	return S_OK;
}

int DShowCapture::stop_camera_video()
{
	if (!_video_filter_graph.pMediaControl)
	{
		return S_OK;
	}
	OAFilterState filterStatus;
	HRESULT hr = _video_filter_graph.pMediaControl->Pause();
	hr = _video_filter_graph.pMediaControl->GetState(500, &filterStatus);
	hr = _video_filter_graph.pMediaControl->Stop();

	disconnect_video_filter_graph();
	_filter_graph_status = NOT_CONNECTED;
	if (pDevice && !_video_filter_graph.pCapFilter)
		_filter_graph_status = DEVICE_CHANGED;

	return S_OK;
}

int DShowCapture::start_camera_video()
{
	_frame_count = 0;

	INT iRet = create_video_filter_graph();

	if (S_OK != iRet)
	{
		ErrorLog("construct video input filter graph failed, name=%s", 
                  _capture_state._capture_name.name);

		return iRet;
	}

	HRESULT hr = _video_filter_graph.pMediaControl->Stop();

	hr = _video_filter_graph.pMediaControl->Run();
	if (SUCCEEDED(hr))
	{
		OAFilterState state;
		hr = _video_filter_graph.pMediaControl->GetState(100, &state);
		if (SUCCEEDED(hr))
		{
		}
		return S_OK;
	}
	else
	{
		ErrorLog("start input video filter graph failed, name=%s, hr=0x%x", 
                  _capture_state._capture_name.name, hr);

		disconnect_video_filter_graph();
		return S_FALSE;
	}
}

void DShowCapture::set_cpu_level(int cpulevel)
{
	DebugLog("set cpu level, level=%d", cpulevel);
	_cpu_level = cpulevel;
}

void DShowCapture::set_capture_cap_by_cpu_level()
{
	switch (_cpu_level)
	{
	case 1:
		_cpu_capabilities.width = _video_capabilities.width = 1920;
		_cpu_capabilities.height = _video_capabilities.height = 1080;
		_cpu_capabilities.framerate = _video_capabilities.framerate = 30;
		break;
	case 2:
		_cpu_capabilities.width = _video_capabilities.width = 1280;
		_cpu_capabilities.height = _video_capabilities.height = 720;
		_cpu_capabilities.framerate = _video_capabilities.framerate = 30;
		break;
	case 3:
		_cpu_capabilities.width = _video_capabilities.width = 960;
		_cpu_capabilities.height = _video_capabilities.height = 540;
		_cpu_capabilities.framerate = _video_capabilities.framerate = 30;
		break;
	default:
		break;
	}

    DebugLog("set video capture caps by cpu level, level=%d, width=%d, height=%d, "
             "framerate=%d",
              _cpu_level, _video_capabilities.width, _video_capabilities.height,
              _video_capabilities.framerate);
}

int DShowCapture::set_camera_device(const video_device* pDev)
{
	INT ret = S_OK;

	if (NULL != pDev)
	{
		pDevice = copy_video_device(pDevice, pDev);
		set_filter_status(DEVICE_CHANGED);
		if (CAPTURE_STATE::RUNNING == _capture_state._state)
		{
			_capture_state._state = CAPTURE_STATE::STOPPED;
			stop_camera_video();
			set_filter_status(DEVICE_CHANGED);
		}

		if (S_OK != start_camera_video())
		{
			ErrorLog("error! set video input failed");

			_capture_state._state = CAPTURE_STATE::STOPPED;
			set_filter_status(DEVICE_CHANGED);

			ret = S_FALSE;
		}
		else 
        {
			_capture_state._state = CAPTURE_STATE::RUNNING;
			set_filter_status(DEVICE_CHANGED);

			DebugLog("video capture state running");
		}
	}
	else
	{
		set_filter_status(DEVICE_CHANGED);
		if (CAPTURE_STATE::RUNNING == _capture_state._state)
		{
			_capture_state._state = CAPTURE_STATE::STOPPED;
			stop_camera_video();
			set_filter_status(DEVICE_CHANGED);
			_capture_state._state = CAPTURE_STATE::RUNNING;
		}
		release_video_device(pDevice);
		pDevice = NULL;
		if (CAPTURE_STATE::RUNNING == _capture_state._state)
		{
			start_camera_video();
		}
	}

	return ret;
}

std::wstring DShowCapture::get_current_camera_device_id()
{
	return pDevice ? pDevice->id : L"";
}

int DShowCapture::start_preview()
{
	INT ret = S_OK;

	if (CAPTURE_STATE::RUNNING == _capture_state._state)
	{
	}
	else
	{
		ret = start_camera_video();

		if (S_OK == ret)
		{
			_capture_state._state = CAPTURE_STATE::RUNNING;
		}

		if (S_FALSE == ret)
		{
			if (pDevice)
			{
				set_filter_status(DEVICE_CHANGED);
				stop_camera_video();
				ret = start_camera_video();
				if (S_OK == ret)
				{
					_capture_state._state = CAPTURE_STATE::RUNNING;
				}
			}
		}

		if (S_OK != ret)
		{
			ErrorLog("error! can not start video input channel");
			if (pDevice)
			{
				video_device* pStoredVideoDevice = pDevice;
				pDevice = NULL;
				set_filter_status(DEVICE_CHANGED);
				start_camera_video();
				_capture_state._state = CAPTURE_STATE::RUNNING;
				pDevice = pStoredVideoDevice;
				set_filter_status(DEVICE_CHANGED);
			}
			ret = S_FALSE;
		}
	}

	return ret;
}

int DShowCapture::stop_preview()
{
	INT ret = S_OK;

	if (CAPTURE_STATE::STOPPED == _capture_state._state)
	{
	}
	else
	{
		_capture_state._state = CAPTURE_STATE::STOPPED;
		ret = stop_camera_video();
	}

	return ret;
}

const PVIDEO_CAPS_STRUCT DShowCapture::get_video_capabilities()
{
	return &_current_video_capability;
}
void DShowCapture::set_video_capabilities(const VIDEO_CAPS_STRUCT& newCap)
{
	_current_video_capability.width = _video_capabilities.width = newCap.width;
	_current_video_capability.height = _video_capabilities.height = newCap.height;
	_current_video_capability.framerate = _video_capabilities.framerate = newCap.framerate;

}
const PVIDEO_CAPS_STRUCT DShowCapture::get_cpu_capabilities()
{
	return &_cpu_capabilities;
}

