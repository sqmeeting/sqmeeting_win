#include <dxgi1_2.h>
#include <d3d11.h>

#define ADJUST_RESOLUTION_16x9(w, h)	\
    if(w * 9 > h * 16)					\
        h = w * 9 / 16;					\
    else								\
        w = h * 16 / 9;

class DXGI_DesktopDuplicator
{
public:
    DXGI_DesktopDuplicator(int device_index, LPCWSTR lpwDeviceName);
    ~DXGI_DesktopDuplicator();
    bool init();
    bool capture_screen(void **ppDstBuf, int *dst_width, int *dst_height);
    void cleanup();
    HRESULT get_dxgi_error();
private:
    ID3D11Device    *m_pDevice = NULL;
    ID3D11DeviceContext *m_pImmediateContext = NULL;

    ID3D11Texture2D *m_pGDIImage;
    ID3D11Texture2D *m_pDestImage;
    IDXGIOutputDuplication *m_pDeskDupl;

    unsigned int    m_nPrevWidth;
    unsigned int    m_nPrevHeight;
    unsigned int    m_DesktopWidth;
    unsigned int    m_DesktopHeight;
    unsigned int    m_nDevIndex;
    WCHAR           m_wszDeviceName[33];
    HANDLE          m_Mutex;

    HRESULT m_hrLastDXGIErr;
};