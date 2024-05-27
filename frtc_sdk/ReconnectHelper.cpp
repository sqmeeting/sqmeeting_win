#include "ReconnectHelper.h"
#include "frtccall_manager.h"
#include "log.h"

#include <chrono>
#include <thread>
#include <memory>


extern std::shared_ptr<FrtcManager> g_frtc_mgr;

ReconnectHelper::ReconnectHelper()
	: reconnect_state_(ReconnectState::RECONNECT_IDLE),
	last_call_param_(NULL),
	retry_cnt_(0),
	retry_processing_(false),
	reconnect_canceled_(false),
	reconnect_start_time_(0)
{

}

ReconnectHelper::~ReconnectHelper()
{
	if (last_call_param_)
	{
		delete last_call_param_;
		last_call_param_ = NULL;
	}
}

void ReconnectHelper::SetLastCallParam(const FrtcCallParam& param)
{
	if (last_call_param_)
		delete last_call_param_;
	last_call_param_ = new FrtcCallParam();
	DeepCopyFrtcCallParam(last_call_param_, param);
	//last_call_param_ = param.clone();
}

void ReconnectHelper::SetLastCallPwdForGuest(const char* pwd)
{
	if (pwd && last_call_param_)
	{
		if (last_call_param_->_meeting_pwd)
			delete[] last_call_param_->_meeting_pwd;
		size_t endingLen = 1;
		size_t len = strlen(pwd);
		last_call_param_->_meeting_pwd = new char[len + endingLen];
		memset(last_call_param_->_meeting_pwd, 0, len + endingLen);
		strcpy_s(last_call_param_->_meeting_pwd, len + endingLen, pwd);
	}
}

void ReconnectHelper::SetLastCallLatestDisplayName(const char* new_name)
{
	if (new_name && last_call_param_)
	{
		if (last_call_param_->_display_name)
			delete[] last_call_param_->_display_name;
		size_t endingLen = 1;
		size_t len = strlen(new_name);
		last_call_param_->_display_name = new char[len + endingLen];
		memset(last_call_param_->_display_name, 0, len + endingLen);
		strcpy_s(last_call_param_->_display_name, len + endingLen, new_name);
	}
}

void ReconnectHelper::ResetReconnectStatue()
{
	reconnect_state_ = ReconnectState::RECONNECT_IDLE;
	retry_cnt_ = 0;
	retry_processing_ = false;
	reconnect_canceled_ = false;
	reconnect_start_time_ = 0;
}

ReconnectState ReconnectHelper::HandleCallStateChange(RTC::MeetingStatus state, RTC::MeetingStatusChangeReason reason)
{
	AutoLock lock(reconnect_cs_);
	if (reconnect_state_ == ReconnectState::RECONNECT_IDLE)
	{
		if (state == RTC::MeetingStatus::kDisconnected
			&& reason == RTC::MeetingStatusChangeReason::kMeetingEndAbnormal
			&& reconnect_start_time_ == 0)
		{
			reconnect_state_ = ReconnectState::RECONNECT_TRYING;
			reconnect_start_time_ = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()).count();
			TryReconnect();
		}
	}
	else if (reconnect_state_ == ReconnectState::RECONNECT_TRYING)
	{
		if (state == RTC::MeetingStatus::kConnected)
		{
			reconnect_state_ = ReconnectState::RECONNECT_SUCCESS;
		}
		else
		{
			if (reason == RTC::MeetingStatusChangeReason::kMeetingEndAbnormal)
			{
				uint64_t t = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()).count();
				if (reconnect_start_time_ > 0 && (t - reconnect_start_time_ < 500))//avoid double 49
				{
					return reconnect_state_;
				}
			}
			if (!retry_processing_
				&& reason != RTC::MeetingStatusChangeReason::kAborted)//call state disconnect send twice, avoid second reaseon 0
			{
				TryReconnect();
			}
		}
	}
	else//manully reconnect result
	{
		reconnect_state_ =
			state == RTC::MeetingStatus::kConnected ? ReconnectState::RECONNECT_SUCCESS : ReconnectState::RECONNECT_FAILED;
	}
	retry_processing_ = false;
	return reconnect_state_;
}

ReconnectState ReconnectHelper::GetReconnectState()
{
	return reconnect_state_;
}

const FrtcCallParam* ReconnectHelper::GetLastCallParam()
{
	return last_call_param_;
}

void ReconnectHelper::TryReconnect()
{
	if (last_call_param_ && retry_cnt_ < CALL_RECONNECT_MAX_RETRY_CNT)
	{
		retry_processing_ = true;
		std::thread timer_thread(DoReconnect, this); // Register your `YourFunction`.
		timer_thread.detach(); // this will be non-blocking thread.
		retry_cnt_++;
		std::this_thread::sleep_for(std::chrono::milliseconds(50));
	}
	else
	{
		DebugLog("ReconnectHelper::TryReconnect failed, last_call_param_ is null or retry_cnt_ >= CALL_RECONNECT_MAX_RETRY_CNT");
		reconnect_state_ = ReconnectState::RECONNECT_FAILED;
	}
}

void ReconnectHelper::DoReconnect(ReconnectHelper* reconnectHelper)
{
	DebugLog("ReconnectHelper::DoReconnect, retry_cnt_ is %d", reconnectHelper->retry_cnt_);
	if (reconnectHelper->retry_cnt_ == 0)
	{
		std::this_thread::sleep_for(std::chrono::milliseconds(2000));
	}
	else
	{
		std::this_thread::sleep_for(std::chrono::milliseconds(reconnectHelper->retry_cnt_ * 10000));
	}

	if (reconnectHelper->GetReconnectState() == ReconnectState::RECONNECT_TRYING && !reconnectHelper->reconnect_canceled_)
	{
		g_frtc_mgr->reconnect_current_meeting(*reconnectHelper->last_call_param_);
	}
	else
	{
		reconnectHelper->reconnect_canceled_ = false;
	}
}

void ReconnectHelper::cancel_next_reconnect()
{
	reconnect_canceled_ = true;
}

void ReconnectHelper::DeepCopyFrtcCallParam(FrtcCallParam* target, const FrtcCallParam& source)
{
	size_t endingLen = 1;
	size_t len = strlen(source._server_address);
	target->_server_address = new char[len + endingLen];
	memset(target->_server_address, 0, len + endingLen);
	strcpy_s(target->_server_address, len + endingLen, source._server_address);

	len = strlen(source.callNumber);
	target->callNumber = new char[len + endingLen];
	memset(target->callNumber, 0, len + endingLen);
	strcpy_s(target->callNumber, len + endingLen, source.callNumber);

	len = strlen(source._display_name);
	target->_display_name = new char[len + endingLen];
	memset(target->_display_name, 0, len + endingLen);
	strcpy_s(target->_display_name, len + endingLen, source._display_name);

	if (source._user_token)
	{
		len = strlen(source._user_token);
		target->_user_token = new char[len + endingLen];
		memset(target->_user_token, 0, len + endingLen);
		strcpy_s(target->_user_token, len + endingLen, source._user_token);
	}

	if (source._meeting_pwd)
	{
		len = strlen(source._meeting_pwd);
		target->_meeting_pwd = new char[len + endingLen];
		memset(target->_meeting_pwd, 0, len + endingLen);
		strcpy_s(target->_meeting_pwd, len + endingLen, source._meeting_pwd);
	}

	len = strlen(source._call_rate);
	target->_call_rate = new char[len + endingLen];
	memset(target->_call_rate, 0, len + endingLen);
	strcpy_s(target->_call_rate, len + endingLen, source._call_rate);

	target->isAudioOnly = source.isAudioOnly;
	target->muteAudio = source.muteAudio;
	target->muteVideo = source.muteVideo;
	target->layout = source.layout;
}
