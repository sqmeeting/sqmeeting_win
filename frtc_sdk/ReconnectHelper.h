#pragma once

#include "stdafx.h"
#include "rtc_definitions.h"
#include "frtc_sdk_api.h"
#include "auto_lock.h"

#define CALL_RECONNECT_MAX_RETRY_CNT 3

enum ReconnectState
{
	RECONNECT_IDLE,
	RECONNECT_SUCCESS,
	RECONNECT_TRYING,
	RECONNECT_FAILED
};

class ReconnectHelper
{
public:
	ReconnectHelper();
	~ReconnectHelper();

	ReconnectState HandleCallStateChange(RTC::MeetingStatus state, RTC::MeetingStatusChangeReason reason);
	void SetLastCallParam(const FrtcCallParam& param);
	void SetLastCallPwdForGuest(const char* pwd);
	void SetLastCallLatestDisplayName(const char* new_name);
	void ResetReconnectStatue();
	ReconnectState GetReconnectState();
	const FrtcCallParam* GetLastCallParam();
	void cancel_next_reconnect();
    
private:
	void DeepCopyFrtcCallParam(FrtcCallParam* target, const FrtcCallParam& source);
	void TryReconnect();
	static void DoReconnect(ReconnectHelper* reconnectHelper);

private:
	CritSec reconnect_cs_;
	ReconnectState reconnect_state_;
	FrtcCallParam* last_call_param_;
	int retry_cnt_;
	bool retry_processing_;
	bool reconnect_canceled_;
	uint64_t reconnect_start_time_;
	RTC::MeetingStatusChangeReason last_reason_;
};

