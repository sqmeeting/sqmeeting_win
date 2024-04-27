#include "frtc_sdk_util.h"
#include "resource.h"

extern HINSTANCE g_hInstance;

namespace FRTCSDK {

int kMonitorIndex = 0;

//YUV420 to RGB
long int crv_tab[256];
long int cbu_tab[256];
long int cgu_tab[256];
long int cgv_tab[256];
long int tab_76309[256];
unsigned char clp[1024]; //for clip in Rec.601(CCIR 601)

// RGB to YUV420
int rgb2yuv_yr[256], rgb2yuv_yg[256], rgb2yuv_yb[256];
int rgb2yuv_ur[256], rgb2yuv_ug[256], rgb2yuv_ubvr[256];
int rgb2yuv_vg[256], rgb2yuv_vb[256];

void FRTCSdkUtil::init_lookup_table() {
	for (int i = 0; i < 256; i++) {
        rgb2yuv_yr[i] = (int)((float)65.481 * (i << 8));
        rgb2yuv_yg[i] = (int)((float)128.553 * (i << 8));
        rgb2yuv_yb[i] = (int)((float)24.966 * (i << 8));
        rgb2yuv_ur[i] = (int)((float)37.797 * (i << 8));
        rgb2yuv_ug[i] = (int)((float)74.203 * (i << 8));
        rgb2yuv_ubvr[i] = (int)((float)112 * (i << 8));
        rgb2yuv_vg[i] = (int)((float)93.786 * (i << 8));
        rgb2yuv_vb[i] = (int)((float)18.214 * (i << 8));
    }
}

void FRTCSdkUtil::init_convert_table_yuv420_rgb() {
    long int crv = 104597; 
    long int cbu = 132201;
    long int cgu = 25675;  
    long int cgv = 53279;

    for (int i = 0; i < 256; i++) {
        crv_tab[i] = (i - 128) * crv;
        cbu_tab[i] = (i - 128) * cbu;
        cgu_tab[i] = (i - 128) * cgu;
        cgv_tab[i] = (i - 128) * cgv;
        tab_76309[i] = 76309 * (i - 16);
    }

    for (int i = 0; i < 384; i++) {
        clp[i] = 0;
    }

    int ind = 384;
    for (int i = 0; i < 256; i++) {
        clp[ind++] = i;
    }

    ind = 640;
    for (int i = 0; i < 384; i++) {
        clp[ind++] = 255;
    }
}

void FRTCSdkUtil::yuv420_to_argb(unsigned char* src0,
                                 unsigned char* src1,
                                 unsigned char* src2,
                                 unsigned char* dst_ori,
                                 int width,
                                 int height) {
    int y1, y2, u, v;
    int c1, c2, c3, c4;

    unsigned char* py1 = src0;
    unsigned char* py2 = py1 + width;
    unsigned char* d1 = dst_ori;
    unsigned char* d2 = d1 + 4 * width;

    for (int j = 0; j < height; j += 2) {
        for (int i = 0; i < width; i += 2) {
            u = *src1++;
            v = *src2++;

            c1 = cbu_tab[u];
            c2 = cgu_tab[u];
            c3 = cgv_tab[v];
            c4 = crv_tab[v];

            //up-left
            y1 = tab_76309[*py1++];
            *d1++ = clp[384 + ((y1 + c1) >> 16)];
            *d1++ = clp[384 + ((y1 - c2 - c3) >> 16)];
            *d1++ = clp[384 + ((y1 + c4) >> 16)];
            *d1++ = 0xFF;

            //down-left
            y2 = tab_76309[*py2++];
            *d2++ = clp[384 + ((y2 + c1) >> 16)];
            *d2++ = clp[384 + ((y2 - c2 - c3) >> 16)];
            *d2++ = clp[384 + ((y2 + c4) >> 16)];
            *d2++ = 0xFF;

            //up-right
            y1 = tab_76309[*py1++];
            *d1++ = clp[384 + ((y1 + c1) >> 16)];
            *d1++ = clp[384 + ((y1 - c2 - c3) >> 16)];
            *d1++ = clp[384 + ((y1 + c4) >> 16)];
            *d1++ = 0xFF;

            //down-right
            y2 = tab_76309[*py2++];
            *d2++ = clp[384 + ((y2 + c1) >> 16)];
            *d2++ = clp[384 + ((y2 - c2 - c3) >> 16)];
            *d2++ = clp[384 + ((y2 + c4) >> 16)];
            *d2++ = 0xFF;
        }

        d1 += 4 * width;
        d2 += 4 * width;
        py1 += width;
        py2 += width;
    }
}

void FRTCSdkUtil::merge_yuv420(void* card_ptr,
                               long w,
                               long h,
                               void* video_base,
                               long vw,
                               long vh) {
    int margin = 0;
    if (w > vw || h + margin > vh) {
        return;
    }

    int real_width = w;
    if (real_width < 0)
        real_width = 0;

    unsigned long start_x_pos = 0;
    unsigned long start_y_pos = vh - h - margin;
    unsigned long bytes_per_row = vw;
    unsigned long pic_y_pos = bytes_per_row * start_y_pos + start_x_pos;
    unsigned char* base_addr = (unsigned char*)video_base;
    unsigned char* y = (unsigned char*)card_ptr;
    unsigned char* u = y + w * h;
    unsigned char* v = u + w * h * 1 / 4;
    unsigned char* a = (unsigned char*)card_ptr + w * h * 3;
    unsigned int base_card = w - real_width;

    for (int i = 0; i < h; i++) {
        for (int j = 0; j < real_width; j++) {
            if (a[i * w + j + base_card]) {
                base_addr[pic_y_pos + i * bytes_per_row + j] = 
                    (base_addr[pic_y_pos + i * bytes_per_row + j] / 3) + 
                    (y[i * w + j + base_card] * 2 / 3);
            }
        }
    }

    bytes_per_row = vw / 2;
    unsigned long pic_u_pos = vw * vh + 
                              bytes_per_row * start_y_pos / 2 + 
                              start_x_pos / 2;

    unsigned long pic_v_pos = vw * vh + 
                              vw * vh / 4 + 
                              bytes_per_row * start_y_pos / 2 + 
                              start_x_pos / 2;

    for (int i = 0; i < h / 2; i++) {
        for (int j = 0; j < real_width / 2; j++) {
            if (a[i * 2 * w + 2 * j + base_card]) {
                // u
                base_addr[pic_u_pos + i * bytes_per_row + j] = 
                    u[i * w / 2 + j + base_card / 2];

                // v
                base_addr[pic_v_pos + i * bytes_per_row + j] = 
                    v[i * w / 2 + j + base_card / 2];
            }
        }
    }
}

int FRTCSdkUtil::bgra_to_yuv420(int w,
                                int h,
                                unsigned char* bmp,
                                unsigned char* yuv) {
    unsigned char* start_pos = yuv + w * h * 3;

    for (int i = 0; i < h; ++i) {
        for (int j = 0; j < w; ++j) {
            *start_pos = *(bmp + (i * w + j) * 4 + 3);
            ++start_pos;
        }
    }

    return bgra_to_yuv(w, h, bmp, yuv, w);
}

int FRTCSdkUtil::bgra_to_yuv(int w,
                             int h,
                             unsigned char* bmp,
                             unsigned char* yuv,
                             int stride) {
    w = w & ~1;
    h = h & ~1;

    if (w > stride) {
        return 0;
    }

    unsigned char* uu = (unsigned char*)malloc(sizeof(unsigned char) * (w * h));
    if (uu == NULL) {
        return 0;
    }

    unsigned char* vv = (unsigned char*)malloc(sizeof(unsigned char) * (w * h));
    if (vv == NULL) {
        free(uu);
        return 0;
    }

    unsigned char* y = yuv;
    unsigned char* u = uu;
    unsigned char* v = vv;

    unsigned char* b = bmp;
    unsigned char* g = bmp + 1;
    unsigned char* r = bmp + 2;

    // yuv values for rgb values
    for (int i = 0; i < h; i++) {
        for (int j = 0; j < w; j++) {
            *y++ = (rgb2yuv_yr[*r] + rgb2yuv_yg[*g] + rgb2yuv_yb[*b] + 1048576) >> 16;
            *u++ = (-rgb2yuv_ur[*r] - rgb2yuv_ug[*g] + rgb2yuv_ubvr[*b] + 8388608) >> 16;
            *v++ = (rgb2yuv_ubvr[*r] - rgb2yuv_vg[*g] - rgb2yuv_vb[*b] + 8388608) >> 16;
            r += 4;
            g += 4;
            b += 4;
        }

        if (stride > w) {
            r += (stride - w) * 4;
            g += (stride - w) * 4;
            b += (stride - w) * 4;
        }
    }

    // sample the u & v to obtain yuv 4:2:0 format
    u = yuv + w * h;
    v = u + (w * h) / 4;

    unsigned char* pu1 = uu;
    unsigned char* pu2 = pu1 + 1;
    unsigned char* pu3 = pu1 + w;
    unsigned char* pu4 = pu3 + 1;

    unsigned char* pv1 = vv;
    unsigned char* pv2 = pv1 + 1;
    unsigned char* pv3 = pv1 + w;
    unsigned char* pv4 = pv3 + 1;
    
    // do sampling
    for (int i = 0; i < h; i += 2) {
        for (int j = 0; j < w; j += 2) {
            *u++ = (*pu1 + *pu2 + *pu3 + *pu4) >> 2;
            *v++ = (*pv1 + *pv2 + *pv3 + *pv4) >> 2;
            pu1 += 2;
            pu2 += 2;
            pu3 += 2;
            pu4 += 2;
            pv1 += 2;
            pv2 += 2;
            pv3 += 2;
            pv4 += 2;
        }

        pu1 += w;
        pu2 += w;
        pu3 += w;
        pu4 += w;
        pv1 += w;
        pv2 += w;
        pv3 += w;
        pv4 += w;
    }

    free(uu);
    free(vv);

    return 1;
}

bool FRTCSdkUtil::rgb24_to_i420(unsigned char* argb,
                                int width,
                                int height,
                                unsigned char* yuv) {
    static unsigned short y_tbl[3][256] = { {0}, {0}, {0} };
    static short u_tbl[3][256] = { {0}, {0}, {0} };
    static short v_tbl[3][256] = { {0}, {0}, {0} };

    if (y_tbl[0][255] == 0) {
        for (int i = 0; i < 256; i++) {
            y_tbl[0][i] = 66 * i;
            y_tbl[1][i] = 129 * i;
            y_tbl[2][i] = 25 * i;
            u_tbl[0][i] = -38 * i;
            u_tbl[1][i] = -74 * i;
            u_tbl[2][i] = 112 * i;
            v_tbl[0][i] = 112 * i;
            v_tbl[1][i] = -94 * i;
            v_tbl[2][i] = -18 * i;
        }
    }

    bool need_uv_vertically;
    unsigned char* u = yuv + width * height;
    unsigned char* v = u + width * height / 4;
    unsigned char* rgb = argb;

    for (int i = 0; i < height; i++) {
        need_uv_vertically = (i % 2 == 0);
        argb = rgb + (width * 3) * (height - i - 1);

        for (int j = 0; j < width; j++) {
            int r_off = 2;
            int g_off = 1;
            int b_off = 0;

            *yuv = (unsigned char)((y_tbl[0][*(argb + r_off)] + 
                                    y_tbl[1][*(argb + g_off)] + 
                                    y_tbl[2][*(argb + b_off)] + 128) >> 8) + 16;
            yuv++;

            if (need_uv_vertically && (j % 2 == 0)) {
                *u = (unsigned char)((u_tbl[0][*(argb + r_off)] + 
                                      u_tbl[1][*(argb + g_off)] + 
                                      u_tbl[2][*(argb + b_off)] + 128) >> 8) + 128;

                *v = (unsigned char)((v_tbl[0][*(argb + r_off)] + 
                                      v_tbl[1][*(argb + g_off)] + 
                                      v_tbl[2][*(argb + b_off)] + 128) >> 8) + 128;
                u++;
                v++;
            }

            argb += 3;
        }
    }

    return true;
}

Gdiplus::Image* FRTCSdkUtil::load_image_from_resource(HMODULE module,
                                                      const wchar_t* resid,
                                                      const wchar_t* restype) {
    IStream* stream = nullptr;
    HGLOBAL global = nullptr;
    Gdiplus::Image* img;

    HRSRC hrsrc = FindResourceW(g_hInstance, resid, restype);
    if (hrsrc) {
        DWORD resource_size = SizeofResource(module, hrsrc);
        if (resource_size > 0) {
            HGLOBAL global_resource = LoadResource(module, hrsrc);
            if (global_resource) {
                void* image_bytes = LockResource(global_resource);

                global = ::GlobalAlloc(GHND, resource_size);
                if (global) {
                    void* buffer = ::GlobalLock(global);
                    if (buffer) {
                        memcpy(buffer, image_bytes, resource_size);
                        HRESULT ret = CreateStreamOnHGlobal(global, TRUE, &stream);
                        if (SUCCEEDED(ret)) {
                            global = nullptr;
                            img = Gdiplus::Image::FromStream(stream);
                        }
                    }
                }
            }
        }
    }

    if (stream) {
        stream->Release();
        stream = nullptr;
    }

    if (global) {
        GlobalFree(global);
    }

    return img;
}

// memory needs to be deleted by caller
Gdiplus::Bitmap* FRTCSdkUtil::resize_bitmap(Gdiplus::Bitmap* image,
                                            int width,
                                            int height) {
    int w = image->GetWidth();
    int h = image->GetHeight();
    if (w == width && h == height) {
        return image;
    }

    Gdiplus::Bitmap* new_image = new Gdiplus::Bitmap(width, 
                                                     height, 
                                                     PixelFormat32bppARGB);
    new_image->SetResolution(image->GetHorizontalResolution(), 
                             image->GetVerticalResolution());
    Gdiplus::Graphics gr(new_image);

    gr.SetSmoothingMode(Gdiplus::SmoothingModeDefault);
    gr.SetInterpolationMode(Gdiplus::InterpolationModeDefault);
    gr.SetPixelOffsetMode(Gdiplus::PixelOffsetModeDefault);
    gr.DrawImage(image, Gdiplus::RectF(0, 0, width, height));

    return new_image;
}

// memory needs to be deleted by caller
BYTE* FRTCSdkUtil::serialize_to_memory(Gdiplus::Bitmap* src_bitmap,
                                       int dst_width,
                                       int dst_height) {
    Gdiplus::Bitmap* dst_bitmap = FRTCSdkUtil::resize_bitmap(src_bitmap, 
                                                               dst_width, 
                                                               dst_height);
    int w = dst_bitmap->GetWidth();
    int h = dst_bitmap->GetHeight();
    BYTE* rgb = FRTCSdkUtil::bitmap_to_bgra(dst_bitmap);

    return rgb;
}

BYTE* FRTCSdkUtil::bitmap_to_bgra(Gdiplus::Bitmap* bitmap) {
    int w = bitmap->GetWidth();
    int h = bitmap->GetHeight();
    BYTE* rgba_bytes = new BYTE[4 * (w * h)];

    int i = 0;
    for (int y = 0; y < h; y++) {
        for (int x = 0; x < w; x++) {
            Gdiplus::Color pix;
            bitmap->GetPixel(x, y, &pix);
            rgba_bytes[i++] = pix.GetB();
            rgba_bytes[i++] = pix.GetG();
            rgba_bytes[i++] = pix.GetR();
            rgba_bytes[i++] = pix.GetA();
        }
    }

    return rgba_bytes;
}

BYTE* FRTCSdkUtil::create_name_card(std::string& user_name,
                                    int wv,
                                    int hv,
                                    SDK_NAME_CARD_TYPE namecard_type,
                                    SDK_NAME_CARD_FONT_SIZE_TYPE font_size_type) {
    if (wv > 1920 || hv > 1080) {
        return NULL;
    }

    Gdiplus::FontFamily  font_family(L"Arial");
    float font_size = ((float)hv / 3) * 2;

    switch (namecard_type) {
    case NAME_CARD_BIG:
        font_size = (float)hv / 12;
        break;
    case NAME_CARD_MEDIUM:
        font_size = (float)hv / 12;
        if (font_size_type == FONT_SIZE_MEDIUM)
            font_size = (float)hv / 12;//6;
        break;
    case NAME_CARD_SMALL:
        font_size = (float)hv / 6;
        break;
    case NAME_CARD_TINY:
        font_size = ((float)hv / 3) * 2;
        break;
    default:
        break;
    }

    Gdiplus::Font font(&font_family, 
                       font_size, 
                       Gdiplus::FontStyleBold, 
                       Gdiplus::UnitPixel);

    Gdiplus::SolidBrush white_brush(Gdiplus::Color(255, 255, 255));
    Gdiplus::PointF point((float)hv / 8, (float)hv / 6);
    Gdiplus::Bitmap* bmp_buf = new Gdiplus::Bitmap(wv, hv);
    Gdiplus::Graphics gc(bmp_buf);

    gc.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);
    gc.SetInterpolationMode(Gdiplus::InterpolationModeHighQualityBicubic);
    gc.SetCompositingQuality(Gdiplus::CompositingQualityHighQuality);

    Gdiplus::Image* bg_img = nullptr;
    if (namecard_type != NAME_CARD_TINY) {
        bg_img = FRTCSdkUtil::load_image_from_resource(g_hInstance, 
                                                       MAKEINTRESOURCE(IDB_PNG1), 
                                                       L"PNG");
        gc.DrawImage(bg_img, 0, 0, wv, hv);
    }
    else {
        Gdiplus::SolidBrush bg_brush(Gdiplus::Color(50, 86, 111));
        gc.FillRectangle(&bg_brush, 0, 0, wv, hv);
    }

    std::string tmp_str = FRTCSDK::FRTCSdkUtil::get_ansi_string(user_name);
    std::string display_name;
    if (tmp_str.size() > 20) {
        display_name = tmp_str.substr(0, 20) + "...";
    }
    else {
        display_name = tmp_str;
    }

    std::wstring name_wstr = FRTCSDK::FRTCSdkUtil::string_to_wstring(display_name);
    std::wstring str = L"";
    if (namecard_type != NAME_CARD_TINY) {
        str = FRTCSdkUtil::get_fitted_string(&gc, 
                                             &font, 
                                             name_wstr, 
                                             (double)wv * 3 / 4);
    }
    else {
        str = FRTCSdkUtil::get_fitted_string(&gc, 
                                             &font, 
                                             name_wstr, 
                                             wv);
    }

    Gdiplus::StringFormat str_fmt = Gdiplus::StringFormat::GenericTypographic();
    Gdiplus::RectF rect((Gdiplus::REAL)0.0, 
                        (Gdiplus::REAL)0.0, 
                        (Gdiplus::REAL)wv, 
                        (Gdiplus::REAL)hv);
    Gdiplus::RectF bounds;
    gc.MeasureString(str.c_str(), -1, &font, Gdiplus::PointF(), &str_fmt, &bounds);

    if (namecard_type != NAME_CARD_TINY) {
        float x = (wv - bounds.Width) / 2;
        x *= 0.97;
        float y = (hv - bounds.Height) / 2;
        Gdiplus::PointF pt_center(x, y);
        gc.DrawString(str.c_str(), -1, &font, pt_center, &white_brush);
    }
    else {
        gc.DrawString(str.c_str(), -1, &font, point, &white_brush);
    }

    BYTE* name_pic = serialize_to_memory(bmp_buf, wv, hv);
    delete bmp_buf;

    BYTE* name_yuv = new BYTE[wv * hv * 4];
    bgra_to_yuv420(wv, hv, name_pic, name_yuv);
    delete[] name_pic;

    if (bg_img != nullptr) {
        delete bg_img;
    }

    return name_yuv;
}

BYTE* FRTCSdkUtil::create_water_mark(std::string& msg, int wv, int hv) {
    if (wv > 1920 || hv > 1080) {
        return NULL;
    }

    Gdiplus::FontFamily font_family(L"Microsoft YaHei");
    float font_size = ((float)hv / 3) * 2;
    Gdiplus::Font font(&font_family, 
                       40, 
                       Gdiplus::FontStyleRegular, 
                       Gdiplus::UnitPixel);

    Gdiplus::SolidBrush white_brush(Gdiplus::Color(0x09, 0xCC, 0xCC, 0xCC));
    Gdiplus::PointF point((float)(-wv) / 6, (float)hv / 5 * 4);
    Gdiplus::Bitmap bmp_buf(wv, hv, PixelFormat32bppARGB);
    Gdiplus::Graphics gc(&bmp_buf);
    gc.Clear(Gdiplus::Color::Transparent);
    gc.SetTextRenderingHint(
        Gdiplus::TextRenderingHint::TextRenderingHintSingleBitPerPixelGridFit);
    gc.SetCompositingQuality(Gdiplus::CompositingQualityGammaCorrected);

    std::string _water_mark_str = msg;
    for (int i = 0; i < 20; i++) {
        _water_mark_str = _water_mark_str.append("  ");
        _water_mark_str = _water_mark_str.append(msg);
    }

    std::wstring msg_wstr = string_to_wstring(_water_mark_str);
    std::wstring str = L"";
    str = FRTCSdkUtil::get_fitted_string_ex(&gc, &font, msg_wstr, (double)wv);

    Gdiplus::StringFormat str_fmt = Gdiplus::StringFormat::GenericTypographic();
    Gdiplus::RectF rect((Gdiplus::REAL)0.0, 
                        (Gdiplus::REAL)0.0, 
                        (Gdiplus::REAL)wv, 
                        (Gdiplus::REAL)hv);
    Gdiplus::RectF bounds;
    gc.MeasureString(str.c_str(), -1, &font, Gdiplus::PointF(), &str_fmt, &bounds);
    gc.RotateTransform(-30);
    gc.DrawString(str.c_str(), -1, &font, point, &white_brush);
    gc.ResetTransform();

    BYTE* name_pic = serialize_to_memory(&bmp_buf, wv, hv);
    BYTE* name_yuv = new BYTE[wv * hv * 4];
    bgra_to_yuv420(wv, hv, name_pic, name_yuv);
    delete[] name_pic;

    return name_yuv;
}

BOOL CALLBACK GetDisplayMonitors(HMONITOR hMonitor, 
                                 HDC hdcMonitor, 
                                 LPRECT lprcMonitor, 
                                 LPARAM dwData) {
	MONITORINFOEX  monitor_info;
	monitor_info.cbSize = sizeof(MONITORINFOEX);
	bool success = GetMonitorInfo(hMonitor, &monitor_info);
	if (success) {
		DISPLAY_MONITOR_INFO info;
		info.device_name = std::wstring(monitor_info.szDevice);
		kMonitorIndex++;
		info.monitor_name = L"Desktop " + std::to_wstring(kMonitorIndex);
		info.idx = kMonitorIndex;
		info.monitor_handle = hMonitor;
		info.rect = *lprcMonitor;
		if (monitor_info.dwFlags == MONITORINFOF_PRIMARY) {
			info.is_primary = true;
        }
		else {
			info.is_primary = false;
        }

		((std::vector<DISPLAY_MONITOR_INFO> *)dwData)->push_back(info);
	}
		
    return TRUE;
}

void FRTCSdkUtil::get_monitor_list(std::vector<DISPLAY_MONITOR_INFO>& monitors) {
    kMonitorIndex = 0;
    monitors.clear();
    EnumDisplayMonitors(NULL, NULL, GetDisplayMonitors, (LPARAM)&monitors);
}

std::wstring FRTCSdkUtil::get_fitted_string(Gdiplus::Graphics* g,
                                            Gdiplus::Font* font,
                                            std::wstring& source,
                                            int max_length) {
    std::wstring target = source;
    const Gdiplus::StringFormat* str_fmt = 
        Gdiplus::StringFormat::GenericTypographic();
    Gdiplus::RectF bounds;
    g->MeasureString(source.c_str(), 
                     -1, 
                     font, 
                     Gdiplus::PointF(), 
                     str_fmt, 
                     &bounds);

    while (bounds.Width > max_length) {
        target = target.substr(0, target.length() - 4);
        target += L"...";
        g->MeasureString(target.c_str(), 
                         -1, 
                         font, 
                         Gdiplus::PointF(), 
                         str_fmt, 
                         &bounds);
    }

    return target;
}

std::wstring FRTCSdkUtil::get_fitted_string_ex(Gdiplus::Graphics* g,
                                                   Gdiplus::Font* font,
                                                   std::wstring& source,
                                                   int max_length) {
    std::wstring target = source;
    const Gdiplus::StringFormat* str_fmt = 
        Gdiplus::StringFormat::GenericTypographic();
    Gdiplus::RectF bounds;
    g->MeasureString(source.c_str(), 
                     -1, 
                     font, 
                     Gdiplus::PointF(), 
                     str_fmt, 
                     &bounds);

    while (bounds.Width > max_length) {
        target = target.substr(0, target.length() - 1);
        g->MeasureString(target.c_str(), 
                         -1, 
                         font, 
                         Gdiplus::PointF(), 
                         str_fmt, 
                         &bounds);
    }

    return target;
}

std::wstring FRTCSdkUtil::string_to_wstring(const std::string str) {
    unsigned len = str.size() * 2;
    std::string strLocale = setlocale(LC_CTYPE, "");
    wchar_t* p = new wchar_t[len];
    wmemset(p, 0, len);

    mbstowcs(p, str.c_str(), len);
    std::wstring wstr(p);
    delete[] p;
    setlocale(LC_CTYPE, strLocale.c_str());
    return wstr;
}

std::string FRTCSdkUtil::wstring_to_string(const std::wstring wstr) {
    unsigned len = wstr.size() * 4;
    std::string strLocale = setlocale(LC_ALL, "");
    char* p = new char[len];
    memset(p, 0, len);

    wcstombs(p, wstr.c_str(), len);
    std::string str(p);
    delete[] p;
    setlocale(LC_ALL, strLocale.c_str());
    return str;
}

std::string FRTCSdkUtil::get_utf8_string(const std::string& str) {
    size_t wlen = MultiByteToWideChar(CP_ACP, 0, str.c_str(), -1, NULL, NULL);
    LPWSTR wstr = (LPWSTR)_alloca((wlen + 1) * sizeof(WCHAR));
    MultiByteToWideChar(CP_ACP, 0, str.c_str(), -1, wstr, wlen);
    wstr[wlen] = '\0';

    size_t len = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, NULL, NULL, NULL, NULL);
    char* buffer = new char[len + 1];
    memset(buffer, 0, len + 1);
    WideCharToMultiByte(CP_UTF8, 0, wstr, wlen, buffer, len, NULL, NULL);
    std::string str_utf8(buffer);
    delete[] buffer;
    return std::move(str_utf8);
}

std::string FRTCSdkUtil::get_ansi_string(const std::string& str) {
    size_t wlen = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, NULL, NULL);
    LPWSTR wstr = (LPWSTR)_alloca((wlen + 1) * sizeof(WCHAR));
    MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, wstr, wlen);
    wstr[wlen] = '\0';

    size_t len = WideCharToMultiByte(CP_ACP, 0, wstr, -1, NULL, NULL, NULL, NULL);
    char* buffer = new char[len + 1];
    memset(buffer, 0, len + 1);
    WideCharToMultiByte(CP_ACP, 0, wstr, wlen, buffer, len, NULL, NULL);
    std::string str_ansi(buffer);
    delete[] buffer;
    return std::move(str_ansi);
}

void FRTCSdkUtil::guid_to_string(const GUID& guid, std::string& str) {
    CHAR result[70];
    sprintf(result, "{%08X-%04X-%04x-%02X%02X-%02X%02X%02X%02X%02X%02X}", 
            guid.Data1, guid.Data2, guid.Data3, guid.Data4[0], 
            guid.Data4[1], guid.Data4[2], guid.Data4[3], guid.Data4[4], 
            guid.Data4[5], guid.Data4[6], guid.Data4[7]);
    str = result;
}

void FRTCSdkUtil::guid_to_wstring(const GUID& guid, std::wstring& wstr) {
    WCHAR result[70];
    wsprintf(result, L"{%08X-%04X-%04x-%02X%02X-%02X%02X%02X%02X%02X%02X}", 
             guid.Data1, guid.Data2, guid.Data3, guid.Data4[0], 
             guid.Data4[1], guid.Data4[2], guid.Data4[3], guid.Data4[4], 
             guid.Data4[5], guid.Data4[6], guid.Data4[7]);
    wstr = result;
}

void FRTCSdkUtil::get_guid_from_wstring(const TCHAR* guid_str, GUID* guid) {
    char s[40];
    unsigned long p0;
    int p1, p2, p3, p4, p5, p6, p7, p8, p9, p10;
    std::string str = wstring_to_string(guid_str);

    sscanf_s(str.c_str(), "{%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}", 
             &p0, &p1, &p2, &p3, &p4, &p5, &p6, &p7, &p8, &p9, &p10);

    guid->Data1 = p0;
    guid->Data2 = p1;
    guid->Data3 = p2;
    guid->Data4[0] = p3;
    guid->Data4[1] = p4;
    guid->Data4[2] = p5;
    guid->Data4[3] = p6;
    guid->Data4[4] = p7;
    guid->Data4[5] = p8;
    guid->Data4[6] = p9;
    guid->Data4[7] = p10;
}

unsigned int FRTCSdkUtil::timestamp() {
    LARGE_INTEGER StartTime;
    LARGE_INTEGER Frequency;

    ::QueryPerformanceCounter(&StartTime);
    ::QueryPerformanceFrequency(&Frequency);

    return (unsigned long)((long long)(
        StartTime.QuadPart * 1000000.0) / Frequency.QuadPart);
}
}