#include "DXGI_DesktopDuplicator.h"
#include "auto_release_object.h"

#define WIN32_LEAN_AND_MEAN    
#define IMIN(a,b)     ((a) < (b) ? (a) : (b))

#include <windows.h>
#include <shlobj.h>
#include <shellapi.h>

#include "log.h"

DXGI_DesktopDuplicator::DXGI_DesktopDuplicator(int nDevIndex, LPCWSTR lpwDeviceName)
	: m_nDevIndex(nDevIndex),
	m_wszDeviceName{ 0 },
	m_pDevice(NULL),
	m_pImmediateContext(NULL),
	m_pGDIImage(NULL),
	m_pDestImage(NULL),
	m_pDeskDupl(NULL),
	m_hrLastDXGIErr(0)
{
	m_Mutex = CreateMutex(NULL, FALSE, NULL);
	m_DesktopWidth = m_DesktopHeight = m_nPrevWidth = m_nPrevWidth = 0;

	wcsncpy(m_wszDeviceName, lpwDeviceName, 32);
}

DXGI_DesktopDuplicator::~DXGI_DesktopDuplicator()
{
	WaitForSingleObject(m_Mutex, INFINITE);

	cleanup();
	if (m_Mutex) CloseHandle(m_Mutex);

}

void DXGI_DesktopDuplicator::cleanup()
{
	if (m_pGDIImage != NULL)
	{
		m_pGDIImage->Release();
		m_pGDIImage = NULL;
	}

	if (m_pDeskDupl != NULL) {
		m_pDeskDupl->Release();
		m_pDeskDupl = NULL;
	}

	if (m_pDestImage != NULL)
	{
		m_pDestImage->Release();
		m_pDestImage = NULL;
	}

	if (m_pImmediateContext != NULL)
	{
		m_pImmediateContext->ClearState();
		m_pImmediateContext->Flush();

		m_pImmediateContext->Release();
		m_pImmediateContext = NULL;
	}

	if (m_pDevice != NULL)
	{
		m_pDevice->Release();
		m_pDevice = NULL;
	}
}

HRESULT DXGI_DesktopDuplicator::get_dxgi_error()
{
	return m_hrLastDXGIErr;
}

bool DXGI_DesktopDuplicator::init()
{
	bool bRetVal = false;
	HRESULT hRes = E_FAIL;
	int nRes = -1;
	D3D11_TEXTURE2D_DESC textureDesc;
	D3D_FEATURE_LEVEL d3dFeatureLvl;
	IDXGIDevice* pDxgiDevice;
	IDXGIAdapter* pDxgiAdapter;
	IDXGIOutput* pDxgiOutput;
	IDXGIOutput1* pDxgiOutput1;
	DXGI_OUTPUT_DESC outputDesc;
	DXGI_OUTDUPL_DESC outputDuplDesc;

	AutoReleaseLock lock(m_Mutex);

	int nMonNum = m_nDevIndex;

	D3D_FEATURE_LEVEL featureLevels[] = {   // Feature levels supported
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
		D3D_FEATURE_LEVEL_9_1 };

	hRes = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0, featureLevels,
		ARRAYSIZE(featureLevels), D3D11_SDK_VERSION, &m_pDevice, &d3dFeatureLvl, &m_pImmediateContext);
	if (FAILED(hRes))
	{
		goto errout;
	}

	Sleep(100);

	if (m_pDevice == NULL)
	{
		goto errout;
	}

	// Get DXGI device
	hRes = m_pDevice->QueryInterface(IID_PPV_ARGS(&pDxgiDevice));
	if (FAILED(hRes))
	{
		goto errout;
	}

	// Get DXGI adapter
	hRes = pDxgiDevice->GetParent(__uuidof(IDXGIAdapter), reinterpret_cast<void**>(&pDxgiAdapter));
	if (FAILED(hRes))
	{
		goto errout;
	}

	pDxgiDevice->Release();

	// Get DXGI output of the DGXI 
	int i = 0;
	hRes = pDxgiAdapter->EnumOutputs(i, &pDxgiOutput);
	DXGI_OUTPUT_DESC desc{ 0 };
	if (pDxgiOutput != NULL)
	{
		pDxgiOutput->GetDesc(&desc);
	}
	while (hRes != DXGI_ERROR_NOT_FOUND && wcscmp(desc.DeviceName, m_wszDeviceName) != 0)
	{
		++i;
		hRes = pDxgiAdapter->EnumOutputs(i, &pDxgiOutput);
		memset(&desc, 0, sizeof(DXGI_OUTPUT_DESC));
		if (pDxgiOutput != NULL)
		{
			pDxgiOutput->GetDesc(&desc);
		}
	}
	if (FAILED(hRes))
	{
		goto errout;
	}

	pDxgiAdapter->Release();

	if (pDxgiOutput == NULL)
	{
		goto errout;
	}

	hRes = pDxgiOutput->GetDesc(&outputDesc);
	if (FAILED(hRes))
	{
		goto errout;
	}

	if (outputDesc.Rotation != DXGI_MODE_ROTATION_IDENTITY)
	{
		goto errout;
	}

	// Query interface for Output 1
	hRes = pDxgiOutput->QueryInterface(IID_PPV_ARGS(&pDxgiOutput1));
	if (FAILED(hRes))
	{
		goto errout;
	}

	pDxgiOutput->Release();

	hRes = pDxgiOutput1->DuplicateOutput(m_pDevice, &m_pDeskDupl);
	if (FAILED(hRes))
	{
		goto errout;
	}

	pDxgiOutput1->Release();

	m_pDeskDupl->GetDesc(&outputDuplDesc);

	textureDesc.Width = outputDuplDesc.ModeDesc.Width;
	textureDesc.Height = outputDuplDesc.ModeDesc.Height;
	textureDesc.Format = outputDuplDesc.ModeDesc.Format;
	textureDesc.ArraySize = 1;
	textureDesc.BindFlags = D3D11_BIND_FLAG::D3D11_BIND_RENDER_TARGET;
	textureDesc.MiscFlags = D3D11_RESOURCE_MISC_GDI_COMPATIBLE;
	textureDesc.SampleDesc.Count = 1;
	textureDesc.SampleDesc.Quality = 0;
	textureDesc.MipLevels = 1;
	textureDesc.CPUAccessFlags = 0;
	textureDesc.Usage = D3D11_USAGE_DEFAULT;
	hRes = m_pDevice->CreateTexture2D(&textureDesc, NULL, &m_pGDIImage);
	if (FAILED(hRes))
	{
		goto errout;
	}

	if (m_pGDIImage == NULL)
	{
		goto errout;
	}

	// Update descriptor to create a CPU accessible texture
	textureDesc.BindFlags = textureDesc.MiscFlags = 0;
	textureDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ | D3D11_CPU_ACCESS_WRITE;
	textureDesc.Usage = D3D11_USAGE_STAGING;
	hRes = m_pDevice->CreateTexture2D(&textureDesc, NULL, &m_pDestImage);
	if (FAILED(hRes))
	{
		goto errout;
	}

	if (m_pDestImage == NULL)
	{
		goto errout;
	}

	m_nPrevWidth = m_nPrevHeight = 0;
	m_DesktopWidth = outputDuplDesc.ModeDesc.Width;
	m_DesktopHeight = outputDuplDesc.ModeDesc.Height;
	bRetVal = true;

errout:
	if (!bRetVal)
	{
		// Ensure any outstanding handles are closed
		cleanup();
		m_hrLastDXGIErr = hRes;
	}
	return bRetVal;
}

bool DXGI_DesktopDuplicator::capture_screen(void** ppDstBuf, int* dst_width, int* dst_height)
{
	UINT crop_width, crop_height, nSubresource, nDstPitch, nSrcPitch;
	unsigned int nHeight, nAdjustWidth, nAdjustHeight;
	BITMAPINFO	 bmpInfo = { 0 };
	ID3D11Texture2D* pAcquiredDesktopImage = NULL;
	IDXGIResource* pDesktopResourceDxIf = NULL;
	IDXGISurface1* pIDXGISurface1 = NULL;
	DXGI_OUTDUPL_FRAME_INFO frameInfo;
	D3D11_MAPPED_SUBRESOURCE resource;
	CURSORINFO lCursorInfo = { 0 };
	BYTE* sptr, * dptr;
	HRESULT hRes = -1;
	int nRetryCnt = 5;

	lCursorInfo.cbSize = sizeof(lCursorInfo);


	AutoReleaseLock lock(m_Mutex);
	do
	{
		// Get new frame
		if (m_pDeskDupl)
		{

			hRes = m_pDeskDupl->AcquireNextFrame(1000, &frameInfo, &pDesktopResourceDxIf);
			if (SUCCEEDED(hRes))
			{
				break;
			}

			// An error occured...
			if (hRes == DXGI_ERROR_WAIT_TIMEOUT || hRes == DXGI_ERROR_INVALID_CALL)
			{
				Sleep(1);
				m_pDeskDupl->ReleaseFrame();
				WarnLog("capture_screen: AcquireNextFrame error %08x", hRes);
				continue;
			}

			if (hRes == DXGI_ERROR_ACCESS_LOST)
			{
				if (*ppDstBuf != NULL)
				{
					free(*ppDstBuf);
					*ppDstBuf = NULL;
				}
				int retryCnt = 0;
				Sleep(500);
				while (!init() && retryCnt < 50)
				{
					retryCnt++;
					Sleep(500);
				}
				continue;
			}

			ErrorLog("capture_screen: AcquireNextFrame failed [err: 0x%08x]", hRes);
			break;
		}
		else
		{
			init();
		}
	} while (--nRetryCnt > 0 && m_pDeskDupl);

	if (FAILED(hRes))
	{
		ErrorLog("capture_screen: AcquireNextFrame failed [err: 0x%08x]", hRes);
		if (pDesktopResourceDxIf)
			pDesktopResourceDxIf->Release();

		if (hRes == DXGI_ERROR_WAIT_TIMEOUT)
		{
			*dst_width = m_nPrevWidth;
			*dst_height = m_nPrevHeight;
			return true;
		}
		return false;
	}

	// QueryInterface for ID3D11Texture2D
	hRes = pDesktopResourceDxIf->QueryInterface(IID_PPV_ARGS(&pAcquiredDesktopImage));
	if (FAILED(hRes))
	{
		return false;
	}

	pDesktopResourceDxIf->Release();

	if (pAcquiredDesktopImage == nullptr)
	{
		return false;
	}

	// Copy image into GDI drawing texture
	m_pImmediateContext->CopyResource(m_pGDIImage, pAcquiredDesktopImage);

	// Get the interface to the GDI surface
	hRes = m_pGDIImage->QueryInterface(IID_PPV_ARGS(&pIDXGISurface1));
	if (FAILED(hRes))
	{
		return false;
	}

	// Draw cursor image into GDI surface
	if ((GetCursorInfo(&lCursorInfo)) && (lCursorInfo.flags == CURSOR_SHOWING))
	{
		POINT lCursorPosition = lCursorInfo.ptScreenPos;
		DWORD lCursorSize = lCursorInfo.cbSize;
		HDC  lHDC;

		WCHAR   wszDeviceName[33] = { 0 };
		HMONITOR MonitorOfCurrentWindow = MonitorFromPoint(lCursorPosition, MONITOR_DEFAULTTONULL);
		if (MonitorOfCurrentWindow != NULL)
		{
			MONITORINFOEX mi;
			memset(&mi, 0, sizeof(mi));
			mi.cbSize = sizeof(mi);
			if (GetMonitorInfo(MonitorOfCurrentWindow, &mi))
			{
				lCursorPosition.x = lCursorPosition.x - mi.rcMonitor.left;
				lCursorPosition.y = lCursorPosition.y - mi.rcMonitor.top;
				wcsncpy(wszDeviceName, mi.szDevice, 32);
			}
		}

		if (wcscmp(wszDeviceName, m_wszDeviceName) == 0)
		{
			pIDXGISurface1->GetDC(FALSE, &lHDC);
			DrawIconEx(lHDC, lCursorPosition.x, lCursorPosition.y,
				lCursorInfo.hCursor, 0, 0, 0, 0, DI_NORMAL | DI_DEFAULTSIZE);
			pIDXGISurface1->ReleaseDC(nullptr);
		}

		pIDXGISurface1->Release();
	}

	// Copy image into CPU access texture
	m_pImmediateContext->CopyResource(m_pDestImage, m_pGDIImage);

	// Copy from CPU access texture to bitmap buffer
	nSubresource = D3D11CalcSubresource(0, 0, 0);
	m_pImmediateContext->Map(m_pDestImage, nSubresource, D3D11_MAP_READ_WRITE, 0, &resource);

	nAdjustWidth = m_DesktopWidth;
	nAdjustHeight = m_DesktopHeight;

	ADJUST_RESOLUTION_16x9(nAdjustWidth, nAdjustHeight);


	// BMP 32 bpp
	bmpInfo.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	bmpInfo.bmiHeader.biBitCount = 32;
	bmpInfo.bmiHeader.biCompression = BI_RGB;
	bmpInfo.bmiHeader.biWidth = nAdjustWidth;
	bmpInfo.bmiHeader.biHeight = nAdjustHeight;
	bmpInfo.bmiHeader.biPlanes = 1;
	bmpInfo.bmiHeader.biSizeImage = nAdjustWidth * nAdjustHeight * 4;

	*dst_width = nAdjustWidth;
	*dst_height = nAdjustHeight;

	if (m_nPrevWidth != nAdjustWidth || m_nPrevHeight != nAdjustHeight)
	{
		if (*ppDstBuf != NULL)
		{
			free(*ppDstBuf);
			*ppDstBuf = NULL;
		}

		*ppDstBuf = (unsigned char*)malloc(bmpInfo.bmiHeader.biSizeImage);
		m_nPrevWidth = nAdjustWidth;
		m_nPrevHeight = nAdjustHeight;
	}
	memset(*ppDstBuf, 0, bmpInfo.bmiHeader.biSizeImage);

	crop_width = (nAdjustWidth - m_DesktopWidth) / 2;
	crop_width = crop_width > 0 ? crop_width : 0;
	crop_height = (nAdjustHeight - m_DesktopHeight) / 2;
	crop_height = crop_height > 0 ? crop_height : 0;

	nDstPitch = nAdjustWidth * 4;

	sptr = reinterpret_cast<unsigned char*>(resource.pData);
	dptr = (unsigned char*)*ppDstBuf + crop_width * 4;

	nSrcPitch = IMIN(m_DesktopWidth * 4, resource.RowPitch);

	for (nHeight = crop_height; nHeight < m_DesktopHeight + crop_height; ++nHeight)
	{
		memcpy(dptr, sptr, nSrcPitch);
		sptr += resource.RowPitch;
		dptr += nDstPitch;
	}

	m_pImmediateContext->Unmap(m_pDestImage, nSubresource);
	m_pDeskDupl->ReleaseFrame();
	return true;

}
