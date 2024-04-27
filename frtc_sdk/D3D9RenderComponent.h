#pragma once

#include <map>
#include "d3d9.h"
#include "Wtsapi32.h"
#pragma comment(lib,"Wtsapi32.lib")

#define SMALL_CELL_WIDTH 256
#define SMALL_CELL_HEIGHT 144

#define SMALL_CELL_WIDTH_4K 512
#define SMALL_CELL_HEIGHT_4K 288

#define D3DFMT_YV12 (D3DFORMAT)MAKEFOURCC('Y', 'V', '1', '2')
#define D3DFMT_NV12 (D3DFORMAT)MAKEFOURCC('N', 'V', '1', '2')
#define D3DFMT_I420 (D3DFORMAT)MAKEFOURCC('I', '4', '2', '0')

class D3D9RenderComponent
{
public:
    D3D9RenderComponent(HWND hWnd, 
                        IDirect3DDevice9 *pDevice, 
                        D3DPRESENT_PARAMETERS deviceParameter, 
                        D3DFORMAT offScreenFormat, 
                        UINT width, 
                        UINT height);
    ~D3D9RenderComponent();

    HRESULT create_render();
    HRESULT destroy_render();
    HRESULT recreate_render(IDirect3DDevice9* device, 
                            D3DPRESENT_PARAMETERS device_param);

    IDirect3DSurface9* get_backbuffer();
    HRESULT draw_frame(D3DFORMAT Format, 
                       BYTE* image, 
                       int width, 
                       int height, 
                       bool hasBorder, 
                       int borderWidth, 
                       bool isContent, 
                       std::string watermark_str);

private:
    void copy_frame_to_d3d_rect_yv12(BYTE* src, 
                                      BYTE* dest, 
                                      int width, 
                                      int height, 
                                      int stride);

private:
    bool _border_in_last_frame;
    HWND _wnd;
    UINT _width;    
    UINT _height;   
    IDirect3DSwapChain9* _swap_chain_ptr;
    IDirect3DSurface9* _plain_surface_ptr;
    IDirect3DDevice9* _device_ptr;
    D3DPRESENT_PARAMETERS _device_parameter;
    D3DFORMAT _offscreen_format;

    bool _frame_width_logged = false;
};

class D3D9RendererEngine
{
public:
    D3D9RendererEngine();
    ~D3D9RendererEngine();

    HRESULT create_d3d9_device(HWND hwnd);
    HRESULT reset_d3d9_device();
    void destory_d3d9_device();

    HRESULT create_render_component(HWND hwnd, 
                                    int width, 
                                    int height, 
                                    D3DFORMAT format);

    HRESULT draw_frame(HWND hwnd, 
                       D3DFORMAT format, 
                       BYTE* image, 
                       int width, 
                       int height, 
                       bool has_border, 
                       bool is_content, 
                       std::string watermark_str);

    HRESULT test_cooperative_level();

private:
    HWND _wnd;
    IDirect3D9Ex* _d3d9_ex;
    IDirect3DDevice9* _device_ptr;
    D3DPRESENT_PARAMETERS _d3d_param;
    std::map<HWND, D3D9RenderComponent*> _render_list;
};