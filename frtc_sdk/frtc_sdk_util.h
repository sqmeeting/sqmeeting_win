#ifndef FRTC_SDK_UTIL_H_
#define FRTC_SDK_UTIL_H_

#include <windows.h>
#include <stdlib.h>
#include <string>
#include <locale>
#include <vector>
#include "stdafx.h"
#include "d3d9.h"
#include "gdiplus.h"
#include "gdipluscolor.h"

namespace FRTCSDK {
	typedef enum {
		NAME_CARD_BIG,
		NAME_CARD_MEDIUM,
		NAME_CARD_SMALL,
		NAME_CARD_TINY
	}SDK_NAME_CARD_TYPE;

	typedef enum {
		FONT_SIZE_BIG,
		FONT_SIZE_MEDIUM,
		FONT_SIZE_SMALL,
		FONT_SIZE_TINY
	}SDK_NAME_CARD_FONT_SIZE_TYPE;

	typedef struct {
		std::wstring monitor_name;
		std::wstring device_name;
		RECT rect;
		HMONITOR monitor_handle;
		int idx;
		bool is_primary;
	}DISPLAY_MONITOR_INFO, * PDISPLAY_MONITOR_INFO;


	class FRTCSdkUtil {
	public:
		static void init_lookup_table();
		static void init_convert_table_yuv420_rgb();
		static void yuv420_to_argb(unsigned char* src0,
			unsigned char* src1,
			unsigned char* src2,
			unsigned char* dst_ori,
			int width,
			int height);

		static void merge_yuv420(void* card_ptr,
			long w,
			long h,
			void* video_base,
			long vw,
			long vh);

		static int bgra_to_yuv420(int w,
			int h,
			unsigned char* bmp,
			unsigned char* yuv);

		static int bgra_to_yuv(int w,
			int h,
			unsigned char* bmp,
			unsigned char* yuv,
			int iStride);

		static bool rgb24_to_i420(unsigned char* argb,
			int width,
			int height,
			unsigned char* yuv);

		static Gdiplus::Image* load_image_from_resource(HMODULE module,
			const wchar_t* resid,
			const wchar_t* restype);

		static Gdiplus::Bitmap* resize_bitmap(Gdiplus::Bitmap* image,
			int width,
			int height);

		static BYTE* serialize_to_memory(Gdiplus::Bitmap* src_bitmap,
			int dst_width,
			int dst_height);

		static BYTE* bitmap_to_bgra(Gdiplus::Bitmap* bitmap);

		static BYTE* create_name_card(std::string& user_name,
			int wv,
			int hv,
			SDK_NAME_CARD_TYPE namecard_type,
			SDK_NAME_CARD_FONT_SIZE_TYPE font_size_type);

		static BYTE* create_water_mark(std::string& msg,
			int wv,
			int hv);

		static void get_monitor_list(std::vector<DISPLAY_MONITOR_INFO>& monitors);

		static std::wstring get_fitted_string(Gdiplus::Graphics* g,
			Gdiplus::Font* font,
			std::wstring& source,
			int max_length);

		static std::wstring get_fitted_string_ex(Gdiplus::Graphics* g,
			Gdiplus::Font* font,
			std::wstring& source,
			int max_length);

		static std::wstring string_to_wstring(const std::string str);
		static std::string wstring_to_string(const std::wstring str);

		static std::string get_utf8_string(const std::string& str);
		static std::string get_ansi_string(const std::string& str);

		static void guid_to_string(const GUID& guid, std::string& str);
		static void guid_to_wstring(const GUID& guid, std::wstring& wstr);
		static void get_guid_from_wstring(const TCHAR* guid_str, GUID* guid);

		static unsigned int timestamp();

		template<typename ... Args>
		static std::string string_format(const std::string& format, Args ... args)
		{
			int size_s = std::snprintf(nullptr, 0, format.c_str(), args ...) + 1; // Extra space for '\0'
			if (size_s <= 0) { throw std::runtime_error("Error during formatting."); }
			auto size = static_cast<size_t>(size_s);
			std::unique_ptr<char[]> buf(new char[size]);
			std::snprintf(buf.get(), size, format.c_str(), args ...);
			return std::string(buf.get(), buf.get() + size - 1); // We don't want the '\0' inside
		}
	};
}

#endif
