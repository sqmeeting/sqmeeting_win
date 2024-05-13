#include "FrtcVideoWndMgr.h"
#include "frtc_sdk_util.h"
#include "log.h"


FRTCVideoWndMgr::FRTCVideoWndMgr()
	: _hMain(NULL),
	_active_speaker_uuid(""),
	_cell_customization_uuid(""),
	_current_lecture_uuid(""),
	_current_video_view(NULL)
{
	_small_cell_width = SMALL_CELL_WIDTH;
	_small_cell_height = SMALL_CELL_HEIGHT;

	int screenWidth = GetSystemMetrics(SM_CXSCREEN);
	if (screenWidth > 2560)
	{
		_small_cell_width = SMALL_CELL_WIDTH_4K;
		_small_cell_height = SMALL_CELL_HEIGHT_4K;
	}
	_is_sharing_content = false;
	_is_full_screen = false;
	_wnd_count = 10;
	_is_grid_mode = false;
	_is_local_video_hidden = false;
	_is_watermark_enabled = false;
	_watermark_str = "";

	_d3d9_renderer = std::make_shared<D3D9RendererEngine>();
}


FRTCVideoWndMgr::~FRTCVideoWndMgr()
{

}

void FRTCVideoWndMgr::init_free_video_wnd()
{
	_free_video_wnd_list.clear();
	for (int i = 0; i < 10/*_wnd_count*/; i++) {
		_free_video_wnd_list.push_back(&_video_windows[i]);
	}
}


void FRTCVideoWndMgr::set_main_window(HWND hMain, HINSTANCE hInst, bool useGDIRender)
{
	AutoLock autolock(_lock);
	_hMain = hMain;
	::GetClientRect(_hMain, &_main_rect);
	if (useGDIRender)
	{
		for (int i = 0; i < 10; i++) {
			BOOL ret = _video_windows[i].Create(
                            0, 
                            L"", 
                            WS_CHILD | WS_BORDER | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
		                    200, 
                            390, 
                            1920, 
                            1080, 
                            _hMain, 
                            NULL, 
                            hInst);

			InfoLog("set_main_window, index:%d,create ret=%d,err:%u hwnd:%u", 
                     i, ret, GetLastError(), (HWND)_video_windows[i]);

			_video_windows[i].create_gdi_renderer();
			::ShowWindow(_video_windows[i], _video_windows[i]._show_wnd_cmd);
		}
	}
	else
	{
		_d3d9_renderer->create_d3d9_device(_hMain);
		for (int i = 0; i < 10; i++) {
			BOOL ret = _video_windows[i].Create(
                            0, 
                            L"", 
                            WS_CHILD | WS_BORDER | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
				            200, 
                            390, 
                            1920, 
                            1080, 
                            _hMain, 
                            NULL, 
                            hInst);

			InfoLog("set_main_window, index:%d,create ret=%d,err:%u hwnd:%u", 
                     i, ret, GetLastError(), (HWND)_video_windows[i]);

			_video_windows[i].set_d3d9_renderer(_d3d9_renderer);

			_d3d9_renderer->create_render_component(_video_windows[i], 
                                                    1920, 
                                                    1080, 
                                                    D3DFMT_YV12);

			::ShowWindow(_video_windows[i], _video_windows[i]._show_wnd_cmd);
		}
	}
}

void FRTCVideoWndMgr::set_local_stream_msid(const std::string& localStreamID)
{
	this->_local_stream_msid = localStreamID;
	this->_local_preview_uuid = localStreamID;
	set_msid(_local_preview_uuid, _local_stream_msid);
}

void FRTCVideoWndMgr::remove_main_window()
{
	AutoLock autolock(_lock);
	set_active_speaker("", true);
	for (int i = 0; i < _wnd_count; i++)
	{
		_video_windows[i].stop_video();
	}
	_user_name_map.clear();
	_msid_map.clear();
	_free_video_wnd_list.clear();
	_occupied_video_wnd_List.clear();
	_occupied_video_wnd_map.clear();
	_reserved_video_stream_list.clear();
	_d3d9_renderer->destory_d3d9_device();
	_is_sharing_content = false;
	_current_video_view = NULL;
}

void FRTCVideoWndMgr::show_video_window()
{
	AutoLock autolock(_lock);
	_current_video_view = NULL;
	_is_sharing_content = false;
	_is_full_screen = false;
	update_layout();
}

void FRTCVideoWndMgr::hide_video_window()
{
	AutoLock autolock(_lock);
	for (int i = 0; i < _wnd_count; i++) {
		::ShowWindow(_video_windows[i], SW_HIDE);
	}
}

void FRTCVideoWndMgr::set_watermark(bool enable, std::string msg)
{
	_is_watermark_enabled = enable;
	_watermark_str = msg;

	auto tmpContent = _occupied_video_wnd_List.begin();
	for (; tmpContent != _occupied_video_wnd_List.end(); tmpContent++)
	{
		VideoWnd* pContentVidWnd = *tmpContent;
		set_content_stream(pContentVidWnd->get_uuid(), pContentVidWnd);
	}
}

int FRTCVideoWndMgr::get_cells_count(const std::string& newStreamUUID)
{
	int showignCellsCount = _layout_cell_max_count + 1;
	std::string prefixContent = "VCR";
	if (newStreamUUID.compare(0, prefixContent.length(), prefixContent) == 0)
	{
		showignCellsCount++;
	}
	else
	{
		auto tmpContent = _occupied_video_wnd_List.begin();
		for (; tmpContent != _occupied_video_wnd_List.end(); tmpContent++)
		{
			VideoWnd* pContentVidWnd = *tmpContent;
			if (pContentVidWnd->get_uuid().compare(
                                    0, 
                                    kContentMSIDPrefixStr.length(), 
                                    kContentMSIDPrefixStr) == 0)
			{
				showignCellsCount++;
				break;
			}
		}
	}

	return showignCellsCount;
}


void FRTCVideoWndMgr::set_content_stream(const std::string& msid, 
                                         VideoWnd* pViewWnd)
{
	std::string prefixContent = "VCR";
	if (msid.compare(0, prefixContent.length(), prefixContent) == 0)
	{
		if (_is_watermark_enabled)
		{
			pViewWnd->set_is_content_stream(true);
			pViewWnd->set_watermark_str(_watermark_str);
		}
		else
		{
			pViewWnd->set_is_content_stream(true);
			pViewWnd->set_watermark_str("");
		}
	}
	else
	{
		pViewWnd->set_is_content_stream(false);
		pViewWnd->set_watermark_str("");
	}
}

void FRTCVideoWndMgr::show_video_stream(std::string msid, bool bFake)
{
	DebugLog("show video stream, msid=%s, fake=%d", msid.c_str(), bFake);

	AutoLock autolock(_lock);

	std::string uuid = find_uuid(msid);
	if (uuid == "" || uuid.length() == 0)
	{
		return;
	}

	bool bNotFind = true;
	VideoWnd* pVidWndToChange = NULL;
	for (auto it = _occupied_video_wnd_List.begin(); 
         it != _occupied_video_wnd_List.end(); ++it) {
		if ((*it)->check_stream_uuid(uuid))
		{
			bNotFind = false;
			pVidWndToChange = (*it);
		}
	}

	for (auto it = _reserved_video_stream_list.begin(); 
         it != _reserved_video_stream_list.end(); ++it) {
		if ((*it).uuid == uuid) {
			bNotFind = false;
			break;
		}
	}

	if (bNotFind) {
		int showignCellsCount = get_cells_count(uuid);
		if (_free_video_wnd_list.size() > 0 && 
           (_occupied_video_wnd_List.size() < showignCellsCount)) {
			VideoWnd* pVidWnd = _free_video_wnd_list.front();
			_free_video_wnd_list.pop_front();
			if (uuid == this->_local_preview_uuid) {
				pVidWnd->stop_video();
				if (bFake)
				{
					pVidWnd->start_fake_video(msid, uuid);
				}
				else
				{
					pVidWnd->start_video(TRUE, msid, uuid);
				}

				::SetWindowPos(*pVidWnd, NULL, 0, 0, 0, 0, SWP_NOOWNERZORDER);
				_occupied_video_wnd_List.push_back(pVidWnd);
				_occupied_video_wnd_map.try_emplace(uuid, pVidWnd);
			}
			else {
				pVidWnd->stop_video();
				if (bFake)
				{
					auto iter = this->_user_name_map.find(msid);
					if (iter != this->_user_name_map.end())
					{
						pVidWnd->update_user_name(iter->second);
					}
					pVidWnd->start_fake_video(msid, uuid);
				}
				else
				{
					auto iter = this->_user_name_map.find(msid);
					if (iter != this->_user_name_map.end())
					{
						pVidWnd->update_user_name(iter->second);
					}
					set_content_stream(msid, pVidWnd);
					pVidWnd->start_video(FALSE, msid, uuid);
				}

				if (_current_video_view != NULL && 
                    msid.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
				{
					_current_video_view->set_main_cell(false);
					::ShowWindow(*_current_video_view, SW_HIDE);
					_current_video_view = pVidWnd;
				}

				::SetWindowPos(*pVidWnd, NULL, 0, 0, 0, 0, SWP_NOOWNERZORDER);
				_occupied_video_wnd_List.push_back(pVidWnd);
				_occupied_video_wnd_map.try_emplace(uuid, pVidWnd);

				if (_is_sharing_content)
				{
					if (pVidWnd != _current_video_view)
						set_current_view(pVidWnd);
				}
			}
			for (int i = 0; i < _wnd_count; i++) {
				if (_is_local_video_hidden && 
                    _video_windows[i].get_msid() == _local_stream_msid) {
					::ShowWindow(_video_windows[i], SW_HIDE);
                }
				else {
					::ShowWindow(_video_windows[i], _video_windows[i]._show_wnd_cmd);
                }

				if (_video_windows[i]._show_wnd_cmd == SW_SHOW)
				{
					auto iter = this->_user_name_map.find(_video_windows[i].get_msid());
					if (iter != this->_user_name_map.end())
					{
						_video_windows[i].update_user_name(iter->second);
					}
				}
			}

			if (!(uuid != _local_preview_uuid && _is_sharing_content))
				update_layout();
		}
		else {
			DebugLog("put msid %s, uuid = %s, bFake =%d,into reserved_video_stream_list", 
                      msid.c_str(), uuid.c_str(), bFake);

			STEAM_INFO _streamInfo;
			_streamInfo.uuid = uuid;
			_streamInfo._is_video_ready = !bFake;
			_reserved_video_stream_list.push_back(_streamInfo);
		}
	}
	else if (pVidWndToChange != NULL)
	{
		BOOL bLocal = msid == this->_local_stream_msid ? TRUE : FALSE;

		pVidWndToChange->stop_video();
		if (!bLocal)
		{
			auto iter = this->_user_name_map.find(msid);
			if (iter != this->_user_name_map.end())
			{
				pVidWndToChange->update_user_name(iter->second);
			}
		}

		if (bFake)
		{
			pVidWndToChange->start_fake_video(msid, uuid);
		}
		else
		{
			if (!bLocal)
			{
				set_content_stream(msid, pVidWndToChange);
			}
			pVidWndToChange->start_video(bLocal, msid, uuid);
		}

		if (_is_sharing_content && (msid != _local_stream_msid)) {
			if (_current_video_view != pVidWndToChange)
				set_current_view(pVidWndToChange);
		}


		if (_active_speaker_uuid == uuid)
		{
			// active speaker stream is a new arrived stream, not show on the main cell.
			auto it = _occupied_video_wnd_List.begin();
			if ((*it)->get_uuid() != uuid && 
               (*it)->get_uuid().compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) != 0)
			{
				update_layout();
			}
		}
	}
}

void FRTCVideoWndMgr::set_video_data_zero_check_timer(std::string msid)
{
	AutoLock autolock(_lock);

	std::string uuid = find_uuid(msid);
	if (uuid == "" || uuid.length() == 0)
	{
		DebugLog("uuid is null, msid = %s", uuid.c_str(), msid.c_str());
		return;
	}

	DebugLog("set video data zero check timer, uuid=%s, msid=%s",
		uuid.c_str(), msid.c_str());

	bool bNotFind = true;
	VideoWnd* pVidWndToChange = NULL;
	for (auto it = _occupied_video_wnd_List.begin(); 
         it != _occupied_video_wnd_List.end(); ++it) {
		if ((*it)->check_stream_uuid(uuid))
		{
			pVidWndToChange = (*it);
			pVidWndToChange->set_video_data_zero_check_timer();
			break;
		}
	}
}

void FRTCVideoWndMgr::remove_video_stream(std::string msid, bool bFake)
{
	AutoLock autolock(_lock);
	if (!_is_processing_layout_change)
		return;

	std::string uuid = find_uuid(msid);
	if (uuid == "" || uuid.length() == 0)
	{
		ErrorLog("remove_video_stream failed, uuid is null, msid=%s", 
                  msid.c_str());
		return;
	}

	DebugLog("remove_video_stream, uuid=%s, msid=%s", uuid.c_str(), msid.c_str());

	VideoWnd* pVidWnd = NULL;
	bool bFullSize = false;
	bool bWakeUpContent = false;
	auto it = _occupied_video_wnd_List.begin();
	for (; it != _occupied_video_wnd_List.end(); ++it) {
		if ((*it)->check_stream_uuid(uuid))
		{
			DebugLog("remove_video_stream, msid = %s, uuid = %s", 
                      msid.c_str(), uuid.c_str());
			pVidWnd = *it;

			// if remove the full mode video
			if (_current_video_view != NULL)
			{
				DebugLog("currentView uuid = %s, _is_grid_mode = %d", 
                         _current_video_view->get_uuid().c_str(), 
                         _is_grid_mode ? 1 : 0);

				bFullSize = true;

				// remove content && the content is _current_video_view && the layout mode is 3*3 if no content
				if (_is_grid_mode && 
                   (msid == _current_video_view->get_msid()) && 
                   (_current_video_view->get_uuid().compare(0, 3, "VCR") == 0))
				{
					DebugLog("currentView uuid=%s, _is_grid_mode=%d, set current to NULL", 
                              _current_video_view->get_uuid().c_str(), 
                              _is_grid_mode ? 1 : 0);

					_current_video_view->set_main_cell(false);
					set_current_view(_current_video_view);
					bFullSize = false;
				}
			}
			break;
		}
	}

	auto itID = _reserved_video_stream_list.begin();
	while (itID != _reserved_video_stream_list.end())
	{
		if (itID->uuid == uuid)
		{
			itID = _reserved_video_stream_list.erase(itID);
		}
		else
		{
			itID++;
		}
	}

	if (pVidWnd != NULL) {
		pVidWnd->stop_video();

		if (_reserved_video_stream_list.size() > 0) {
			STEAM_INFO _streamInfo = _reserved_video_stream_list.front();
			std::string uuid = _streamInfo.uuid;
			_reserved_video_stream_list.pop_front();
			std::string msid = "";
			auto itMap = this->_msid_map.find(uuid);
			if (itMap != this->_msid_map.end())
			{
				msid = itMap->second;
				if (uuid.compare(0, kContentMSIDPrefixStr.length(), kContentMSIDPrefixStr) == 0)
				{
					bWakeUpContent = true;
				}

				if (uuid == this->_local_preview_uuid) {
					DebugLog("remove_video_stream  start local video");
					pVidWnd->start_video(TRUE, msid, uuid);
				}
				else {
					auto iter = this->_user_name_map.find(msid);
					if (iter != this->_user_name_map.end())
					{
						pVidWnd->update_user_name(iter->second);
					}

					std::string tmpsID = "CID1-82935167-b4a8-4b3f-93fa-be83b58fb66c";
					if (msid.length() >= tmpsID.length())
					{
						DebugLog("audioOnly, msid = %s", msid.c_str());
						pVidWnd->start_fake_video(msid, uuid);
					}
					else
					{
						if (_streamInfo._is_video_ready)
						{
							DebugLog("video ready, msid = %s", msid.c_str());
							set_content_stream(msid, pVidWnd);
							pVidWnd->start_video(FALSE, msid, uuid);
						}
						else
						{
							DebugLog("video not ready , msid = %s", msid.c_str());
							pVidWnd->start_fake_video(msid, uuid);
						}
					}
				}
			}
			else
			{
				_occupied_video_wnd_List.erase(it);
				_occupied_video_wnd_map.erase(uuid);
				pVidWnd->show_audio_muted_icon_wnd(false);
				pVidWnd->show_pinned_icon_wnd(false);
				_free_video_wnd_list.push_back(pVidWnd);

				ShowWindow(*pVidWnd, SW_HIDE);
				pVidWnd->update_user_name("");
			}
		}
		else {
			_occupied_video_wnd_List.erase(it);
			_occupied_video_wnd_map.erase(uuid);
			pVidWnd->show_audio_muted_icon_wnd(false);
			pVidWnd->show_pinned_icon_wnd(false);
			_free_video_wnd_list.push_back(pVidWnd);
			ShowWindow(*pVidWnd, SW_HIDE);
			pVidWnd->update_user_name("");
		}
	}

	DebugLog("bFullSize = %d, bWakeUpContent = %d.", 
              bFullSize ? 1 : 0, bWakeUpContent ? 1 : 0);

	if (!bFullSize)
	{
		for (int i = 0; i < _wnd_count; i++) {
			if (_is_local_video_hidden && 
                _video_windows[i].get_msid() == _local_stream_msid)
            {
				::ShowWindow(_video_windows[i], SW_HIDE);
            }
			else
			{
				::ShowWindow(_video_windows[i], _video_windows[i]._show_wnd_cmd);
				if (_video_windows[i].get_msid() == _local_stream_msid)
				{
					DebugLog("remove video stream, start local video");
				}
			}

			if (_video_windows[i]._show_wnd_cmd == SW_SHOW)
			{
				auto iter = this->_user_name_map.find(_video_windows[i].get_msid());
				if (iter != this->_user_name_map.end())
				{
					_video_windows[i].update_user_name(iter->second);
				}
			}
		}
	}
	else
	{
		auto it = _occupied_video_wnd_List.begin();

		if (bWakeUpContent)
		{
			for (; it != _occupied_video_wnd_List.end(); it++)
			{
				VideoWnd* pVidWnd = *it;
				if (pVidWnd->get_uuid().compare(0, 
                                    kContentMSIDPrefixStr.length(), 
                                    kContentMSIDPrefixStr) == 0)
				{
					_current_video_view->set_main_cell(false);
					::ShowWindow(*_current_video_view, SW_HIDE);
					_current_video_view = pVidWnd;
					::ShowWindow(*_current_video_view, SW_SHOW);
					break;
				}
				else
				{
					continue;
				}
			}
		}
		else
		{
			if (_current_video_view->get_uuid().compare(0, 
                                            kContentMSIDPrefixStr.length(), 
                                            kContentMSIDPrefixStr) != 0)
			{
				std::string mainCelluuid;
				if (_active_speaker_uuid == "")
				{
					mainCelluuid = _first_request_stream_uuid;
				}
				else
				{
					mainCelluuid = _active_speaker_uuid;
				}
				for (; it != _occupied_video_wnd_List.end(); it++)
				{
					VideoWnd* pVidWnd = *it;
					if (pVidWnd->get_uuid() == mainCelluuid) // find main cell stream uuid.
					{
						_current_video_view->set_main_cell(false);
						::ShowWindow(*_current_video_view, SW_HIDE);
						_current_video_view = pVidWnd;
						::ShowWindow(*_current_video_view, SW_SHOW);
						break;
					}
					else
					{
						continue;
					}
				}
			}
		}

		if (it == _occupied_video_wnd_List.end() && 
            _occupied_video_wnd_List.size() > 0)
		{
			_current_video_view = *(_occupied_video_wnd_List.begin());
		}
	}

	update_layout();
}

void FRTCVideoWndMgr::set_layout_cell_max_count(int maxLayoutCells)
{
	_layout_cell_max_count = maxLayoutCells;
}

void FRTCVideoWndMgr::set_main_cell_size()
{
	if (_occupied_video_wnd_List.size() > 0)
	{
		if (_current_video_view != NULL)
		{
			::SetWindowPos(*_current_video_view, 
                           NULL, 
                           0, 
                           0, 
                           1280, 
                           720, 
                           SWP_NOOWNERZORDER);
		}
		else
		{
			auto it = _occupied_video_wnd_List.begin();
			VideoWnd* pVidWnd = *it;
			::SetWindowPos(*pVidWnd, NULL, 0, 0, 1280, 720, SWP_NOOWNERZORDER);
			if (_is_full_screen)
				pVidWnd->set_main_cell(true);
			else
				pVidWnd->set_main_cell(false);

		}
	}
}

void FRTCVideoWndMgr::on_main_window_size_changed(LONG left, 
                                                  LONG top, 
                                                  LONG right, 
                                                  LONG bottom)
{
	AutoLock autolock(_lock);
	if (right != left && bottom != top) {
		_main_rect.left = left;
		_main_rect.top = top;
		_main_rect.right = right;
		_main_rect.bottom = bottom;
	}
	update_layout();
}

void FRTCVideoWndMgr::update_layout()
{
	if (!_is_processing_layout_change)
		return;


	if (_occupied_video_wnd_List.size() == 0)
		return;

	bool hasContent = false;
	int videoCount = _occupied_video_wnd_List.size();

	auto tmpContent = _occupied_video_wnd_List.begin();
	for (; tmpContent != _occupied_video_wnd_List.end(); tmpContent++)
	{
		VideoWnd* pContentVidWnd = *tmpContent;
		if (pContentVidWnd->get_uuid().compare(0, 
                                    kContentMSIDPrefixStr.length(), 
                                    kContentMSIDPrefixStr) == 0)
		{
			hasContent = true;
			break;
		}
	}

	if (!_is_grid_mode || hasContent || _is_sharing_content)
	{
		switch_to_1n5_layout();
	}
	else
	{
		switch_to_grid_layout();
	}
}

void FRTCVideoWndMgr::switch_to_1n5_layout()
{
	int w = _main_rect.right - _main_rect.left;
	int h = _main_rect.bottom - _main_rect.top;

	if (!(_is_sharing_content) && !_is_full_screen)
		h = h * 5 / 6;

	InfoLog("switch_to_1n5_layout, window width = %d, height = %d, hide local "
            "video = %s", 
             _main_rect.right - _main_rect.left, 
             _main_rect.bottom - _main_rect.top, 
             _is_local_video_hidden ? "true" : "false");

	int h1, w1, hv, wv;
	if (w * 9 > h * 16)
	{
		h1 = h;
		w1 = h1 * 16 / 9;
	}
	else
	{
		w1 = w;
		h1 = w1 * 9 / 16;
	}

	h = _main_rect.bottom - _main_rect.top;
	int x = (w - w1) / 2;
	int y = (h - h1) / 2;

	hv = h1;
	wv = w1;

	InfoLog("switch_to_1n5_layout, _occupied_video_wnd_List.size() = %d", 
             _occupied_video_wnd_List.size());

	if (_current_video_view == NULL)
	{
		hv = h1 / 5; 
		wv = w1 / 5; 
		x = (w - w1) / 2;
		y = hv;										

		if (_is_full_screen)
		{
			hv = _small_cell_height;
			wv = _small_cell_width;
			y = (h - h1) / 2;
		}

		if (_is_sharing_content)
		{
			x = 0;
			y = 0;
		}

		bool hasContent = false;
		bool hasLocalVideo = false;
		int videoCount = _occupied_video_wnd_List.size();
		int index = 0;
		int contentIndex = -1;
		int cellCustomization = -1;
		int localIndex = -1;

		auto tmpContent = _occupied_video_wnd_List.begin();
		for (; tmpContent != _occupied_video_wnd_List.end(); tmpContent++)
		{
			VideoWnd* pContentVidWnd = *tmpContent;
			if (pContentVidWnd->get_uuid() == _cell_customization_uuid && 
                _cell_customization_uuid != "")
			{
				pContentVidWnd->show_pinned_icon_wnd(true);
			}
			else
			{
				pContentVidWnd->show_pinned_icon_wnd(false);
			}

			pContentVidWnd->set_border(false);
			if (pContentVidWnd->get_uuid().compare(0, 
                                    kContentMSIDPrefixStr.length(), 
                                    kContentMSIDPrefixStr) == 0)
			{
				contentIndex = index;
				hasContent = true;
			}

			auto it = this->_user_name_map.find(pContentVidWnd->get_msid());
			if (it != this->_user_name_map.end())
			{
				DebugLog("index = %d, name= %s, msid = %s, uuid = %s", 
                          index, 
                          it->second.c_str(), 
                          pContentVidWnd->get_msid().c_str(), 
                          pContentVidWnd->get_uuid().c_str());
			}
			else
			{
				DebugLog("msid not find, index = %d, msid = %s, uuid = %s", 
                          index, 
                          pContentVidWnd->get_msid().c_str(), 
                          pContentVidWnd->get_uuid().c_str());
			}

			index++;
		}

		if (contentIndex != -1)
		{
			int i = 0;
			auto tmp1 = _occupied_video_wnd_List.begin();
			std::list<VideoWnd*>::iterator tmp2;
			auto tmp = _occupied_video_wnd_List.begin();
			for (; tmp1 != _occupied_video_wnd_List.end(); tmp++)
			{
				if (contentIndex == i)
				{
					tmp2 = tmp;
					break;
				}
				i++;
			}

			std::swap(*tmp1, *tmp2);
		}

		auto tmpLast = _occupied_video_wnd_List.rbegin();
		auto tmpLocal = _occupied_video_wnd_List.begin();
		index = 0;
		for (; tmpLocal != _occupied_video_wnd_List.end(); tmpLocal++)
		{
			if ((*tmpLocal)->get_uuid() == _local_preview_uuid)
			{
				localIndex = index;
				break;
			}
			index++;
		}

		if (localIndex != -1)
		{
			std::swap(*tmpLast, *tmpLocal);
			hasLocalVideo = true;
		}

		{
			int pinVideoIndex = -1;
			if (_cell_customization_uuid != "")
			{
				DebugLog("cellCustomization != NULL, _cell_customization_uuid = %s", 
                          _cell_customization_uuid.c_str());

				auto tmpPin = _occupied_video_wnd_List.begin();
				if (hasContent)
				{
					tmpPin++;
				}

				auto tmp = _occupied_video_wnd_List.begin();
				index = 0;
				for (tmp; tmp != _occupied_video_wnd_List.end(); tmp++)
				{
					if ((*tmp)->get_uuid() == _cell_customization_uuid)
					{
						pinVideoIndex = index;
						break;
					}
					index++;
				}

				if (pinVideoIndex != -1)
				{
					if (tmpPin != _occupied_video_wnd_List.end())
						std::swap(*tmpPin, *tmp);
				}
			}
		}

		{
			int speakerIndex = -1;
			std::string mainCelluuid;
			if (_active_speaker_uuid == "")
			{
				mainCelluuid = _first_request_stream_uuid;
				DebugLog("active speaker is NULL, mainCelluuid=%s", mainCelluuid.c_str());
			}
			else
			{
				mainCelluuid = _active_speaker_uuid;
				DebugLog("active speaker exist, mainCelluuid=%s", mainCelluuid.c_str());
			}

			auto tmpFirst = _occupied_video_wnd_List.begin();
			auto tmpSpk = _occupied_video_wnd_List.begin();
			index = 0;
			for (tmpSpk; tmpSpk != _occupied_video_wnd_List.end(); tmpSpk++)
			{
				if ((*tmpSpk)->get_uuid() == mainCelluuid)
				{
					speakerIndex = index;
					break;
				}
				index++;
			}

			if (speakerIndex != -1)
			{
				if (!hasContent)
				{
					if (_cell_customization_uuid == "")
					{
						std::swap(*tmpFirst, *tmpSpk);
					}
					else
					{
						if (_msid_map.size() > 2 && _cell_customization_uuid != _active_speaker_uuid &&
							_active_speaker_uuid != "" && _current_lecture_uuid != (*tmpSpk)->get_uuid()) {
							(*tmpSpk)->set_border(true);
						}
						else
						{
							std::string name;
							(*tmpSpk)->get_user_name(&name);
							DebugLog("Will not show active speaker border for %s, _msid_map.size() = %d, _cell_customization_uuid is %s, _active_speaker_uuid is %s", name.c_str(), _msid_map.size(), _cell_customization_uuid.c_str(), _active_speaker_uuid.c_str());
						}
					}
				}
				else
				{
					if (_msid_map.size() > 2 && (*tmpSpk)->get_uuid().compare(0,
						kContentMSIDPrefixStr.length(),
						kContentMSIDPrefixStr) != 0 && _current_lecture_uuid != (*tmpSpk)->get_uuid()) {
						(*tmpSpk)->set_border(true);
					}
					else
					{
						std::string name;
						(*tmpSpk)->get_user_name(&name);
						DebugLog("Will not show active speaker border for %s while content sharing, _msid_map.size() = %d, uuid is %s", name.c_str(), _msid_map.size(), (*tmpSpk)->get_uuid().c_str());
					}
				}
			}
		}

		std::list<VideoWnd*> _usedVideoWndListTmp;
		_usedVideoWndListTmp.assign(_occupied_video_wnd_List.begin(), 
                                    _occupied_video_wnd_List.end());

		int offset = 0;
		int showingVideoCount = _usedVideoWndListTmp.size();
		showingVideoCount = showingVideoCount < 6 ? showingVideoCount : 6;
		if (_is_local_video_hidden && hasLocalVideo)
			showingVideoCount = showingVideoCount - 1;

		auto it = _usedVideoWndListTmp.begin();
		if (showingVideoCount == 1)
		{
			VideoWnd* pVidWnd = *it;

			InfoLog("switch to 1n5 layout, video count=1, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s, is sharing=%d",
				_main_rect.left + x, _main_rect.top + y, w1, h1,
				pVidWnd->is_local_video() ? 1 : 0,
				pVidWnd->get_msid().c_str(),
				pVidWnd->get_uuid().c_str(),
				_is_sharing_content ? 1 : 0);

			BOOL ret = ::SetWindowPos(
				*pVidWnd,
				NULL,
				_main_rect.left + (_main_rect.right - _main_rect.left - w1) / 2,
				_main_rect.top + (_main_rect.bottom - _main_rect.top - h1) / 2,
				w1,
				h1,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}

			if (!hasContent && videoCount == 2)
			{
				ShowWindow(*(_occupied_video_wnd_List.back()), SW_HIDE);
			}

			pVidWnd->set_icon_wnd_position(w1, h1);

			return;
		}

		if (showingVideoCount > 1) {
			VideoWnd* pVidWnd = *it;
			it++;

			InfoLog("switch to 1n5 layout, first, video count>1, x=%d, y=%d, "
				"wv=%d, hv=%d, local=%d, msid=%s, uuid=%s, is sharing=%d",
				_main_rect.left + x, _main_rect.top + y, w1, h1,
				pVidWnd->is_local_video() ? 1 : 0,
				pVidWnd->get_msid().c_str(),
				pVidWnd->get_uuid().c_str(),
				_is_sharing_content ? 1 : 0);

			BOOL ret = ::SetWindowPos(*pVidWnd,
				HWND_BOTTOM,
				_main_rect.left + x,
				_main_rect.top + y,
				w1,
				h1,
				SWP_NOOWNERZORDER);

			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}

			pVidWnd->set_main_cell(true);
			pVidWnd->set_icon_wnd_position(w1, h1);
		}

		if (videoCount > 6)
		{
			auto itHide = _usedVideoWndListTmp.begin();
			auto itShow = _usedVideoWndListTmp.rbegin();
			int i = 0;
			while (i < 5)
			{
				itHide++;
				i++;
			}

			while (*itHide != *itShow)
			{
				BOOL ret = ::SetWindowPos(*(*itHide),
					NULL,
					0,
					0,
					0,
					0,
					SWP_NOOWNERZORDER);
				if (!ret)
				{
					WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
						GetLastError());
				}

				itHide++;
			}
		}

		if (_is_sharing_content && showingVideoCount > 1)
			return;

		switch (showingVideoCount) {
		case 2:
			offset = (w1 - wv) / 2;
			break;
		case 3:
			offset = (w1 - 2 * wv) / 2;
			break;
		case 4:
			offset = (w1 - 3 * wv) / 2;
			break;
		case 5:
			offset = (w1 - 4 * wv) / 2;
			break;
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
			offset = (w1 - 5 * wv) / 2;
			break;
		default:
			break;
		}

		int smallVideoY = 0;
		int smallCellCount = 0;
		if (showingVideoCount > 2) {
			VideoWnd* pVidWnd = *it;
			it++;

			InfoLog("switch to 1n5 layout, video count>2, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x + offset + smallCellCount * wv,
				_main_rect.top + (h - y - hv), wv, hv,
				pVidWnd->is_local_video() ? 1 : 0,
				pVidWnd->get_msid().c_str(),
				pVidWnd->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWnd,
				NULL,
				_main_rect.left + x + offset + smallCellCount * wv,
				smallVideoY,
				wv,
				hv,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}

			pVidWnd->set_main_cell(false);
			smallCellCount++;
			pVidWnd->set_icon_wnd_position(wv, hv);
		}

		if (showingVideoCount > 3) {
			VideoWnd* pVidWnd = *it;
			it++;

			InfoLog("switch to 1n5 layout, video count>3, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x + offset + smallCellCount * wv,
				_main_rect.top + (h - y - hv), wv, hv,
				pVidWnd->is_local_video() ? 1 : 0,
				pVidWnd->get_msid().c_str(),
				pVidWnd->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWnd,
				NULL,
				_main_rect.left + x + offset + smallCellCount * wv,
				smallVideoY,
				wv,
				hv,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}

			pVidWnd->set_main_cell(false);
			smallCellCount++;
			pVidWnd->set_icon_wnd_position(wv, hv);
		}

		if (showingVideoCount > 4) {
			VideoWnd* pVidWnd = *it;
			it++;

			InfoLog("switch to 1n5 layout, video count>4, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x + offset + smallCellCount * wv,
				_main_rect.top + (h - y - hv), wv, hv,
				pVidWnd->is_local_video() ? 1 : 0,
				pVidWnd->get_msid().c_str(),
				pVidWnd->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWnd,
				NULL,
				_main_rect.left + x + offset + smallCellCount * wv,
				smallVideoY,
				wv,
				hv,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout SetWindowPos failed, err=%u",
					GetLastError());
			}

			pVidWnd->set_main_cell(false);
			smallCellCount++;
			pVidWnd->set_icon_wnd_position(wv, hv);
		}

		if (showingVideoCount > 5) {
			VideoWnd* pVidWnd = *it;
			it++;

			InfoLog("switch to 1n5 layout, video count>5, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x + offset + smallCellCount * wv,
				_main_rect.top + (h - y - hv),
				wv,
				hv,
				pVidWnd->is_local_video() ? 1 : 0,
				pVidWnd->get_msid().c_str(),
				pVidWnd->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWnd,
				NULL,
				_main_rect.left + x + offset + smallCellCount * wv,
				smallVideoY,
				wv,
				hv,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}

			pVidWnd->set_main_cell(false);
			smallCellCount++;
			pVidWnd->set_icon_wnd_position(wv, hv);
		}

		VideoWnd* pVidWndLast = NULL;
		if (_is_local_video_hidden)
		{
			pVidWndLast = _usedVideoWndListTmp.back();
			ShowWindow(*pVidWndLast, SW_HIDE);
			pVidWndLast = *it;
		}
		else
		{
			pVidWndLast = _usedVideoWndListTmp.back();
			ShowWindow(*pVidWndLast, SW_SHOW);
		}

		if (showingVideoCount > 2)
		{
			InfoLog("switch to 1n5 layout, last, video count>2, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x + offset + smallCellCount * wv,
				_main_rect.top + (h - y - hv), wv, hv,
				pVidWndLast->is_local_video() ? 1 : 0,
				pVidWndLast->get_msid().c_str(),
				pVidWndLast->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWndLast,
				NULL,
				_main_rect.left + x + offset + smallCellCount * wv,
				smallVideoY,
				wv,
				hv,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}
			pVidWndLast->set_main_cell(false);
			pVidWndLast->set_icon_wnd_position(wv, hv);
		}
		else if (showingVideoCount > 1)
		{
			InfoLog("switch to 1n5 layout, last, video count>1, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x + w1 / 2 - wv / 2,
				_main_rect.top + (h - y - hv), wv, hv,
				pVidWndLast->is_local_video() ? 1 : 0,
				pVidWndLast->get_msid().c_str(),
				pVidWndLast->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWndLast,
				NULL,
				_main_rect.left + x + (w1 - wv) / 2,
				smallVideoY,
				wv,
				hv,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}
			pVidWndLast->set_main_cell(false);
			pVidWndLast->set_icon_wnd_position(wv, hv);
		}
		else
		{
			InfoLog("switch_to_1n5_layout, last, video count=1, x=%d, y=%d, wv=%d, "
				"hv=%d, local=%d, msid=%s, uuid=%s",
				_main_rect.left + x, _main_rect.top + y, w1, h1,
				pVidWndLast->is_local_video() ? 1 : 0,
				pVidWndLast->get_msid().c_str(),
				pVidWndLast->get_uuid().c_str());

			BOOL ret = ::SetWindowPos(*pVidWndLast,
				NULL,
				_main_rect.left + x,
				_main_rect.top + y,
				w1,
				h1,
				SWP_NOOWNERZORDER);
			if (!ret)
			{
				WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
					GetLastError());
			}

			pVidWndLast->set_main_cell(true);
			pVidWndLast->set_icon_wnd_position(w1, h1);
		}
	}
	else
	{
		find_current_view_wnd();

		InfoLog("switch to 1n5 layout, full video size mode, x=%d, y=%d, wv=%d, "
			"hv=%d, local=%d, msid=%s, uuid=%s",
			_main_rect.left + x, _main_rect.top + y, wv, hv,
			_current_video_view->is_local_video() ? 1 : 0,
			_current_video_view->get_msid().c_str(),
			_current_video_view->get_uuid().c_str());

		BOOL ret = ::SetWindowPos(*_current_video_view,
			NULL,
			_main_rect.left + x,
			_main_rect.top + y,
			wv,
			hv,
			SWP_NOOWNERZORDER);
		if (!ret)
		{
			WarnLog("switch to 1n5 layout, SetWindowPos failed, err=%u",
				GetLastError());
		}

		::ShowWindow(*_current_video_view, SW_SHOW);
		_current_video_view->set_main_cell(true);
		_current_video_view->set_icon_wnd_position(wv, hv);
	}
}

void FRTCVideoWndMgr::find_current_view_wnd()
{
	std::string currentViewUUID = "";

	if (_cell_customization_uuid == "")
	{
		currentViewUUID = _active_speaker_uuid;
	}
	else
	{
		currentViewUUID = _cell_customization_uuid;
	}

	if (currentViewUUID == "")
		return;

	if (_is_full_screen &&
		_current_video_view != NULL &&
		!(_current_video_view->get_uuid().compare(0,
			kContentMSIDPrefixStr.length(),
			kContentMSIDPrefixStr) == 0))
	{
		for (int i = 0; i < _wnd_count; i++)
		{
			if (_video_windows[i].get_uuid() == currentViewUUID &&
				_current_video_view->get_uuid() != currentViewUUID)
			{
				_current_video_view->set_main_cell(false);
				::ShowWindow(*_current_video_view, SW_HIDE);
				_current_video_view = &_video_windows[i];
				::ShowWindow(_video_windows[i], _video_windows[i]._show_wnd_cmd);
				std::string name_;
				_video_windows[i].get_user_name(&name_);

				DebugLog("find_current_view_wnd show wnd %s", name_.c_str());

				auto iter = this->_user_name_map.find(_video_windows[i].get_msid());
				if (iter != this->_user_name_map.end())
				{
					_video_windows[i].update_user_name(iter->second);
				}

				break;
			}
		}
	}
}

void FRTCVideoWndMgr::switch_to_grid_layout()
{
	RECT rc;
	rc.left = _main_rect.left;
	rc.top = _main_rect.top;
	rc.right = _main_rect.right;
	rc.bottom = _main_rect.bottom;
	int w = rc.right - rc.left;
	int h = rc.bottom - rc.top;

	InfoLog("switch to grid layout, window width=%d, height=%d, hide local video=%s",
		_main_rect.right - _main_rect.left,
		_main_rect.bottom - _main_rect.top,
		_is_local_video_hidden ? "true" : "false");

	/*	if (!_is_sharing_content  && (_main_rect.right - _main_rect.left) < 1280)
		{
			InfoLog("switch to grid layout, window width=%d, height=%d, share content=%d",
					 _main_rect.right - _main_rect.left,
					 _main_rect.bottom - _main_rect.top,
					 _is_sharing_content ? 1 : 0);
			return;
		}
	*/

	int h1, w1;
	if (w * 9 > h * 16)
	{
		h1 = h;
		//w1 = h1 * 4 /3;
		w1 = h1 * 16 / 9;
	}
	else
	{
		w1 = w;
		h1 = w1 * 9 / 16;
	}

	int x = (w - w1) / 2;
	int y = (h - h1) / 2;

	int hv = h1, wv = w1;

	//if (_current_video_view == NULL)
	{
		int remoteVideoCount = 0;
		int videoCount = _occupied_video_wnd_List.size();

		InfoLog("switch_to_grid_layout, window width=%d, height=%d, video count=%d", 
                 _main_rect.right - _main_rect.left, 
                 _main_rect.bottom - _main_rect.top, 
                 videoCount);

		InfoLog("switch to grid layout, video count=%d", videoCount);

		if (videoCount < 1)
			return;

		auto tmpBorder = _occupied_video_wnd_List.begin();
		for (; tmpBorder != _occupied_video_wnd_List.end(); tmpBorder++)
		{
			(*tmpBorder)->set_border(false);

			if ((*tmpBorder)->get_uuid() == _cell_customization_uuid && 
                _cell_customization_uuid != "")
			{
				(*tmpBorder)->show_pinned_icon_wnd(true);
			}
			else
			{
				(*tmpBorder)->show_pinned_icon_wnd(false);
			}
		}

		int index = 0;
		int localIndex = -1;
		bool hasLocal = false;
		auto tmpLastWnd = _occupied_video_wnd_List.rbegin();
		auto tmpLocalWnd = _occupied_video_wnd_List.begin();
		for (; tmpLocalWnd != _occupied_video_wnd_List.end(); tmpLocalWnd++)
		{
			if ((*tmpLocalWnd)->get_uuid() == _local_preview_uuid)
			{
				localIndex = index;
				hasLocal = true;
				break;
			}
			index++;
		}

		if (localIndex != -1)
		{
			std::swap(*tmpLocalWnd, *tmpLastWnd);
		}

		// find the pin video
		{
			int pinVideoIndex = -1;
			if (_cell_customization_uuid != "")
			{
				DebugLog("cellCustomization != NULL, _cell_customization_uuid = %s", 
                          _cell_customization_uuid.c_str());

				auto tmpPin = _occupied_video_wnd_List.begin();
				auto tmp = _occupied_video_wnd_List.begin();
				index = 0;
				for (tmp; tmp != _occupied_video_wnd_List.end(); tmp++)
				{
					if ((*tmp)->get_uuid() == _cell_customization_uuid)
					{
						pinVideoIndex = index;
						break;
					}
					index++;
				}

				if (pinVideoIndex != -1)
				{
					std::swap(*tmpPin, *tmp);
				}
			}
		}

		int speakerIndex = -1;
		std::string mainCelluuid = _active_speaker_uuid;

		DebugLog("active speaker exist, mainCelluuid=%s", mainCelluuid.c_str());

		auto tmpFirst = _occupied_video_wnd_List.begin();
		auto tmpSpk = _occupied_video_wnd_List.begin();
		index = 0;
		for (tmpSpk; tmpSpk != _occupied_video_wnd_List.end(); tmpSpk++)
		{
			if ((*tmpSpk)->get_uuid() == mainCelluuid)
			{
				speakerIndex = index;
				break;
			}

			index++;
		}

		if (_msid_map.size() > 2 && speakerIndex != -1 && _current_lecture_uuid != (*tmpSpk)->get_uuid())
		{
			(*tmpSpk)->set_border(true);
		}
		else
		{
			if (speakerIndex != -1)
			{
				std::string name;
				(*tmpSpk)->get_user_name(&name);
				DebugLog("Will not show active speaker border for %s, _msid_map.size() = %d, speakerIndex is %d", name.c_str(), _msid_map.size(), speakerIndex);
			}
		}

		auto tmpwnd = _occupied_video_wnd_List.begin();
		for (; tmpwnd != _occupied_video_wnd_List.end(); tmpwnd++)
		{
			VideoWnd* tmp = *tmpwnd;
			tmp->set_main_cell(false);

			InfoLog("switch to grid layout, msid=%s", tmp->get_msid().c_str());

			tmp->stop_timer();
		}

		auto it = _occupied_video_wnd_List.begin();
		if (videoCount == 1)
		{
			VideoWnd* pVidWnd = *it;
			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y,
				w1,
				h1,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(w1, h1);
			if (hasLocal)
			{
				pVidWnd->start_timer();
				return;
			}
		}

		if (hasLocal)
			remoteVideoCount = videoCount - 1;
		else
			remoteVideoCount = videoCount;

		if (remoteVideoCount == 1) {
			VideoWnd* pVidWnd = *it;

			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y,
				w1,
				h1,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(w1, h1);
		}
		else if (remoteVideoCount == 2)
		{
			VideoWnd* pVidWnd = *it;
			int width = w1 / 2;
			int height = h1 / 2;
			y = y + h1 / 4;

			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
			it++;
			pVidWnd = *it;

			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x + width,
				rc.top + y,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
		}
		else if (remoteVideoCount == 3)
		{
			VideoWnd* pVidWnd = *it;
			int width = w1 / 2;
			int height = h1 / 2;

			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
			it++;
			pVidWnd = *it;

			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x + width,
				rc.top + y,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
			it++;
			pVidWnd = *it;

			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y + height,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
		}
		else if (remoteVideoCount == 4)
		{
			VideoWnd* pVidWnd = *it;
			int width = w1 / 2;
			int height = h1 / 2;
			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
			it++;
			pVidWnd = *it;
			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x + width,
				rc.top + y,
				width,
				height,
				SWP_NOOWNERZORDER);

			pVidWnd->set_icon_wnd_position(width, height);
			it++;
			pVidWnd = *it;
			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x,
				rc.top + y + height,
				width,
				height,
				SWP_NOOWNERZORDER);
			it++;
			pVidWnd = *it;
			::SetWindowPos(*pVidWnd,
				NULL,
				rc.left + x + width,
				rc.top + y + height,
				width,
				height,
				SWP_NOOWNERZORDER);
			pVidWnd->set_icon_wnd_position(width, height);
		}
		else if (remoteVideoCount >= 5)
		{
			int width = w1 / 3;
			int height = h1 / 3;
			int showedCount = 0;
			while (showedCount < remoteVideoCount)
			{
				VideoWnd* pVidWnd = *it;
				int row = showedCount / 3;
				int colum = showedCount % 3;

				::SetWindowPos(*pVidWnd,
					HWND_BOTTOM,
					rc.left + x + width * colum,
					rc.top + y + height * row,
					width,
					height,
					SWP_NOOWNERZORDER);

				InfoLog("switch to grid layout, showedCount=%d, row=%d, colum=%d, "
					"x=%d, y=%d, wv=%d, hv=%d, local=%d, msid=%s, uuid=%s",
					showedCount, row, colum,
					rc.left + x + width * colum,
					rc.top + y + height * row,
					width, height,
					pVidWnd->is_local_video() ? 1 : 0,
					pVidWnd->get_msid().c_str(),
					pVidWnd->get_uuid().c_str());

				pVidWnd->set_icon_wnd_position(width, height);
				showedCount++;
				it++;
			}
		}

		if (hasLocal)
		{
			VideoWnd* pVidWnd = _occupied_video_wnd_List.back();
			if (!_is_local_video_hidden)
			{
				std::string name;
				pVidWnd->get_user_name(&name);
				DebugLog("switch to grid layout, user name=%s", name.c_str());

				ShowWindow(*pVidWnd, SW_SHOW);

				::SetWindowPos(*pVidWnd,
					NULL,
					rc.right - 256,
					rc.top,
					256,
					144,
					SWP_NOOWNERZORDER);

				pVidWnd->set_icon_wnd_position(256, 144);
			}
			else
			{
				ShowWindow(*pVidWnd, SW_HIDE);
			}
		}

		auto itTmp = _occupied_video_wnd_List.begin();
		for (; itTmp != _occupied_video_wnd_List.end(); itTmp++)
		{
			(*itTmp)->start_timer();
		}
	}
}

void FRTCVideoWndMgr::setShareContent(bool bShare)
{
	_is_sharing_content = bShare;
	if (!_is_sharing_content)
	{
		set_current_view(_current_video_view);
	}
}

void FRTCVideoWndMgr::enter_full_screen(bool bFull)
{
	if ((_is_full_screen && !bFull) || (!_is_full_screen && bFull))
	{
		_is_full_screen = bFull;
		if (!_is_full_screen && _current_video_view != NULL)// exit full screen
		{
			set_current_view(_current_video_view);
		}

		//update_layout();
	}
}

void FRTCVideoWndMgr::toggle_layout_grid_mode(bool gridMode)
{
	_is_grid_mode = gridMode;
	if (_is_grid_mode)
	{
		_wnd_count = 10;
		if (_current_video_view != NULL)
		{
			set_current_view(_current_video_view);
		}
		else
		{
			update_layout();
		}
	}
	else
	{
		_wnd_count = 10;
		if (_occupied_video_wnd_List.size() > 6)
		{
			int index = 0;
			auto tmpLast = _occupied_video_wnd_List.rbegin();
			auto tmp = _occupied_video_wnd_List.begin();
			for (; tmp != _occupied_video_wnd_List.end() && *tmp != *tmpLast;)
			{
				index++;
				if (index > 5)
				{
					VideoWnd* pVidWnd = *tmp;
					ShowWindow(*pVidWnd, SW_HIDE);
				}
				tmp++;
			}
		}

		update_layout();
	}
}

void FRTCVideoWndMgr::toggle_local_video_hidden(bool hide)
{
	_is_local_video_hidden = hide;
	update_layout();
}

bool FRTCVideoWndMgr::is_local_video_visible()
{
	return _is_local_video_hidden;
}

bool FRTCVideoWndMgr::is_local_only()
{
	if (_occupied_video_wnd_List.size() == 1)
	{
		VideoWnd* pVidWnd = _occupied_video_wnd_List.front();
		if (pVidWnd->get_msid() == _local_stream_msid)
		{
			return true;
		}
	}
	return false;
}

void FRTCVideoWndMgr::set_handle_layout_change(bool bHandle)
{
	_is_processing_layout_change = bHandle;
}

void FRTCVideoWndMgr::toggle_audio_state_icon_window_show(std::string uuid,
	bool audioIconshow)
{
	if (uuid.empty())
		return;

	for (const std::pair<std::string, VideoWnd*>& item : _occupied_video_wnd_map)
	{
		VideoWnd* pVidWnd = item.second;
		std::string wndUUID(pVidWnd->get_uuid());
		std::string prefixContent = "VCR";
		if (wndUUID.compare(0, prefixContent.length(), prefixContent) == 0 &&
			wndUUID.length() > prefixContent.length())
		{
			wndUUID = wndUUID.substr(prefixContent.length(),
				wndUUID.length() - prefixContent.length());
		}

		if (wndUUID == uuid)
		{
			pVidWnd->show_audio_muted_icon_wnd(audioIconshow);
			if (pVidWnd->get_uuid() == _cell_customization_uuid)
			{
				pVidWnd->show_pinned_icon_wnd(true);
			}
			else
			{
				pVidWnd->show_pinned_icon_wnd(false);
			}
			pVidWnd->update_icon_window();
		}
	}
}

void FRTCVideoWndMgr::show_all_icon_window()
{
	auto tmp = _occupied_video_wnd_List.begin();
	for (; tmp != _occupied_video_wnd_List.end(); tmp++)
	{
		VideoWnd* pVidWnd = *tmp;

		if (pVidWnd->get_uuid() == _cell_customization_uuid)
		{
			pVidWnd->show_pinned_icon_wnd(true);
		}
		else
		{
			pVidWnd->show_pinned_icon_wnd(false);
		}

		pVidWnd->update_icon_window();
	}
}

void FRTCVideoWndMgr::clear_params()
{
	_user_name_map.clear();
	_msid_map.clear();
	_is_local_video_hidden = false;
	_is_watermark_enabled = false;
	_watermark_str = "";
}

void  FRTCVideoWndMgr::set_current_view(VideoWnd* pView)
{
	AutoLock autolock(_lock);
	if (_current_video_view == pView) {
		_current_video_view = NULL;
		DebugLog("set current view to NULL");

		for (int i = 0; i < _wnd_count; i++) {
			if (_is_local_video_hidden &&
				_video_windows[i].get_msid() == _local_stream_msid) {
				::ShowWindow(_video_windows[i], SW_HIDE);
			}
			else {
				::ShowWindow(_video_windows[i], _video_windows[i]._show_wnd_cmd);
			}

			if (_video_windows[i]._show_wnd_cmd == SW_SHOW)
			{
				auto iter = this->_user_name_map.find(_video_windows[i].get_msid());
				if (iter != this->_user_name_map.end())
				{
					_video_windows[i].update_user_name(iter->second);
				}
			}
		}
	}
	else {
		DebugLog("Set current view to view");
		_current_video_view = pView;
		for (int i = 0; i < _wnd_count; i++) {
			if (_current_video_view->get_msid() != _video_windows[i].get_msid())
			{
				::ShowWindow(_video_windows[i], SW_HIDE);
			}
		}

		::ShowWindow(*_current_video_view, SW_SHOW);

		auto iter = this->_user_name_map.find(_current_video_view->get_msid());
		if (iter != this->_user_name_map.end())
		{
			_current_video_view->update_user_name(iter->second);
		}
	}

	update_layout();
}

void FRTCVideoWndMgr::set_active_speaker(const std::string& uuid, bool forceSet)
{
	if (forceSet)// drop call
	{
		_active_speaker_uuid = "";
		return;
	}

	if (uuid == this->_local_preview_uuid)// active speaker is self
	{
		//do nothing
	}
	else
	{
		if (_active_speaker_uuid != uuid)
		{
			_active_speaker_uuid = uuid;

			if (_is_full_screen &&
				_current_video_view != NULL &&
				!(_current_video_view->get_uuid().compare(0,
					kContentMSIDPrefixStr.length(),
					kContentMSIDPrefixStr) == 0) &&
				_current_video_view->get_uuid() != _cell_customization_uuid &&
				_active_speaker_uuid != "")
			{
				for (int i = 0; i < _wnd_count; i++)
				{
					if (_video_windows[i].get_uuid() == _active_speaker_uuid)
					{
						_current_video_view->set_main_cell(false);
						::ShowWindow(*_current_video_view, SW_HIDE);

						_current_video_view = &_video_windows[i];
						::ShowWindow(_video_windows[i], SW_SHOW);

						auto iter = this->_user_name_map.find(_video_windows[i].get_msid());
						if (iter != this->_user_name_map.end())
						{
							_video_windows[i].update_user_name(iter->second);
						}

						break;
					}
				}
			}

			std::string msid = "";
			auto iter = this->_msid_map.find(uuid);
			if (iter != this->_msid_map.end())
				msid = iter->second;

			update_layout();
		}
	}
}

void FRTCVideoWndMgr::set_cell_customizations(const std::string& uuid)
{
	if (uuid == this->_local_preview_uuid)
	{
		return;
	}

	if (_cell_customization_uuid == uuid)
	{
		return;
	}

	_cell_customization_uuid = uuid;

	std::string currentViewUUID;
	if (_cell_customization_uuid == "")
	{
		currentViewUUID = _active_speaker_uuid;
	}
	else
	{
		currentViewUUID = _cell_customization_uuid;
	}

	if (_is_full_screen &&
		_current_video_view != NULL &&
		!(_current_video_view->get_uuid().compare(0,
			kContentMSIDPrefixStr.length(),
			kContentMSIDPrefixStr) == 0) &&
		currentViewUUID != "")
	{
		for (int i = 0; i < _wnd_count; i++)
		{
			if (_video_windows[i].get_uuid() == currentViewUUID)
			{
				_current_video_view->set_main_cell(false);
				::ShowWindow(*_current_video_view, SW_HIDE);

				_current_video_view = &_video_windows[i];
				::ShowWindow(_video_windows[i], SW_SHOW);

				auto iter = this->_user_name_map.find(_video_windows[i].get_msid());
				if (iter != this->_user_name_map.end())
				{
					_video_windows[i].update_user_name(iter->second);
				}

				break;
			}
		}
	}

	std::string msid = "";
	auto iter = this->_msid_map.find(uuid);
	if (iter != this->_msid_map.end())
		msid = iter->second;

	DebugLog("set_cell_customizations uuid = %s, msid = %s",
		_cell_customization_uuid.c_str(), msid.c_str());

	update_layout();
}

void FRTCVideoWndMgr::set_current_lecture(const std::string& uuid)
{
	_current_lecture_uuid = uuid;
	update_layout();
}

void FRTCVideoWndMgr::set_first_request_stream_uuid(const std::string& uuid)
{
	_first_request_stream_uuid = uuid;
}

void FRTCVideoWndMgr::set_user_name(const std::string& msid,
	const std::string& siteName)
{
	auto it = this->_user_name_map.find(msid);
	if (it == this->_user_name_map.end())
	{
		this->_user_name_map.insert(std::pair<std::string, std::string>(
			msid, siteName));
	}
	else
	{
		if (it->second != siteName)
		{
			it->second = siteName;
			std::string uuid = find_uuid(msid);
			if (!uuid.empty())
			{
				auto found = _occupied_video_wnd_map.find(uuid);
				if (found != _occupied_video_wnd_map.end())
				{
					found->second->update_user_name(siteName);
					found->second->update_icon_window();
				}
			}
		}
	}
}

void FRTCVideoWndMgr::update_user_name_by_uuid(const std::string& uuid,
	const std::string& username)
{
	auto it = this->_msid_map.find(uuid);
	if (it != this->_msid_map.end())
	{
		auto _iter_username = this->_user_name_map.find(it->second);
		if (_iter_username != this->_user_name_map.end())
		{
			_iter_username->second = username;
			auto wndIt = _occupied_video_wnd_map.find(uuid);
			if (wndIt != _occupied_video_wnd_map.end())
			{
				wndIt->second->update_user_name(username);
			}
		}
	}
}

void FRTCVideoWndMgr::set_msid(const std::string& uuid, const std::string& msid)
{
	AutoLock _lock(_uuid_to_msid_map_sec);
	auto it = this->_msid_map.find(uuid);
	if (it == this->_msid_map.end())
	{
		DebugLog("set msid failed, not found uuid=%s, msid=%s",
			uuid.c_str(), msid.c_str());

		this->_msid_map.insert(std::pair<std::string, std::string>(uuid, msid));
	}
	else
	{
		DebugLog("set msid success, uuid=%s, old msid=%s, new msid=%s",
			uuid.c_str(), it->second.c_str(), msid.c_str());
		it->second = msid;
	}
}

std::string FRTCVideoWndMgr::find_uuid(const std::string msid)
{
	AutoLock _lock(_uuid_to_msid_map_sec);
	auto it = _msid_map.begin();
	for (; it != _msid_map.end(); it++)
	{
		if (it->second == msid)
		{
			DebugLog("find uuid success, uuid=%s, msid=%s",
				it->first.c_str(), msid.c_str());
			return it->first;
		}
	}

	DebugLog("find uuid failed, msid=%s", msid.c_str());

	return "";
}