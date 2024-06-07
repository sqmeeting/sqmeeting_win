using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using SQMeeting.Commands;
using SQMeeting.FRTCView;
using SQMeeting.LogTool;
using SQMeeting.Model;
using SQMeeting.Model.DataObj;
using SQMeeting.MvvMMessages;
using SQMeeting.Properties;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace SQMeeting.ViewModel
{
    public enum MeetingRecurring
    {
        NoRecurring,
        Daily,
        Weekly,
        Monthly
    }

    public class MeetingRecurringOption : ObservableObject
    {
        private string _recurringName = string.Empty;
        public string RecurringName
        {
            get
            {
                return _recurringName;
            }
            set
            {
                if (_recurringName != value)
                {
                    _recurringName = value;
                    RaisePropertyChanged();
                }
            }
        }
        private MeetingRecurring _recurring = MeetingRecurring.NoRecurring;
        public MeetingRecurring Recurring
        {
            get
            {
                return _recurring;
            }
            set
            {
                if (_recurring != value)
                {
                    _recurring = value;
                    RaisePropertyChanged();
                }
            }
        }
    }

    public class RecurringFrequenceOption : ObservableObject
    {
        private string _frequenceName = string.Empty;
        public string FrequenceName
        {
            get
            {
                return _frequenceName;
            }
            set
            {
                if (_frequenceName != value)
                {
                    _frequenceName = value;
                    RaisePropertyChanged();
                }
            }
        }
        private int _frequence = 1;
        public int Frequence
        {
            get
            {
                return _frequence;
            }
            set
            {
                if (_frequence != value)
                {
                    _frequence = value;
                    RaisePropertyChanged();
                }
            }
        }
    }

    public class ScheduleMeetingViewModel : FRTCViewModelBase
    {
        MeetingScheduleManager m_scheduleMgr;
        FRTCUserManager m_signInMgr;

        object detailDialogLockObj = null;

        bool _isIncall = false;
        string _inCallMeetingNum = string.Empty;

        System.Timers.Timer _updateScheduleTimer = null;
        Task _updateScheduleTimerDelayTask = null;
        CancellationTokenSource _updateScheduleTimerDelayTaskCancel = null;

        public ScheduleMeetingViewModel()
        {
            this.ShutdownAfterClose = false;

            detailDialogLockObj = new object();

            m_scheduleMgr = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<MeetingScheduleManager>();
            m_signInMgr = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<FRTCUserManager>();



            MessengerInstance.Register<FRTCAPIResultMessage>(this, OnUserSingInResult);
            MessengerInstance.Register<FRTCMeetingScheduledMessage>(this, OnMeetingScheduled);
            MessengerInstance.Register<MeetingScheduleAPIErrorMessage>(this, OnMeetingScheduleError);
            MessengerInstance.Register<FRTCScheduleMeetingListMessage>(this, OnScheduleMeetingList);
            MessengerInstance.Register<FRTCRecurringMeetingGroupMessage>(this, OnRecurringMeetingGroup);
            MessengerInstance.Register<FRTCScheduledMeetingDetailMessage>(this, OnMeetingDetail);
            MessengerInstance.Register<FRTCEditMeetingSuccessMsg>(this, OnMeetingUpdated);
            MessengerInstance.Register<FRTCCallStateChangeMessage>(this, (msg) =>
            {
                if (ShowMask)
                    ShowMask = false;
                if (msg.callState == FrtcCallState.CONNECTED)
                {
                    _isIncall = true;
                    _inCallMeetingNum = msg.meetingId;
                    var reminders = FRTCGlobalReminderToast.GetReminders();
                    if (reminders != null && reminders.Count > 0)
                    {
                        var found = reminders.FirstOrDefault((p) => { return p.MeetingNumber == _inCallMeetingNum; });
                        if (found != null)
                        {
                            reminders.Remove(found);
                        }
                        if (reminders.Count == 0)
                        {
                            FRTCGlobalReminderToast.CloseGlobalReminder();
                            _reminderCloseTimer?.Stop();
                        }
                    }
                }
                else if (msg.callState == FrtcCallState.DISCONNECTED)
                {
                    _isIncall = false;
                    _inCallMeetingNum = string.Empty;
                    if (m_signInMgr.IsUserSignIn)
                        _refreshScheduledMeetingCommand.Execute(null);
                }
            });

            MessengerInstance.Register<NotificationMessage<ScheduledMeetingDislpayData>>(this, (msg) =>
            {
                if (msg.Notification == "join_meeting_reminder" && msg.Content != null)
                {
                    JoinScheduledMeetingCommand.Execute(msg.Content);
                    FRTCGlobalReminderToast.CloseGlobalReminder();
                    _reminderCloseTimer?.Stop();
                }
            });


            #region InitCommands
            _refreshScheduledMeetingCommand = new RelayCommand(() =>
            {
                lock (m_scheduleMeetingListLockObj)
                {
                    nextMeetingListPageIndex = 1;
                    totalMeetingListPageCnt = 0;
                    this.ScheduledMeetingList?.Clear();
                    FetchingMeetings = true;
                    m_scheduleMgr.GetScheduledMeetingList(nextMeetingListPageIndex, 50, string.Empty);
                }
            });

            _queryScheduledMeetingNextPageCommand = new RelayCommand<bool>((query) =>
            {
                lock (m_scheduleMeetingListLockObj)
                {
                    if (query && !FetchingMeetings && nextMeetingListPageIndex < totalMeetingListPageCnt)
                    {
                        FetchingMeetings = true;
                        m_scheduleMgr.GetScheduledMeetingList(nextMeetingListPageIndex, 50, string.Empty);
                    }
                }
            });

            _frtcPopupUpdateScheduledMeetingDialog = new RelayCommand(() =>
            {
                if (CurrentSelectdScheduledMeeting != null)
                    ShowUpdateScheduledMeetingDialog(CurrentSelectdScheduledMeeting);
            });

            _frtcPopupUpdateScheduledMeetingDialogFromList = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                if (data != null)
                    ShowUpdateScheduledMeetingDialog(data);
            });

            _popAddInviteeWindowCommand = new RelayCommand<Window>((w) =>
            {
                m_signInMgr.QueryUsers(1, 50, "");
                InviteeListTemp = new List<ScheduleMeetingInviteeItem>(InviteeList);
                foreach (var u in InviteeListTemp)
                {
                    InvitedUsers.Add(u.Info.user_id, u);
                }
                FRTCView.ScheduleMeetingSelectInviteeWindow invite = new FRTCView.ScheduleMeetingSelectInviteeWindow();
                invite.Owner = w;
                invite.Closed += (s, e) => { ClearInviteeTempData(); };
                invite.ShowDialog();
            });

            _confirmInviteeCommand = new RelayCommand<Window>((w) =>
            {
                if (InviteeListTemp.Count > 100)
                {
                    FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_INVITEE_MAX_CNT, Properties.Resources.FRTC_MEETING_SDKAPP_INVITEE_MAX_CNT_TIP);
                    return;
                }
                InviteeList = new List<ScheduleMeetingInviteeItem>(InviteeListTemp);
                w?.Close();
            });

            _selectInviteeCommand = new RelayCommand<ScheduleMeetingInviteeItem>((invitee) =>
            {
                if (invitee.IsInvited && !InvitedUsers.ContainsKey(invitee.Info.user_id))
                    InvitedUsers.Add(invitee.Info.user_id, invitee);
                else
                    InvitedUsers.Remove(invitee.Info.user_id);

                InviteeListTemp = InvitedUsers.Values.OrderBy((item) => { return item.Info.username; }).ToList();
            });

            _removeInviteeCommand = new RelayCommand<ScheduleMeetingInviteeItem>((removed) =>
            {
                removed.IsInvited = false;
                InvitedUsers.Remove(removed.Info.user_id);
                InviteeListTemp = InvitedUsers.Values.OrderBy((item) => { return item.Info.username; }).ToList();
            });

            _queryUserNextPageCommand = new RelayCommand(() =>
            {
                if (currentUsersPageIndex < this.totalUsersPageCnt)
                {
                    m_signInMgr.QueryUsers(++currentUsersPageIndex, 50, SearchUserPattern);
                }
            });

            _frtcScheduleMeetingCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrEmpty(this.ScheduledMeetingReservationID))
                {
                    LogTool.LogHelper.Debug("Schedule a new meeting");
                    m_scheduleMgr.ScheduleMeeting(CreateMeetingScheduleInfo());
                }
                else
                {
                    LogTool.LogHelper.Debug("Update a scheduled meeting");
                    m_scheduleMgr.UpdateScheduledMeeting(this.ScheduledMeetingReservationID, CreateMeetingScheduleInfo());
                }
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                this.ScheduledMeetingReservationID = string.Empty;
            });

            _frtcUpdateScheduledMeetingCommand = new RelayCommand(() =>
            {
                m_scheduleMgr.UpdateScheduledMeeting(this.ScheduledMeetingReservationID, CreateMeetingScheduleInfo());
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
            });

            _frtcScheduleMeetingMenuCommand = new SQMeetingCommand<string, ScheduledMeetingDislpayData>((tag, data) =>
            {
                if (data == null)
                    return;
                switch (tag)
                {
                    case "copy":
                        if (data.IsAvailable)
                        {
                            long beginTimestamp = long.Parse(data.BeginTime);
                            DateTime beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(beginTimestamp);
                            long endTimestamp = long.Parse(data.EndTime);
                            DateTime endTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTimestamp);

                            try
                            {
                                if (data.IsRecurringMeeting)
                                {
                                    Clipboard.SetText(UIHelper.GetMeetingInvitationText(DisplayName, data));
                                }
                                else
                                    Clipboard.SetText(UIHelper.GetMeetingInvitationText(DisplayName, data.MeetingName, beginTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), data.MeetingNumber, data.Password, data.IsRecurringMeeting, endTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), data.MeetingUrl));
                            }
                            catch (Exception ex)
                            {
                                LogTool.LogHelper.Exception(ex);
                                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_FAILED });
                                return;
                            }
                            MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE });
                        }
                        break;
                    case "recurring_lookup":
                        ShowRecurringGroupWnd(data);
                        break;
                    case "edit":
                        if (data.IsAvailable && data.MeetingType != "instant")
                        {
                            m_scheduleMgr.GetScheduledMeetingDetail(data.ReserveId);
                            if (data.IsRecurringMeeting)
                            {
                                m_scheduleMgr.GetRecurringMeetingGroup(data.RecurrenceId, 1, 365, string.Empty);
                                _currentLookupRecurringMeeting = data;
                            }
                            FRTCPopupUpdateScheduledMeetingDialogFromList.Execute(data);
                        }
                        break;
                    case "edit_single":
                        if (data.IsAvailable && data.MeetingType != "instant")
                        {
                            ShowUpdateScheduledMeetingDialog(data, true);
                        }
                        break;
                    case "cancel":
                        if (data.IsManuallyAdded)
                        {
                            if (data.IsRecurringMeeting)
                            {
                                if (FRTCMessageBox.ShowConfirmMessage(
                                    Resources.FRTC_SDKAPP_MEETING_REMOVE_RECURRING_FROM_LIST,
                                    Resources.FRTC_SDKAPP_MEETING_REMOVE_RECURRING_FROM_LIST_TIP,
                                    Resources.FRTC_SDKAPP_MEETING_REMOVE_RECURRING_FROM_LIST,
                                    Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                                    true))
                                {
                                    m_scheduleMgr.RemoveMeetingFromList(data.GroupInfoKey);
                                }
                            }
                            else
                            {
                                if (FRTCMessageBox.ShowConfirmMessage(
                                        Resources.FRTC_SDKAPP_MEETING_REMOVE_FROM_LIST,
                                        Resources.FRTC_SDKAPP_MEETING_REMOVE_FROM_LIST_TIP,
                                        Resources.FRTC_SDKAPP_MEETING_REMOVE_FROM_LIST,
                                        Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                                        true))
                                {
                                    m_scheduleMgr.RemoveMeetingFromList(data.MeetingInfoKey);
                                }
                            }
                        }
                        else
                        {
                            if (!data.IsRecurringMeeting)
                            {
                                if (FRTCView.FRTCMessageBox.ShowConfirmMessage(
                                    Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE,
                                    Properties.Resources.FRTC_MEETING_CANCEL_SCHEDULE_MSG,
                                    Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE_OK,
                                    Properties.Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                                    true))
                                {
                                    m_scheduleMgr.DeleteScheduledMeeting(data.ReserveId);
                                }
                            }
                            else
                            {
                                bool cancelRecurrence = false;
                                if (FRTCMessageBoxBig.ShowCancelRecurringMeetingConfirmWindow(out cancelRecurrence))
                                {
                                    LogTool.LogHelper.Debug("delete meeting cancel recurrence {0}", cancelRecurrence);
                                    m_scheduleMgr.DeleteScheduledMeeting(data.ReserveId, cancelRecurrence);
                                }
                            }
                        }
                        break;
                    case "cancel_single":
                        if (FRTCView.FRTCMessageBox.ShowConfirmMessage(
                                Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE,
                                Properties.Resources.FRTC_SDKAPP_REMOVE_RECURRING_SINGLE_TIP,
                                Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE_OK,
                                Properties.Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                                true, recurringMeetingGroupWnd))
                        {
                            m_scheduleMgr.DeleteScheduledMeeting(data.ReserveId);
                        }
                        break;
                    default:
                        break;
                }
            });

            _showScheduledMeetingDetailCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                lock (detailDialogLockObj)
                {
                    LogTool.LogHelper.DebugMethodEnter();
                    LogTool.LogHelper.Debug(Environment.StackTrace);
                    LogTool.LogHelper.Debug("Selected schedule detail is {0}", data == null ? "null" : "not null");
                    if (data == null)
                        return;
                    LogTool.LogHelper.Debug("Selected schedule detail is {0}", data.IsEmpty() ? "empty" : "not empty");
                    if (data.IsEmpty())
                        return;
                    if (data.IsRecurringMeeting)
                    {
                        _currentLookupRecurringMeeting = data;
                        {
                            m_scheduleMgr.GetRecurringMeetingGroup(data.RecurrenceId, 1, 365, string.Empty);
                        }
                        if (data.RecurringMeetingGroup.Count() > 0)
                        {
                            RecurringMeetingReservedGroup = data.RecurringMeetingGroup;
                        }
                    }

                    m_scheduleMgr.GetScheduledMeetingDetail(data.ReserveId);

                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        if (FRTCPopupViewManager.CurrentPopup != null && FRTCPopupViewManager.CurrentPopup is FRTCView.ScheduledMeetingDetailWindow)
                        {
                            FRTCPopupViewManager.CurrentPopup.Show();
                            FRTCPopupViewManager.CurrentPopup.Activate();
                        }
                        else
                        {
                            FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCScheduledMeetingDetail, null);
                            FRTCPopupViewManager.CurrentPopup.Closed += (s, e) => CurrentSelectdScheduledMeeting = null;
                        }
                    }));
                }
            });

            _copyScheduledMeetingDetailCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                long beginTimestamp = long.Parse(data.BeginTime);
                DateTime beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(beginTimestamp);
                long endTimestamp = long.Parse(data.EndTime);
                DateTime endTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTimestamp);
                try
                {
                    if (data.IsRecurringMeeting)
                    {
                        Clipboard.SetText(UIHelper.GetMeetingInvitationText(DisplayName, data));
                    }
                    else
                        Clipboard.SetText(UIHelper.GetMeetingInvitationText(DisplayName, data.MeetingName, beginTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), data.MeetingNumber, data.Password, data.IsRecurringMeeting, endTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), data.MeetingUrl));
                }
                catch (Exception ex)
                {
                    LogTool.LogHelper.Exception(ex);
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_FAILED });
                    return;
                }
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_INVITE_INFO_SUCCEED });
            });

            _deleteScheduledMeetingCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                if (data.IsManuallyAdded)
                {
                    if (data.IsRecurringMeeting)
                    {
                        if (FRTCMessageBox.ShowConfirmMessage(
                            Resources.FRTC_SDKAPP_MEETING_REMOVE_RECURRING_FROM_LIST,
                            Resources.FRTC_SDKAPP_MEETING_REMOVE_RECURRING_FROM_LIST_TIP,
                            Resources.FRTC_SDKAPP_MEETING_REMOVE_RECURRING_FROM_LIST,
                            Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                            true))
                        {
                            m_scheduleMgr.RemoveMeetingFromList(data.GroupInfoKey);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (FRTCMessageBox.ShowConfirmMessage(
                                Resources.FRTC_SDKAPP_MEETING_REMOVE_FROM_LIST,
                                Resources.FRTC_SDKAPP_MEETING_REMOVE_FROM_LIST_TIP,
                                Resources.FRTC_SDKAPP_MEETING_REMOVE_FROM_LIST,
                                Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                                true))
                        {
                            m_scheduleMgr.RemoveMeetingFromList(data.MeetingInfoKey);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    bool cancelRecurrence = false;
                    if (MeetingRecurring != MeetingRecurring.NoRecurring)
                    {
                        if (!FRTCMessageBoxBig.ShowCancelRecurringMeetingConfirmWindow(out cancelRecurrence))
                        {
                            return;
                        }
                    }
                    m_scheduleMgr.DeleteScheduledMeeting(data.ReserveId, cancelRecurrence);
                    lock (m_scheduleMeetingListLockObj)
                    {
                        ScheduledMeetingList.Remove(data);
                    }
                }
                FRTCPopupViewManager.CurrentPopup?.Close();
                CurrentSelectdScheduledMeeting = null;
            });

            _joinScheduledMeetingCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                if (data == null || data.IsEmpty())
                    return;
                long startTimestamp = 0;
                if (!long.TryParse(data.BeginTime, out startTimestamp))
                {
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_BEGIN_TIME_ERROR });
                    return;
                }
                DateTime start = UIHelper.GetUTCDateTimeFromUTCTimestamp(startTimestamp);
                if (DateTime.UtcNow < start && start.Subtract(DateTime.UtcNow) > TimeSpan.FromMinutes(30))
                {
                    FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_JOIN_MEETING_FAILED, Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_EARLY);
                    return;
                }
                ShowMask = true;
                FRTCPopupViewManager.CurrentPopup?.Close();
                JoinMeetingFromHistoryOrScheduleList msg = new JoinMeetingFromHistoryOrScheduleList(new MeetingScheduleResult()
                {
                    meeting_number = data.MeetingNumber,
                    meeting_password = data.Password,
                    owner_id = data.OwnerId,
                    schedule_start_time = data.BeginTimeStr,
                    schedule_end_time = data.EndTimeStr,
                });
                MessengerInstance.Send<JoinMeetingFromHistoryOrScheduleList>(msg);
            });


            _joinRecurringMeetingCommand = new RelayCommand(() =>
            {
                if (_currentLookupRecurringMeeting != null)
                {
                    _joinScheduledMeetingCommand.Execute(_currentLookupRecurringMeeting);
                }
            });

            _deleteRecurringMeetingGroupCommand = new RelayCommand(() =>
            {
                if (_currentLookupRecurringMeeting != null)
                {
                    if (FRTCMessageBox.ShowConfirmMessage(
                        Properties.Resources.FRTC_SDKAPP_RECURRING_MEETING_CANCEL_GROUP,
                        Properties.Resources.FRTC_MEETING_CANCEL_SCHEDULE_MSG,
                        Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL,
                        Properties.Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK,
                        true))
                    {
                        LogTool.LogHelper.Debug("delete whole recurrence");
                        m_scheduleMgr.DeleteScheduledMeeting(_currentLookupRecurringMeeting.ReserveId, true);
                        recurringMeetingGroupWnd?.Close();
                        RefreshMeetingListCurrentPage();
                    }
                }
            });

            _popRecurringMeetingGroupWindowCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                if (data != null && !data.IsEmpty())
                {
                    ShowRecurringGroupWnd(data);
                }
            });

            _popEditRecurringMeetingGroupCommand = new RelayCommand<Window>((w) =>
            {
                m_scheduleMgr.GetScheduledMeetingDetail(_currentLookupRecurringMeeting.ReserveId);
                this.ScheduledMeetingReservationID = _currentLookupRecurringMeeting.ReserveId;
                ScheduleMeetingName = _currentLookupRecurringMeeting.MeetingName;
                this.MeetingRecurring = MeetingRecurringOptions.Find((p) => { return p.Recurring == _currentLookupRecurringMeeting.RecurringType; }).Recurring;
                if (this.RecurringMeetingGroup != null && this.RecurringMeetingGroup.Count > 0)
                {
                    this.RecurringFrequence = this.RecurringMeetingGroup[0].RecurringFrequency;
                    if (this.RecurringMeetingGroup[0].RecurringDaysOfMonth != null && this.RecurringMeetingGroup[0].RecurringDaysOfMonth.Count() > 0)
                    {
                        var daysInMonth = this.RecurringMeetingGroup[0].RecurringDaysOfMonth;
                        this._recurringMonthDays.Clear();
                        foreach (var d in daysInMonth)
                        {
                            this._recurringMonthDays.Add(d);
                        }
                        RaisePropertyChanged("RecurringMonthDays");
                    }
                    if (this.RecurringMeetingGroup[0].RecurringDaysOfWeek != null && this.RecurringMeetingGroup[0].RecurringDaysOfWeek.Count() > 0)
                    {
                        var daysInWeek = this.RecurringMeetingGroup[0].RecurringDaysOfWeek;
                        this._recurringWeekDays.Clear();
                        foreach (var d in daysInWeek)
                        {
                            this._recurringWeekDays.Add(d);
                        }
                        RaisePropertyChanged("RecurringWeekDays");
                    }
                }
                var optionNoRecurring = MeetingRecurringOptions[0];
                MeetingRecurringOptions.RemoveAt(0);

                CurrentTimeZone = TimeZoneInfo.Local.DisplayName;

                ScheduleMeetingStartDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(_currentLookupRecurringMeeting.BeginTime)).ToLocalTime().Date;

                FRTCPopupViewManager.CurrentPopup?.Close();

                FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCUpdateRecurringMeetingGroup, null);
                MeetingRecurringOptions.Insert(0, optionNoRecurring);
                ClearMeetingDetailData();
                this.CurrentSelectdScheduledMeeting = null;
                this.InviteeList.Clear();
                this.SearchUserResult?.Clear();
                this.InvitedUsers?.Clear();
                this.InviteeListTemp?.Clear();
                IsEditMeetingPage = false;
            });

            _updateRecurringMeetingGroupCommand = new RelayCommand(() =>
            {
                if (_currentLookupRecurringMeeting != null)
                {
                    MeetingScheduleInfo i = CreateMeetingScheduleInfo();
                    if (UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_end_time))
                        <=
                        UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_start_time)))
                    {
                        FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SHCEDULE_END_TIME_EARLIER);
                        return;
                    }
                    m_scheduleMgr.UpdateRecurringMeetingGroup(_currentLookupRecurringMeeting.ReserveId, i);
                    FRTCPopupViewManager.CurrentPopup?.Close();
                }
            });


            _updateRecurringMeetingSingleCommand = new RelayCommand(() =>
            {
                MeetingScheduleInfo i = CreateUpdateRecurringMeetingSingleInfo();

                if (UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_end_time))
                    <=
                    UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_start_time)))
                {
                    FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SHCEDULE_END_TIME_EARLIER);
                    return;
                }
                else
                {
                    if (_recurringMeetingSingleIndex == -1)
                    {
                        if (RecurringMeetingGroup.Count > 1)
                        {
                            var found = RecurringMeetingGroup.FirstOrDefault(p => { return p.ReserveId == i.meeting_room_id; });
                            if (found != null && UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_end_time))
                                >=
                                UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(found.BeginTime)))
                            {
                                FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_END_TIME_LATER);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (0 < _recurringMeetingSingleIndex && _recurringMeetingSingleIndex < RecurringMeetingGroup.Count - 1)
                        {
                            if (UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_end_time))
                                >=
                                UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[_recurringMeetingSingleIndex + 1].BeginTime)))
                            {
                                FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_END_TIME_LATER);
                                return;
                            }
                            else if (UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_start_time))
                                <=
                                UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[_recurringMeetingSingleIndex - 1].EndTime)))
                            {
                                FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_START_TIME_EARLIER);
                                return;
                            }
                        }
                        else if (_recurringMeetingSingleIndex == 0)
                        {
                            if (UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_end_time))
                                >=
                                UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[1].BeginTime)))
                            {
                                FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_END_TIME_LATER);
                                return;
                            }
                        }
                        else if (_recurringMeetingSingleIndex == RecurringMeetingGroup.Count - 1)
                        {
                            if (UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(i.schedule_start_time))
                                <=
                                UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[_recurringMeetingSingleIndex - 1].EndTime)))
                            {
                                FRTCMessageBox.ShowNotificationMessage(string.Empty, Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_START_TIME_EARLIER);
                                return;
                            }
                        }
                    }
                }

                m_scheduleMgr.UpdateScheduledMeeting(this.ScheduledMeetingReservationID, i);
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
            });

            _saveScheduleToCalendarCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                SaveToCalendar(data);
            });

            _scheduleListCopyToClipboardCommand = new RelayCommand<ScheduledMeetingDislpayData>((data) =>
            {
                try
                {
                    Clipboard.SetText(data.MeetingNumber);
                }
                catch (Exception ex)
                {
                    LogTool.LogHelper.Exception(ex);
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_FAILED });
                    return;
                }
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE });
            });

            _setRecurringWeekDayCmd = new SQMeetingCommand<bool, string>((set, day) =>
            {
                int d = -1;
                int.TryParse(day, out d);
                if (set && !_recurringWeekDays.Contains(d))
                {
                    _recurringWeekDays.Add(d);
                }
                else
                {
                    if (_recurringWeekDays.Contains(d))
                        _recurringWeekDays.Remove(d);
                }
            });

            _setRecurringMonthDayCmd = new SQMeetingCommand<bool, string>((set, day) =>
            {
                int d = -1;
                int.TryParse(day, out d);
                if (set && !_recurringMonthDays.Contains(d))
                {
                    _recurringMonthDays.Add(d);
                }
                else
                {
                    if (_recurringMonthDays.Contains(d))
                        _recurringMonthDays.Remove(d);
                }
            });

            #endregion
        }

        public void SetScheduleMeetingCallRateOptions(IEnumerable<string> callRateOptions)
        {
            this._callRateOptions = callRateOptions.ToList();
            RaisePropertyChanged("CallRateOptions");
        }

        public void PrepareForNewSchedule()
        {
            ClearMeetingDetailData();

            ScheduleMeetingName = string.Format(Properties.Resources.FRTC_MEETING_SDKAPP_SCHEDULE_MEETING_DEFAULT_NAME, m_signInMgr.UserData.real_name);

            if (RoomList != null && RoomList.Count > 0)
                SelectedRoomSchedule = RoomList[0];
            ScheduleMeetingStartDate = ScheduleMeetingEndDate = DateTime.Today;

            RenewScheduleMeetingAvailableStartTime();

            CurrentTimeZone = TimeZoneInfo.Local.DisplayName;
            InviteeList.Clear();
        }

        public void PrepareForUpdateScheduled()
        {
            m_scheduleMgr.GetScheduledMeetingDetail(CurrentSelectdScheduledMeeting.ReserveId);
            this.ScheduledMeetingReservationID = CurrentSelectdScheduledMeeting.ReserveId;
            this.ScheduledMeetingNumber = CurrentSelectdScheduledMeeting.MeetingNumber;
            MeetingPWDInDetailData = CurrentSelectdScheduledMeeting.Password;
        }
        public void ClearData()
        {
            this.currentUsersPageIndex = 1;
            this.totalUsersPageCnt = 0;
            this.UsePersonalMeetingRoomSchedule = false;
            lock (m_scheduleMeetingListLockObj)
            {
                this.ScheduledMeetingList.Clear();
            }
            this.InvitedUsers.Clear();
            this.InviteeList.Clear();
            this.InviteeListTemp.Clear();
            this._isRefreshAfterMeetingScheduled = false;
            this._callRateOptions = new List<string>();
            this.RecurringMeetingGroup.Clear();
            this._currentLookupRecurringMeeting = null;
        }

        public void UnselectMeeting()
        {
            CurrentSelectdScheduledMeeting = null;
        }

        private ScheduledMeetingDislpayData MakeMeetingDislpayData(MeetingScheduleResult meeting)
        {
            string displayMeetingTime = string.Empty;
            bool aboutToBegin = false;
            bool inProgress = false;
            DateTime endTime = DateTime.MinValue;
            string startTimeStr = string.Empty;
            string endTimeStr = string.Empty;
            StringBuilder durationSb = new StringBuilder();
            string current_recurring_end_time = string.Empty;
            try
            {
                long startTimestamp = long.Parse(meeting.schedule_start_time);
                DateTime beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(startTimestamp);

                aboutToBegin = beginTime > DateTime.UtcNow && beginTime.Subtract(DateTime.UtcNow) < new TimeSpan(0, 15, 0);

                long endTimestamp = long.Parse(meeting.schedule_end_time);
                endTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTimestamp);

                LogTool.LogHelper.Debug("Scheduled meeting list === meeting name " + meeting.meeting_name + " start timestamp " + meeting.schedule_start_time + " end timestamp " + meeting.schedule_end_time);

                inProgress = DateTime.UtcNow >= beginTime && DateTime.UtcNow < endTime;

                current_recurring_end_time = endTime.ToLocalTime().ToString("t");

                displayMeetingTime = GetDisplayMeetingTimeString(beginTime.ToLocalTime(), endTime.ToLocalTime(), ref startTimeStr, ref endTimeStr);

                TimeSpan duration = endTime.Subtract(beginTime);

                durationSb.Append(duration.Hours > 0 ? (duration.Hours > 1 ? duration.Hours.ToString() + Properties.Resources.FRTC_MEETING_HOURS : duration.Hours.ToString() + Properties.Resources.FRTC_MEETING_HOUR) : "")
                    .Append(duration.Minutes > 0 ? (duration.Minutes > 1 ? duration.Minutes.ToString() + Properties.Resources.FRTC_MEETING_MINUTES : duration.Minutes.ToString() + Properties.Resources.FRTC_MEETING_MINUTE) : "")
                    .Append(duration.Seconds > 0 ? (duration.Seconds > 1 ? duration.Seconds.ToString() + Properties.Resources.FRTC_MEETING_SECONDS : duration.Minutes.ToString() + Properties.Resources.FRTC_MEETING_SECOND) : "");
            }
            catch { }

            MeetingRecurring type = MeetingRecurring.NoRecurring;
            if (meeting.meeting_type == "recurrence")
            {
                string strType = meeting.recurrence_type.ToLower();
                if (strType == "daily")
                {
                    type = MeetingRecurring.Daily;
                }
                else if (strType == "weekly")
                {
                    type = MeetingRecurring.Weekly;
                }
                else if (strType == "monthly")
                {
                    type = MeetingRecurring.Monthly;
                }
            }

            List<string> participants = null;
            if (meeting.participantUsers != null && meeting.participantUsers.Length > 0)
            {
                participants = meeting.participantUsers.ToList();
            }

            bool isInvited = false;
            if (meeting.owner_id != m_signInMgr.UserData.user_id)
            {
                if (participants != null && participants.Count > 0)
                {
                    isInvited = participants.Contains(m_signInMgr.UserData.user_id);
                }
            }

            bool authed = meeting.owner_id == m_signInMgr.UserData.user_id || m_signInMgr.Role != UserRole.Normal;

            var ret = new ScheduledMeetingDislpayData()
            {
                MeetingType = meeting.meeting_type,
                MeetingNumber = meeting.meeting_number,
                MeetingName = meeting.meeting_name,
                IsRecurringMeeting = meeting.meeting_type == "recurrence",
                BeginTime = meeting.schedule_start_time,
                EndTime = meeting.schedule_end_time,
                MeetingTime = displayMeetingTime,
                MeetingDescription = meeting.meeting_description,
                BeginTimeStr = startTimeStr,
                EndTimeStr = endTimeStr,
                Duration = durationSb.ToString(),
                Password = meeting.meeting_password,
                ReserveId = meeting.reservation_id,
                OwnerId = meeting.owner_id,
                OwnerName = meeting.owner_name,
                IsAvailable = DateTime.UtcNow < endTime,
                IsInvited = isInvited,
                IsAboutToBegin = aboutToBegin,
                IsInProgress = inProgress,
                RecurringMeetingGroup = meeting.recurrenceReservationList,
                ParticipantUsers = participants,
                MeetingInfoKey = meeting.meetingInfoKey,
                GroupInfoKey = meeting.groupInfoKey,
                MeetingUrl = meeting.meeting_url,
                GroupMeetingUrl = meeting.groupMeetingUrl,
                IsManuallyAdded = !isInvited && meeting.owner_id != m_signInMgr.UserData.user_id,
                Authorized = authed
            };

            if (ret.IsRecurringMeeting)
            {
                ret.RecurrenceId = meeting.recurrence_gid;
                ret.RecurringBeginDate = meeting.recurrenceStartDay.HasValue ? meeting.recurrenceStartDay.Value : 0;
                ret.RecurringEndDate = meeting.recurrenceEndDay.HasValue ? meeting.recurrenceEndDay.Value : 0;
                ret.RecurringDaysOfMonth = (int[])meeting.recurrenceDaysOfMonth?.Clone();
                ret.RecurringDaysOfWeek = (int[])meeting.recurrenceDaysOfWeek?.Clone();
                ret.RecurringFrequency = meeting.recurrenceInterval;
                ret.RecurringType = type;
            }
            return ret;
        }

        bool _isRefreshAfterMeetingScheduled = false;
        int nextMeetingListPageIndex = 1;
        int totalMeetingListPageCnt = 0;
        private void OnScheduleMeetingList(FRTCScheduleMeetingListMessage msg)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (FRTCPopupViewManager.CurrentPopup != null)
                {
                    if(FRTCPopupViewManager.CurrentPopup is FRTCView.ScheduleMeetingWindow
                    || FRTCPopupViewManager.CurrentPopup is FRTCView.EditRecurringMeetingGroupWindow
                    || FRTCPopupViewManager.CurrentPopup is FRTCView.EditRecurringMeetingSingleWindow)
                    {
                        return;
                    }
                    if (!(FRTCPopupViewManager.CurrentPopup is SettingWindow)
                        && !(FRTCPopupViewManager.CurrentPopup is FRTCMeetingPasswordWindow))
                    {
                        FRTCPopupViewManager.CurrentPopup?.Close();
                    }
                }

                totalMeetingListPageCnt = msg.MeetingListObj.total_page_num;
                lock (m_scheduleMeetingListLockObj)
                {
                    IEnumerable<ScheduledMeetingDislpayData> list = msg.MeetingListObj.meeting_schedules.Select((meeting) =>
                    {
                        return MakeMeetingDislpayData(meeting);
                    });
                    if (ScheduledMeetingList.Count > 0 && nextMeetingListPageIndex == 1 && !_isRefreshAfterMeetingScheduled)//列表有内容情况下请求第一页 == 用户点击刷新按钮
                    {
                        MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_UPDATED_TO_LATEST });
                    }
                    if (_isRefreshAfterMeetingScheduled)
                        _isRefreshAfterMeetingScheduled = false;

                    ScheduledMeetingDislpayData tmpSelected = null;
                    if (CurrentSelectdScheduledMeeting != null)
                    {
                        tmpSelected = new ScheduledMeetingDislpayData() { ReserveId = CurrentSelectdScheduledMeeting.ReserveId };
                    }
                    if (nextMeetingListPageIndex < 2)
                    {
                        ScheduledMeetingList.Clear();
                        LogTool.LogHelper.Debug("meetinglist clear");
                    }
                    foreach (var itm in list)
                    {
                        var found = this.ScheduledMeetingList.FirstOrDefault((p) => { return p.MeetingNumber == itm.MeetingNumber; });
                        if (found == null)
                        {
                            this.ScheduledMeetingList.Add(itm);
                        }
                    }
                    LogTool.LogHelper.Debug("meetinglist fill");
                    //cancel page turning
                    //if (totalMeetingListPageCnt > 1)
                    //    nextMeetingListPageIndex++;
                    FetchingMeetings = false;
                    if (tmpSelected != null)
                    {
                        var found = ScheduledMeetingList.FirstOrDefault(p => p.ReserveId == tmpSelected.ReserveId);
                        if (found != null)
                        {
                            CurrentSelectdScheduledMeeting = found;
                        }
                        else
                        {
                            FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                            CurrentSelectdScheduledMeeting = null;
                        }
                    }
                }
                if (CommonServiceLocator.ServiceLocator.Current.GetInstance<SettingViewModel>().EnableMeetingReminder)
                {
                    CreateReminderQueue();
                    StartReminderTimer();
                }
            }));
        }


        int recurringMeetingGroupNextPageIndex = 1;
        int recurringMeetingGroupTotalPageCnt = 0;
        RecurringMeetingSeriesWindow recurringMeetingGroupWnd = null;

        static List<string> dayInWeekName = new List<string>()
        {
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_SUN_LONG,
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_MON_LONG,
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_TUE_LONG,
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_WED_LONG,
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_THU_LONG,
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_FRI_LONG,
            Resources.FRTC_SDKAPP_SCHEDULE_DAY_SAT_LONG };
        private string GetDayNameInWeek(int day)
        {
            if (day < 0 || day > 7)
                return string.Empty;
            string ret = dayInWeekName[day];
            return ret;
        }

        private void OnRecurringMeetingGroup(FRTCRecurringMeetingGroupMessage msg)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                totalMeetingListPageCnt = msg.RecurringMeetingGroup.total_page_num;
                lock (m_recurringMeetingGroupLockObj)
                {
                    int[] dOfM = null;
                    if (msg.RecurringMeetingGroup.recurrenceDaysOfMonth != null && msg.RecurringMeetingGroup.recurrenceDaysOfMonth.Count() > 0)
                    {
                        Array.Sort(msg.RecurringMeetingGroup.recurrenceDaysOfMonth);
                        dOfM = (int[])msg.RecurringMeetingGroup.recurrenceDaysOfMonth.Clone();
                    }

                    int[] dOfW = null;
                    if (msg.RecurringMeetingGroup.recurrenceDaysOfWeek != null && msg.RecurringMeetingGroup.recurrenceDaysOfWeek.Count() > 0)
                    {
                        Array.Sort(msg.RecurringMeetingGroup.recurrenceDaysOfWeek);
                        dOfW = (int[])msg.RecurringMeetingGroup.recurrenceDaysOfWeek.Clone();
                    }
                    MeetingRecurring type = MeetingRecurring.NoRecurring;

                    IEnumerable<ScheduledMeetingDislpayData> list = msg.RecurringMeetingGroup.meeting_schedules.Select((meeting) =>
                    {
                        string displayMeetingTime = string.Empty;
                        bool aboutToBegin = false;
                        bool inProgress = false;
                        DateTime endTime = DateTime.MinValue;
                        string startTimeStr = string.Empty;
                        string endTimeStr = string.Empty;
                        StringBuilder durationSb = new StringBuilder();
                        string meetingDayOfWeek = string.Empty;
                        string start_time = string.Empty;
                        string end_time = string.Empty;
                        string startDate = string.Empty;
                        try
                        {
                            long startTimestamp = long.Parse(meeting.schedule_start_time);
                            DateTime beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(startTimestamp);

                            start_time = beginTime.ToLocalTime().ToString("t");

                            DayOfWeek dOfWeek = beginTime.ToLocalTime().DayOfWeek;
                            meetingDayOfWeek = GetDayNameInWeek((int)dOfWeek);

                            aboutToBegin = beginTime > DateTime.UtcNow && beginTime.Subtract(DateTime.UtcNow) < new TimeSpan(0, 15, 0);

                            long endTimestamp = long.Parse(meeting.schedule_end_time);
                            endTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTimestamp);

                            if (endTime.ToLocalTime().Date > beginTime.ToLocalTime().Date)
                                end_time = endTime.ToLocalTime().ToString("MM-dd HH:mm");
                            else
                                end_time = endTime.ToLocalTime().ToString("t");

                            LogTool.LogHelper.Debug("RecurringMeetingGroup === meeting name " + meeting.meeting_name + " start timestamp " + startTimestamp.ToString() + " end timestamp " + endTimestamp.ToString());

                            inProgress = DateTime.UtcNow >= beginTime && DateTime.UtcNow < endTime;

                            displayMeetingTime = GetDisplayMeetingTimeString(beginTime.ToLocalTime(), endTime.ToLocalTime(), ref startTimeStr, ref endTimeStr);

                            startDate = beginTime.ToLocalTime().ToString("yyyy-MM-dd");

                            TimeSpan duration = endTime.Subtract(beginTime);

                            durationSb.Append(duration.Hours > 0 ? (duration.Hours > 1 ? duration.Hours.ToString() + Properties.Resources.FRTC_MEETING_HOURS : duration.Hours.ToString() + Properties.Resources.FRTC_MEETING_HOUR) : "")
                                .Append(duration.Minutes > 0 ? (duration.Minutes > 1 ? duration.Minutes.ToString() + Properties.Resources.FRTC_MEETING_MINUTES : duration.Minutes.ToString() + Properties.Resources.FRTC_MEETING_MINUTE) : "")
                                .Append(duration.Seconds > 0 ? (duration.Seconds > 1 ? duration.Seconds.ToString() + Properties.Resources.FRTC_MEETING_SECONDS : duration.Minutes.ToString() + Properties.Resources.FRTC_MEETING_SECOND) : "");
                        }
                        catch { }

                        {
                            string strType = msg.RecurringMeetingGroup.recurrenceType.ToLower();
                            if (strType == "daily")
                            {
                                type = MeetingRecurring.Daily;
                            }
                            else if (strType == "weekly")
                            {
                                type = MeetingRecurring.Weekly;
                            }
                            else if (strType == "monthly")
                            {
                                type = MeetingRecurring.Monthly;
                            }
                        }

                        var ret = new ScheduledMeetingDislpayData()
                        {
                            MeetingType = "recurrence",
                            MeetingNumber = meeting.meeting_number,
                            MeetingName = meeting.meeting_name,
                            BeginTime = meeting.schedule_start_time,
                            EndTime = meeting.schedule_end_time,
                            MeetingTime = start_time + "-" + end_time,
                            MeetingDayOfWeek = meetingDayOfWeek,
                            MeetingDescription = meeting.meeting_description,
                            BeginTimeStr = startDate,
                            EndTimeStr = end_time,
                            Duration = durationSb.ToString(),
                            Password = meeting.meeting_password,
                            ReserveId = meeting.reservation_id,
                            OwnerId = meeting.owner_id,
                            OwnerName = meeting.owner_name,
                            IsAvailable = DateTime.UtcNow < endTime,
                            IsInvited = meeting.owner_id != m_signInMgr.UserData.user_id,
                            IsAboutToBegin = aboutToBegin,
                            IsInProgress = inProgress,
                            IsRecurringMeeting = meeting.meeting_type == "recurrence",
                            MeetingInfoKey = meeting.meetingInfoKey,
                            MeetingUrl = meeting.meeting_url,
                        };

                        if (ret.IsRecurringMeeting)
                        {
                            ret.RecurrenceId = meeting.recurrence_gid;
                            ret.RecurringBeginDate = msg.RecurringMeetingGroup.recurrenceStartDay.HasValue ? msg.RecurringMeetingGroup.recurrenceStartDay.Value : 0;
                            ret.RecurringEndDate = msg.RecurringMeetingGroup.recurrenceEndDay.HasValue ? msg.RecurringMeetingGroup.recurrenceEndDay.Value : 0;
                            ret.RecurringDaysOfMonth = dOfM;
                            ret.RecurringDaysOfWeek = dOfW;
                            ret.RecurringFrequency = msg.RecurringMeetingGroup.recurrenceInterval;
                            ret.RecurringType = type;
                        }
                        return ret;
                    });

                    ScheduledMeetingDislpayData tmpSelected = null;
                    if (_currentLookupRecurringMeeting != null)
                    {
                        ScheduleMeetingName = _currentLookupRecurringMeeting.MeetingName;
                        DateTime recurringStartDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(msg.RecurringMeetingGroup.recurrenceStartDay.Value).ToLocalTime().Date;
                        if (recurringStartDate > DateTime.Today.Date)
                            this.ScheduleMeetingStartDate = recurringStartDate;

                        string frequencyStr = msg.RecurringMeetingGroup.recurrenceInterval == 1 ? "" : msg.RecurringMeetingGroup.recurrenceInterval.ToString();
                        string days = string.Empty;
                        string recurringType = msg.RecurringMeetingGroup.recurrenceType.ToLower();
                        string periodStr = string.Empty;
                        string daysInPeriod = string.Empty;
                        if (recurringType == "daily")
                        {
                            if (string.IsNullOrEmpty(frequencyStr))
                            {
                                RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_MEETING_RECURRING_DAILY;
                            }
                            else
                            {
                                RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + Resources.FRTC_SDKAPP_ENGLISH_SPACE + frequencyStr + Resources.FRTC_SDKAPP_ENGLISH_SPACE + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
                                periodStr = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
                            }
                        }
                        else if (recurringType == "weekly")
                        {
                            if (string.IsNullOrEmpty(frequencyStr))
                            {
                                RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_MEETING_RECURRING_WEEKLY;
                            }
                            else
                            {
                                RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + Resources.FRTC_SDKAPP_ENGLISH_SPACE + frequencyStr + Resources.FRTC_SDKAPP_ENGLISH_SPACE + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
                                if (msg.RecurringMeetingGroup.recurrenceInterval > 1)
                                    periodStr = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
                            }

                            foreach (int d in dOfW)
                            {
                                days += GetDayNameInWeek(d - 1);
                                days += Resources.FRTC_SDKAPP_STR_SEPERATE;
                            }
                            days = days.TrimEnd('、');

                            daysInPeriod = Resources.FRTC_SDKAPP_STR_ON;
                        }
                        else if (recurringType == "monthly")
                        {
                            if (string.IsNullOrEmpty(frequencyStr))
                            {
                                RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_MEETING_RECURRING_MONTHLY;
                            }
                            else
                            {
                                RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + Resources.FRTC_SDKAPP_ENGLISH_SPACE + frequencyStr + Resources.FRTC_SDKAPP_ENGLISH_SPACE + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
                                periodStr = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
                            }
                            foreach (int d in dOfM)
                            {
                                days += d.ToString() + Resources.FRTC_SDKAPP_SCHEDULE_DAY_IN_MONTH;
                                days += Resources.FRTC_SDKAPP_STR_SEPERATE;
                            }
                            days = days.TrimEnd(Resources.FRTC_SDKAPP_STR_SEPERATE[0]);

                            daysInPeriod = Resources.FRTC_SDKAPP_STR_ON;
                        }

                        if (string.IsNullOrEmpty(frequencyStr))
                        {
                            RecurringMeetingDesc1 = string.Format(Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_GROUP_DESC1, RecurringMeetingDesc0, periodStr, daysInPeriod, days);
                        }
                        else
                        {
                            frequencyStr = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + Resources.FRTC_SDKAPP_ENGLISH_SPACE + frequencyStr;
                            RecurringMeetingDesc1 = string.Format(Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_GROUP_DESC1, frequencyStr, periodStr, daysInPeriod, days);
                        }

                        DateTime endtimeLocal = UIHelper.GetUTCDateTimeFromUTCTimestamp(msg.RecurringMeetingGroup.recurrenceEndDay.Value).ToLocalTime();
                        string endtimeLocalStr = string.Format(Resources.FRTC_SDKAPP_DATE_YEAR_MONTH_DAY, endtimeLocal.Year, endtimeLocal.Month, endtimeLocal.Day);
                        RecurringMeetingDesc2 = string.Format(Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_GROUP_DESC2,
                                                            endtimeLocalStr,
                                                            msg.RecurringMeetingGroup.meeting_schedules.Count());
                    }
                    string currentMeetingId = string.Empty;
                    bool authed = false;
                    if (CurrentSelectdScheduledMeeting != null)
                    {
                        currentMeetingId = CurrentSelectdScheduledMeeting.ReserveId;
                        authed = CurrentSelectdScheduledMeeting.Authorized;
                    }
                    if (recurringMeetingGroupNextPageIndex == 1)
                    {
                        RecurringMeetingGroup.Clear();
                        LogTool.LogHelper.Debug("RecurringMeetingGroup clear");
                    }
                    foreach (var itm in list)
                    {
                        this.RecurringMeetingGroup.Add(itm);
                        if (itm.ReserveId == currentMeetingId)
                        {
                            CurrentSelectdScheduledMeeting.Authorized = authed;
                        }
                    }

                    this.RecurringFrequence = this.RecurringMeetingGroup[0].RecurringFrequency;
                    if (this.RecurringMeetingGroup[0].RecurringDaysOfMonth != null && this.RecurringMeetingGroup[0].RecurringDaysOfMonth.Count() > 0)
                    {
                        var daysInMonth = this.RecurringMeetingGroup[0].RecurringDaysOfMonth;
                        this._recurringMonthDays.Clear();
                        foreach (var d in daysInMonth)
                        {
                            this._recurringMonthDays.Add(d);
                        }
                        RaisePropertyChanged("RecurringMonthDays");
                    }
                    if (this.RecurringMeetingGroup[0].RecurringDaysOfWeek != null && this.RecurringMeetingGroup[0].RecurringDaysOfWeek.Count() > 0)
                    {
                        var daysInWeek = this.RecurringMeetingGroup[0].RecurringDaysOfWeek;
                        this._recurringWeekDays.Clear();
                        foreach (var d in daysInWeek)
                        {
                            this._recurringWeekDays.Add(d);
                        }
                        RaisePropertyChanged("RecurringWeekDays");
                    }

                    string frequency = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + Resources.FRTC_SDKAPP_ENGLISH_SPACE + (this.RecurringFrequence == 1 ? "" : this.RecurringFrequence.ToString());
                    string period = string.Empty;
                    string desc = string.Empty;
                    switch (type)
                    {
                        case MeetingRecurring.Daily:
                            if (this.RecurringFrequence == 1)
                            {
                                desc = Resources.FRTC_SDKAPP_MEETING_RECURRING_DAILY;
                            }
                            else
                            {
                                period = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
                                desc = frequency.Clone().ToString() + Resources.FRTC_SDKAPP_ENGLISH_SPACE + period;
                            }
                            break;
                        case MeetingRecurring.Weekly:
                            if (this.RecurringFrequence == 1)
                            {
                                desc = Resources.FRTC_SDKAPP_MEETING_RECURRING_WEEKLY;
                            }
                            else
                            {
                                period = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
                                desc = frequency.Clone().ToString() + Resources.FRTC_SDKAPP_ENGLISH_SPACE + period;
                            }
                            break;
                        case MeetingRecurring.Monthly:
                            if (this.RecurringFrequence == 1)
                            {
                                desc = Resources.FRTC_SDKAPP_MEETING_RECURRING_MONTHLY;
                            }
                            else
                            {
                                period = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
                                desc = frequency.Clone().ToString() + Resources.FRTC_SDKAPP_ENGLISH_SPACE + period;
                            }
                            break;
                        default:
                            break;
                    }
                    RecurringFrequencyDesc = desc + string.Format(Resources.FRTC_SDKAPP_MEETING_DETAIL_RECURRING_DESC, this.RecurringMeetingGroup.Count());

                    DateTime localTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(msg.RecurringMeetingGroup.recurrenceEndDay.Value).ToLocalTime();
                    string endDateStr = string.Format(Resources.FRTC_SDKAPP_DATE_YEAR_MONTH_DAY, localTime.Year, localTime.Month, localTime.Day);
                    RecurringMeetingDesc2 = string.Format(Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_GROUP_DESC2,
                                                        endDateStr,
                                                        RecurringMeetingGroup.Count);

                    this.RecurringMeetingEndDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(msg.RecurringMeetingGroup.recurrenceEndDay.Value).ToLocalTime().Date;

                    LogTool.LogHelper.Debug("RecurringMeetingGroup fill");
                    if (recurringMeetingGroupTotalPageCnt > 1)
                        recurringMeetingGroupNextPageIndex++;
                    FetchingMeetings = false;

                    recurringMeetingGroupWnd?.Show();
                    recurringMeetingGroupWnd?.Activate();
                }
            }));
        }

        private void OnMeetingDetail(FRTCScheduledMeetingDetailMessage msg)
        {
            this.ScheduledMeetingNumber = msg.MeetingData.meeting_number;
            this.ScheduledMeetingReservationID = msg.MeetingData.reservation_id;

            bool isRecurring = msg.MeetingData.meeting_type.ToLower() == "recurrence";
            if (isRecurring)
            {
                RecurringFrequence = msg.MeetingData.recurrenceInterval;
                string strType = msg.MeetingData.recurrence_type.ToLower();
                string frequency = RecurringFrequence == 1 ? "" : RecurringFrequence.ToString();
                if (strType == "daily")
                {
                    MeetingRecurring = MeetingRecurring.Daily;
                    RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + frequency + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
                }
                else if (strType == "weekly")
                {
                    MeetingRecurring = MeetingRecurring.Weekly;
                    RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + frequency + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
                }
                else if (strType == "monthly")
                {
                    MeetingRecurring = MeetingRecurring.Monthly;
                    RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + frequency + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
                }
            }

            MeetingPWDInDetailData = msg.MeetingData.meeting_password;
            CallRateInDetailData = msg.MeetingData.call_rate_type;
            JoinInAdvanceTimeInDetailData = msg.MeetingData.time_to_join;

            SelectedCallRate = CallRateOptions.Find((p) => p == CallRateInDetailData);
            JoinMeetingInAdvanceTime = JoinInAdvanceTimeOptions.Find((p) => p.InAdvanceTime == JoinInAdvanceTimeInDetailData);


            MuteMicWhenJoin = msg.MeetingData.mute_upon_entry.ToLower() == "enable";

            this.ScheduleMeetingName = msg.MeetingData.meeting_name;
            this.CurrentTimeZone = TimeZoneInfo.Local.DisplayName;
            this.ScheduleMeetingDescription = msg.MeetingData.meeting_description;
            if (this.CurrentSelectdScheduledMeeting != null)
                this.CurrentSelectdScheduledMeeting.MeetingDescription = msg.MeetingData.meeting_description;

            if (msg.MeetingData.owner_id == m_signInMgr.UserData.user_id)
            {
                if (RoomList != null && RoomList.Count > 0)
                {
                    this.UsePersonalMeetingRoomSchedule = !string.IsNullOrEmpty(msg.MeetingData.meeting_room_id);
                    if (UsePersonalMeetingRoomSchedule)
                    {
                        this.SelectedRoomSchedule = RoomList.Find((room) => { return room.RoomID == msg.MeetingData.meeting_room_id; });
                    }
                }
            }

            this.EnableGuestCall = msg.MeetingData.guest_dial_in;
            this.EnableWaterMark = msg.MeetingData.watermark;
            this.EnableMeetingPWD = !string.IsNullOrEmpty(msg.MeetingData.meeting_password);//this will be ignored by server if use personal meeting room

            if (UsePersonalMeetingRoomSchedule)
                this.EnableMeetingPWD = false; //set to default value for user edit meeting then disable personal room cases

            long startTimestamp = 0;
            if (long.TryParse(msg.MeetingData.schedule_start_time, out startTimestamp))
            {
                _scheduleMeetingStartDate = MeetingStartDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(startTimestamp).ToLocalTime();
                _scheduleMeetingStartTime = MeetingStartTime = new TimeSpan(ScheduleMeetingStartDate.Value.Hour, ScheduleMeetingStartDate.Value.Minute, ScheduleMeetingStartDate.Value.Second);
            }

            long endTimestamp = 0;
            if (long.TryParse(msg.MeetingData.schedule_end_time, out endTimestamp))
            {
                _scheduleMeetingEndDate = MeetingEndDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTimestamp).ToLocalTime();
                _scheduleMeetingEndTime = MeetingEndTime = new TimeSpan(ScheduleMeetingEndDate.Value.Hour, ScheduleMeetingEndDate.Value.Minute, ScheduleMeetingEndDate.Value.Second);
            }

            LogHelper.Debug("Meeting detail === meeting name " + msg.MeetingData.meeting_name + " start timestamp " + msg.MeetingData.schedule_start_time + " end timestamp " + msg.MeetingData.schedule_end_time);

            RenewScheduleMeetingAvailableStartTime();


            ScheduleMeetingStartTimeSelectedIndex = -1;
            ScheduleMeetingEndTimeSelectedIndex = -1;

            if (MeetingStartDate.Value.Date.Add(MeetingStartTime.Value) < DateTime.Now)
            {
                ScheduleMeetingTimeIllegal = true;
            }

            _scheduleMeetingStartDate = MeetingStartDate;
            _scheduleMeetingStartTime = MeetingStartTime;
            _scheduleMeetingEndDate = MeetingEndDate;
            _scheduleMeetingEndTime = MeetingEndTime;


            if (msg.MeetingData.invited_users_details != null)
            {
                this.InviteeList = msg.MeetingData.invited_users_details.Select((user) =>
                {
                    return new ScheduleMeetingInviteeItem() { IsInvited = true, Info = new UserInfo() { username = user.username, user_id = user.user_id } };
                }).ToList();
            }

            if (CurrentSelectdScheduledMeeting != null)
            {
                CurrentSelectdScheduledMeeting.Authorized = CurrentSelectdScheduledMeeting.OwnerId == m_signInMgr.UserData.user_id || m_signInMgr.Role != UserRole.Normal;
            }

            if (isRecurring)
            {
                RecurringMeetingEndDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(msg.MeetingData.recurrenceEndDay.Value).ToLocalTime().Date;
            }
        }

        private void OnUserSingInResult(FRTCAPIResultMessage msg)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                switch (msg.Result)
                {
                    case FRTC_API_RESULT.SIGNIN_SUCCESS:
                    case FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN:
                        nextMeetingListPageIndex = 1;
                        totalMeetingListPageCnt = 0;
                        this.ScheduledMeetingList?.Clear();
                        FetchingMeetings = true;
                        m_scheduleMgr.GetScheduledMeetingList(nextMeetingListPageIndex, 50, m_signInMgr.UserData.user_id);
                        int secondsInCurrentHour = DateTime.Now.Minute * 60 + DateTime.Now.Second;
                        int delayToNextQuarter = secondsInCurrentHour <= 900 ?
                                                900 - secondsInCurrentHour
                                                : 900 - (secondsInCurrentHour % 900);
                        if (delayToNextQuarter > 60)
                            delayToNextQuarter -= 60;
                        else
                            delayToNextQuarter = 0;

                        int interval = 15 * 60 * 1000;

                        _updateScheduleTimerDelayTaskCancel = new CancellationTokenSource();
                        _updateScheduleTimerDelayTask = Task.Delay(TimeSpan.FromSeconds(delayToNextQuarter), _updateScheduleTimerDelayTaskCancel.Token).ContinueWith((t) =>
                        {
                            _updateScheduleTimer = new System.Timers.Timer(interval);
                            _updateScheduleTimer.AutoReset = true;
                            _updateScheduleTimer.Elapsed += new ElapsedEventHandler((s, e) =>
                            {
                                LogTool.LogHelper.Debug("Auto refresh meeting list");
                                if (!_isIncall)
                                    RefreshMeetingListCurrentPage();
                            });
                            _updateScheduleTimer.Start();
                            if (!_isIncall)
                                RefreshMeetingListCurrentPage();
                        }, TaskContinuationOptions.NotOnCanceled);
                        break;
                    case FRTC_API_RESULT.USER_ROOM_SUCCESS:
                        if (m_signInMgr.UserMeetingRoomList != null)
                        {
                            RoomList = m_signInMgr.UserMeetingRoomList.meeting_rooms.Select((r) =>
                            {
                                return new UserMeetingRoomDisplayData()
                                {
                                    RoomName = r.meetingroom_name,
                                    RoomNumber = r.meeting_number,
                                    RoomPWD = r.meeting_password,
                                    RoomID = r.meeting_room_id
                                };
                            }).ToList();
                            if (RoomList.Count > 0)
                            {
                                SelectedRoomSchedule = RoomList[0];
                            }
                        }
                        break;
                    case FRTC_API_RESULT.QUERY_USER_SUCCESS:
                        UserInfoList list = msg.DataObj as UserInfoList;
                        if (list != null)
                        {
                            totalUsersPageCnt = list.total_page_num;
                            totalUserCount = list.total_size;
                            if (currentUsersPageIndex == 1)
                            {
                                this.SearchUserResult.Clear();
                            }
                            foreach (var u in list.users)
                            {
                                bool hasInvited = InvitedUsers.ContainsKey(u.user_id);
                                this.SearchUserResult.Add(new ScheduleMeetingInviteeItem()
                                {
                                    Info = u,
                                    IsInvited = hasInvited
                                });
                            };
                        }
                        break;
                    case FRTC_API_RESULT.SIGNOUT_SUCCESS:
                        this.ClearData();
                        try
                        {
                            _updateScheduleTimerDelayTaskCancel.Cancel();
                        }
                        catch { }
                        this._updateScheduleTimer?.Stop();
                        lock (_reminderQueueLockObj)
                        {
                            this._reminderQueue?.Clear();
                        }
                        this._ignoredMeeting?.Clear();
                        FRTCView.FRTCGlobalReminderToast.CloseGlobalReminder();
                        _reminderCloseTimer?.Stop();
                        break;
                    default:
                        break;
                }
            }));
        }

        private void OnMeetingScheduleError(MeetingScheduleAPIErrorMessage msg)
        {
            string errTip = "{0} " + Properties.Resources.FRTC_MEETING_SDKAPP_FAILED;
            switch (msg.StatusCode)
            {
                case MeetingScheduleStatusCode.Schedule_Meeting_General_Error:
                    errTip = string.Format(errTip, Properties.Resources.FRTC_MEETING_SDKAPP_SCHEDULE_MEETING);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Processing = false;
                    });
                    break;
                case MeetingScheduleStatusCode.Get_Scheduled_Meeting_List_General_Error:
                    errTip = string.Format(errTip, Properties.Resources.FRTC_MEETING_SDKAPP_GET_SCHEDULED_MEETINGS);
                    FetchingMeetings = false;
                    break;
                case MeetingScheduleStatusCode.Get_Scheduled_Meeting_Detail_General_Error:
                    errTip = string.Format(errTip, Properties.Resources.FRTC_MEETING_SDKAPP_GET_MEETING_DETAIL);
                    if (FRTCView.FRTCPopupViewManager.CurrentPopup != null && FRTCView.FRTCPopupViewManager.CurrentPopup is FRTCView.ScheduledMeetingDetailWindow)
                    {
                        FRTCView.FRTCPopupViewManager.CurrentPopup.Close();
                    }
                    RefreshMeetingListCurrentPage();
                    break;
                case MeetingScheduleStatusCode.Update_Scheduled_Meeting_General_Error:
                    errTip = string.Format(errTip, Properties.Resources.FRTC_MEETING_SDKAPP_EDIT_MEETING);
                    break;
                case MeetingScheduleStatusCode.Delete_Scheduled_Meeting_General_Error:
                    errTip = string.Format(errTip, Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_SCHEDULE);
                    break;
                case MeetingScheduleStatusCode.Get_Recurring_Meeting_Group_General_Error:
                    recurringMeetingGroupWnd?.Close();
                    recurringMeetingGroupWnd = null;
                    break;
                case MeetingScheduleStatusCode.Add_Meeting_To_My_List_General_Error:
                    if (_isIncall)
                    {
                        MessengerInstance.Send<NotificationMessage<string>>(new NotificationMessage<string>(Resources.FRTC_SDKAPP_ADD_MEEETING_TO_LIST_FAILED, "add_meeting_to_list_notify"));
                    }
                    else
                        errTip = Resources.FRTC_SDKAPP_ADD_MEEETING_TO_LIST_FAILED;
                    break;
                case MeetingScheduleStatusCode.Remove_Meeting_From_My_List_General_Error:
                    errTip = string.Format(errTip, Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_FROM_MEETING);
                    break;
                case MeetingScheduleStatusCode.SessionToken_Invalid:
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _updateScheduleTimer?.Stop();
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED, Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED_TIP);
                    });
                    m_signInMgr.SignOut();
                    break;
                default:
                    errTip = Properties.Resources.FRTC_MEETING_SDKAPP_OPERATION_FAILED;
                    break;
            }
            MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = errTip });
        }


        string scheduledMeetingID = string.Empty;
        MeetingInviteInfoWindow inviteInfoWindow = null;
        private void OnMeetingScheduled(FRTCMeetingScheduledMessage msg)
        {
            _isRefreshAfterMeetingScheduled = true;
            RefreshScheduledMeetingCommand.Execute(null);
            Processing = false;
            if (msg.MeetingData.meeting_type != "instant")
            {
                scheduledMeetingID = msg.MeetingData.reservation_id;
                App.Current.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!string.IsNullOrEmpty(scheduledMeetingID))
                    {
                        if (inviteInfoWindow != null)
                        {
                            inviteInfoWindow.Close();
                            inviteInfoWindow = null;
                        }

                        if (inviteInfoWindow == null)
                        {
                            inviteInfoWindow = new MeetingInviteInfoWindow();
                            inviteInfoWindow.tbTitle.Visibility = Visibility.Collapsed;
                            inviteInfoWindow.successMsg.Visibility = Visibility.Visible;
                            inviteInfoWindow.tbInvite.Visibility = Visibility.Visible;


                            if (msg.MeetingData.meeting_type == "recurrence")
                            {
                                ScheduledMeetingDislpayData d = MakeMeetingDislpayData(msg.MeetingData);
                                inviteInfoWindow.tbInvite.Text = UIHelper.GetMeetingInvitationText(DisplayName, d);
                            }
                            else
                            {
                                string strStartTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(msg.MeetingData.schedule_start_time)).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                                string strEndTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(msg.MeetingData.schedule_end_time)).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                                inviteInfoWindow.tbInvite.Text = UIHelper.GetMeetingInvitationText(DisplayName, msg.MeetingData.meeting_name, strStartTime, msg.MeetingData.meeting_number, msg.MeetingData.meeting_password, false, strEndTime, msg.MeetingData.meeting_url);
                            }


                            inviteInfoWindow.tbInvite.ToolTip = inviteInfoWindow.tbInvite.Text;
                            inviteInfoWindow.btnCopy.Command = null;
                            inviteInfoWindow.btnCopy.Click += (s, e) =>
                            {
                                try
                                {
                                    Clipboard.SetText(inviteInfoWindow.tbInvite.Text);
                                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE });
                                }
                                catch { };
                            };

                            scheduledMeetingID = string.Empty;
                        }
                        inviteInfoWindow.Owner = App.Current.MainWindow;
                        inviteInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        inviteInfoWindow.Show();
                    }
                }));
            }
        }

        private void OnMeetingUpdated(FRTCEditMeetingSuccessMsg msg)
        {
            if (msg.OperationType == "delete_scheduled_meeting_success")
            {
                lock (m_scheduleMeetingListLockObj)
                {
                    Func<ScheduledMeetingDislpayData, bool> searchFunc = new Func<ScheduledMeetingDislpayData, bool>((data) =>
                    {
                        if (data.ReserveId == msg.ReservationId)
                        {
                            return true;
                        }
                        else if (data.RecurrenceId == msg.ReservationId)
                        {
                            return true;
                        }
                        return false;
                    });
                    Action<ScheduledMeetingDislpayData> action = new Action<ScheduledMeetingDislpayData>((d) =>
                    {
                        if (d.IsRecurringMeeting)
                        {
                            this.RecurringMeetingGroup?.Remove(d);
                        }

                        this.ScheduledMeetingList.Remove(d);

                        foreach (var r in _reminderQueue)
                        {
                            var found = r.ScheduleList.Find(p => p.ReserveId == d.ReserveId);
                            if (found != null)
                            {
                                r.ScheduleList.Remove(found);
                                break;
                            }
                        }
                        var reminder = FRTCGlobalReminderToast.GetReminders();
                        if (reminder != null)
                        {
                            var removed = reminder.FirstOrDefault(p => p.ReserveId == d.ReserveId);
                            if (removed != null)
                            {
                                if (reminder.Count() <= 1)
                                {
                                    FRTCGlobalReminderToast.CloseGlobalReminder();
                                    _reminderCloseTimer?.Stop();
                                }
                                else
                                {
                                    FRTCGlobalReminderToast.RemoveReminder(d);
                                }
                            }
                        }

                    });

                    ScheduledMeetingDislpayData searched = null;

                    if (ScheduledMeetingList.Count > 0)
                    {
                        try
                        {
                            searched = ScheduledMeetingList.FirstOrDefault(searchFunc);
                            if (searched != null)
                            {
                                action(searched);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogTool.LogHelper.Exception(ex);
                        }
                    }
                    if (RecurringMeetingGroup != null && RecurringMeetingGroup.Count > 0)
                    {
                        searched = null;
                        try
                        {
                            searched = RecurringMeetingGroup.FirstOrDefault(searchFunc);
                            if (searched != null)
                            {
                                action(searched);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogTool.LogHelper.Exception(ex);
                        }
                    }
                }
            }
            else if (msg.OperationType == "update_scheduled_meeting_succeess")
            {
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_EDIT_SCHEDULE_SUCCEED });
                try
                {
                    var found = ScheduledMeetingList.FirstOrDefault((p) =>
                    {
                        if (p.ReserveId == msg.ReservationId)
                        {
                            return true;
                        }
                        else if (p.RecurrenceId == msg.ReservationId)
                        {
                            return true;
                        }
                        return false;
                    });
                    if (found != null)
                    {
                        if (_ignoredMeeting != null && _ignoredMeeting.ContainsKey(found.MeetingNumber))
                        {
                            _ignoredMeeting.Remove(found.MeetingNumber);
                        }
                        if (found.IsRecurringMeeting)
                            m_scheduleMgr.GetRecurringMeetingGroup(found.RecurrenceId, 1, 365, string.Empty);
                    }

                }
                catch { }
                RefreshScheduledMeetingCommand.Execute(null);
            }
            else if (msg.OperationType == "update_scheduled_recurring_succeess")
            {
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_EDIT_SCHEDULE_SUCCEED });
                RefreshMeetingListCurrentPage();
                recurringMeetingGroupWnd?.Close();
            }
            else if (msg.OperationType == "add_meeting_to_list_success")
            {
                if (_isIncall)
                {
                    MessengerInstance.Send<NotificationMessage<string>>(new NotificationMessage<string>(Resources.FRTC_SDKAPP_ADD_MEEETING_TO_LIST_SUCCEED, "add_meeting_to_list_notify")); ;
                }
                else
                {
                    RefreshMeetingListCurrentPage();
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Resources.FRTC_SDKAPP_ADD_MEEETING_TO_LIST_SUCCEED });
                }
            }
            else if (msg.OperationType == "remove_meeting_from_list_success")
            {
                RefreshMeetingListCurrentPage();
            }
        }

        private void RefreshMeetingListCurrentPage()
        {
            lock (m_scheduleMeetingListLockObj)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FetchingMeetings = true;
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                    m_scheduleMgr.GetScheduledMeetingList(nextMeetingListPageIndex <= 1 ? 1 : nextMeetingListPageIndex - 1, 50, string.Empty);
                });
            }
        }

        int _recurringMeetingSingleIndex = -1;
        private void ShowUpdateScheduledMeetingDialog(ScheduledMeetingDislpayData data, bool updateSingle = false)
        {
            if (data.IsRecurringMeeting)
            {
                bool updateRecurringByUser = false;
                if (!updateSingle)
                {
                    if (!FRTCMessageBoxBig.ShowUpdateRecurringMeetingOptionWindow(out updateRecurringByUser))
                    {
                        return;
                    }
                }
                IsEditMeetingPage = true;
                _recurringMeetingSingleIndex = RecurringMeetingGroup.IndexOf(data);
                if (updateSingle || !updateRecurringByUser)
                {//update single
                    if (this.RecurringMeetingGroup != null && this.RecurringMeetingGroup.Count() > 0)
                    {
                        string frequency = data.RecurringFrequency == 1 ? "" : data.RecurringFrequency.ToString();
                        if (data.RecurringType == MeetingRecurring.Daily)
                        {
                            RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + frequency + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
                        }
                        else if (data.RecurringType == MeetingRecurring.Weekly)
                        {
                            RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + frequency + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
                        }
                        else if (data.RecurringType == MeetingRecurring.Monthly)
                        {
                            RecurringMeetingDesc0 = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + frequency + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
                        }

                        DateTime localTime = RecurringMeetingEndDate.Value.ToLocalTime();
                        string endDateStr = string.Format(Resources.FRTC_SDKAPP_DATE_YEAR_MONTH_DAY, localTime.Year, localTime.Month, localTime.Day);
                        RecurringMeetingDesc2 = string.Format(Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_GROUP_DESC2,
                                        endDateStr,
                                        this.RecurringMeetingGroup?.Count);

                        SetUpdateRecurringMeetingSingleAvailableStartDate(data);
                    }

                    if (FRTCView.FRTCPopupViewManager.CurrentPopup != null)
                    {
                        FRTCView.FRTCPopupViewManager.CurrentPopup.Closed += new EventHandler((s, e) =>
                        {
                            m_scheduleMgr.GetScheduledMeetingDetail(data.ReserveId);
                            this.ScheduledMeetingReservationID = data.ReserveId;
                            this.ScheduledMeetingNumber = data.MeetingNumber;
                            MeetingPWDInDetailData = data.Password;
                            FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCUpdateRecurringMeetingSingle, null);
                            ClearMeetingDetailData();
                            this.CurrentSelectdScheduledMeeting = null;
                            this.InviteeList.Clear();
                            this.SearchUserResult?.Clear();
                            this.InvitedUsers?.Clear();
                            this.InviteeListTemp?.Clear();
                            IsEditMeetingPage = false;
                            _recurringMeetingSingleIndex = -1;
                        });
                        FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                    }
                    else
                    {
                        m_scheduleMgr.GetScheduledMeetingDetail(data.ReserveId);
                        this.ScheduledMeetingReservationID = data.ReserveId;
                        this.ScheduledMeetingNumber = data.MeetingNumber;
                        FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCUpdateRecurringMeetingSingle, null);
                        ClearMeetingDetailData();
                        this.CurrentSelectdScheduledMeeting = null;
                        this.InviteeList.Clear();
                        this.SearchUserResult?.Clear();
                        this.InvitedUsers?.Clear();
                        this.InviteeListTemp?.Clear();
                        IsEditMeetingPage = false;
                        _recurringMeetingSingleIndex = -1;
                    }

                }
                else//update recurrence
                {
                    _currentLookupRecurringMeeting = data;
                    m_scheduleMgr.GetRecurringMeetingGroup(data.RecurrenceId, 1, 365, string.Empty);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PopEditRecurringMeetingGroupCommand.Execute(null);
                    });
                }
            }
            else
            {
                IsEditMeetingPage = true;
                if (FRTCView.FRTCPopupViewManager.CurrentPopup != null)
                {
                    FRTCView.FRTCPopupViewManager.CurrentPopup.Closed += new EventHandler((s, e) =>
                    {
                        this.ScheduledMeetingReservationID = data.ReserveId;
                        this.ScheduledMeetingNumber = data.MeetingNumber;
                        MeetingPWDInDetailData = data.Password;
                        FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCScheduleMeeting, null);
                        ClearMeetingDetailData();
                        this.CurrentSelectdScheduledMeeting = null;
                        this.InviteeList.Clear();
                        this.SearchUserResult?.Clear();
                        this.InvitedUsers?.Clear();
                        this.InviteeListTemp?.Clear();
                        IsEditMeetingPage = false;
                    });
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                }
                else
                {
                    this.ScheduledMeetingReservationID = data.ReserveId;
                    this.ScheduledMeetingNumber = data.MeetingNumber;
                    m_scheduleMgr.GetScheduledMeetingDetail(data.ReserveId);
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCScheduleMeeting, null);
                    ClearMeetingDetailData();
                    this.CurrentSelectdScheduledMeeting = null;
                    this.InviteeList.Clear();
                    this.SearchUserResult?.Clear();
                    this.InvitedUsers?.Clear();
                    this.InviteeListTemp?.Clear();
                    IsEditMeetingPage = false;
                }
            }
        }

        private void ClearMeetingDetailData()
        {
            this.MeetingPWDInDetailData = string.Empty;
            this.ScheduledMeetingNumber = string.Empty;
            this.ScheduledMeetingReservationID = string.Empty;
            this.UsePersonalMeetingRoomSchedule = false;
            this.SelectedRoomSchedule = null;
            this.EnableWaterMark = false;
            this.EnableGuestCall = true;
            this.EnableMeetingPWD = false;
            this.MuteMicWhenJoin = false;
            this.MeetingStartDate = null;
            this.MeetingStartTime = null;
            this.ScheduleMeetingStartTime = null;
            this.MeetingEndDate = null;
            this.MeetingEndTime = null;
            this.ScheduleMeetingTimeIllegal = false;
            this.SelectedCallRate = CallRateOptions[3];
            this.Recurring = string.Empty;
            this.RecurringFrequence = 0;
            this.MeetingRecurring = MeetingRecurringOptions[0].Recurring;
            this.RecurringWeekDays.Clear();
            this.RecurringMonthDays.Clear();
        }
        public void ClearInviteeTempData()
        {
            currentUsersPageIndex = 1;
            totalUsersPageCnt = 1;
            SearchUserPattern = string.Empty;
            SearchUserResult.Clear();
            InvitedUsers.Clear();
            InviteeListTemp.Clear();
        }
        private string GetDisplayMeetingTimeString(DateTime StartTime, DateTime EndTime)
        {
            string start = string.Empty, end = string.Empty;
            return GetDisplayMeetingTimeString(StartTime, EndTime, ref start, ref end);
        }
        private string GetDisplayMeetingTimeString(DateTime StartTime, DateTime EndTime, ref string DisplayStartTime, ref string DisplayEndTime)
        {
            string displayBeginTime = string.Empty, displayEndTime = string.Empty, displayMeetingTime = string.Empty;
            try
            {
                if (StartTime.Date == DateTime.Now.Date)
                {
                    displayBeginTime = Resources.FRTC_MEETING_SDKAPP_TODAY + " " + StartTime.ToString("HH:mm");
                }
                else
                {
                    displayBeginTime = StartTime.ToString("yyyy-MM-dd HH:mm");
                }
                if (DisplayStartTime != null)
                    DisplayStartTime = displayBeginTime;

                if (EndTime.Date == DateTime.Now.Date)
                {
                    if (EndTime.Date == StartTime.Date)
                        displayEndTime = EndTime.ToString("HH:mm");
                    else
                        displayEndTime = Resources.FRTC_MEETING_SDKAPP_TODAY + " " + EndTime.ToString("HH:mm");
                }
                else if (EndTime.Date.Year == DateTime.Now.Year)
                {
                    displayEndTime = EndTime.ToString("MM-dd HH:mm");
                }
                else
                {
                    displayEndTime = EndTime.ToString("yyyy-MM-dd HH:mm");
                }
                if (DisplayEndTime != null)
                    DisplayEndTime = displayEndTime;

                displayMeetingTime = displayBeginTime + " - " + displayEndTime;
            }
            catch (Exception ex) { throw ex; }
            return displayMeetingTime;
        }
        private void RenewScheduleMeetingAvailableStartTime()
        {
            TimeSpan first = TimeSpan.Zero;
            int lastHour = 24;
            if (ScheduleMeetingStartDate.Value.Date == DateTime.Today)
            {
                if (DateTime.Now.Minute < 30)
                {
                    first = DateTime.Now.AddMinutes(30 - DateTime.Now.Minute).Subtract(DateTime.Today);
                }
                else
                {
                    first = DateTime.Now.AddMinutes(60 - DateTime.Now.Minute).Subtract(DateTime.Today);
                }
                lastHour = 24 - DateTime.Now.Hour;
            }

            List<string> tmp = new List<string>();
            tmp.Add(first.ToString("hh\\:mm"));
            for (int i = 1; i < lastHour * 2; i++)
            {
                if (tmp[i - 1] == "23:30")
                {
                    break;
                }
                tmp.Add(TimeSpan.Parse(tmp[i - 1]).Add(TimeSpan.FromMinutes(30)).ToString("hh\\:mm"));
            }
            _scheduleMeetingAvailableStartTime = tmp;


            if (!IsEditMeetingPage && !IsRecurringMeeting)
            {
                bool isStartLaterThan23 = TimeSpan.Parse(_scheduleMeetingAvailableStartTime[0]) >= new TimeSpan(23, 0, 0);
                if (isStartLaterThan23 || ScheduleMeetingStartDate > ScheduleMeetingEndDate)
                {
                    _scheduleMeetingEndDate = DateTime.Today.AddDays(1);
                }
                else if (!isStartLaterThan23 || ScheduleMeetingStartDate < ScheduleMeetingEndDate)
                {
                    _scheduleMeetingEndDate = ScheduleMeetingStartDate;
                }
            }

            TimeSpan start = _scheduleMeetingStartTime.HasValue ? _scheduleMeetingStartTime.Value : new TimeSpan();
            RaisePropertyChanged("ScheduleMeetingAvailableStartTime");
            RaisePropertyChanged("ScheduleMeetingStartDate");

            if (_scheduleMeetingAvailableStartTime.Contains(start.ToString("hh\\:mm")))
            {
                ScheduleMeetingStartTimeSelectedIndex = -1;
                ScheduleMeetingStartTimeSelectedIndex = _scheduleMeetingAvailableStartTime.IndexOf(start.ToString("hh\\:mm"));
            }
            else
            {
                ScheduleMeetingStartTimeSelectedIndex = -1;
                DateTime startTime = DateTime.Parse(_scheduleMeetingAvailableStartTime[0]);
                MeetingStartTime = new TimeSpan(startTime.Hour, startTime.Minute, 0);
                ScheduleMeetingStartTimeSelectedIndex = 0;
            }
        }
        private void RenewScheduleMeetingAvailableEndTime()
        {
            if (!ScheduleMeetingStartDate.HasValue || !ScheduleMeetingEndDate.HasValue)
                return;
            if (ScheduleMeetingAvailableStartTime == null)
                return;

            if (ScheduleMeetingStartDate.HasValue && ScheduleMeetingStartTime.HasValue)
            {
                if (ScheduleMeetingStartDate.Value.Date.Add(ScheduleMeetingStartTime.Value) < DateTime.Now)
                {
                    ScheduleMeetingTimeIllegal = true;
                }
                else
                {
                    ScheduleMeetingTimeIllegal = false;
                }
            }

            ScheduleMeetingEndDateMin = ScheduleMeetingStartDate;
            ScheduleMeetingEndDateMax = ScheduleMeetingStartDate.Value.AddDays(1).Date;

            if (IsEditMeetingPage && RecurringMeetingGroup != null && _currentLookupRecurringMeeting != null && ScheduleMeetingStartDate.HasValue)
            {
                DateTime startDate = ScheduleMeetingStartDate.Value;
                ScheduleMeetingEndDateMax = (UpdateRecurringMeetingSingleLastStartDate > startDate) ? ScheduleMeetingEndDateMax : startDate;
            }

            if (ScheduleMeetingEndDate.Value.Date <= ScheduleMeetingStartDate.Value.Date)
            {
                _scheduleMeetingEndDate = ScheduleMeetingStartDate;
                if (ScheduleMeetingStartTime.HasValue)
                    _scheduleMeetingEndTime = ScheduleMeetingStartTime.Value.Add(TimeSpan.FromMinutes(30));
                else
                    _scheduleMeetingEndTime = TimeSpan.Parse(ScheduleMeetingAvailableStartTime[0]).Add(TimeSpan.FromMinutes(30));

                if (ScheduleMeetingEndTime.Value.Days > 0)
                {
                    _scheduleMeetingEndDate = _scheduleMeetingEndDate.Value.AddDays(1);
                    ScheduleMeetingEndDateMin = ScheduleMeetingEndDate;

                    if (ScheduleMeetingStartTime.HasValue)
                    {
                        ScheduleMeetingEndDateMax = ScheduleMeetingEndDate.Value.AddMinutes(ScheduleMeetingStartTime.Value.TotalMinutes).Date;
                    }
                    else
                    {
                        ScheduleMeetingEndDateMax = ScheduleMeetingEndDate.Value.AddMinutes(TimeSpan.Parse(ScheduleMeetingAvailableStartTime[0]).TotalMinutes).Date;
                    }
                    _scheduleMeetingEndTime = TimeSpan.Zero;
                }
                List<string> tmp = new List<string>();
                TimeSpan needToAdd = ScheduleMeetingEndTime.Value;
                while (needToAdd <= TimeSpan.FromMinutes(23 * 60 + 30))
                {
                    tmp.Add(needToAdd.ToString("hh\\:mm"));
                    needToAdd = needToAdd.Add(TimeSpan.FromMinutes(30));
                    if (needToAdd.Days > 0)
                        break;
                }
                _scheduleMeetingAvailableEndTime = tmp;

            }
            else if (ScheduleMeetingEndDate.Value.Date > ScheduleMeetingStartDate.Value.Date)
            {
                if (ScheduleMeetingEndDate.Value.Date.Subtract(ScheduleMeetingStartDate.Value.Date).Days > 1)
                {
                    _scheduleMeetingEndDate = ScheduleMeetingStartDate.Value.AddDays(1);
                    _scheduleMeetingEndTime = TimeSpan.Zero;
                }

                List<string> tmp = new List<string>();
                TimeSpan needToAdd = TimeSpan.Zero;
                TimeSpan beginTimeSpan = ScheduleMeetingStartTime.HasValue ? ScheduleMeetingStartTime.Value : TimeSpan.Parse(ScheduleMeetingAvailableStartTime[0]);
                if (MeetingRecurring != MeetingRecurring.NoRecurring
                    && _currentLookupRecurringMeeting != null
                    && ScheduleMeetingEndDate.Value.Date.Subtract(ScheduleMeetingStartDate.Value.Date).Days == 1)
                {
                    DateTime beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(_currentLookupRecurringMeeting.BeginTime)).ToLocalTime();
                    beginTimeSpan = new TimeSpan(beginTime.Hour, 0, 0);
                }

                while (needToAdd < beginTimeSpan)
                {
                    tmp.Add(needToAdd.ToString("hh\\:mm"));
                    needToAdd = needToAdd.Add(TimeSpan.FromMinutes(30));
                    if (needToAdd.Days > 0)
                        break;
                }
                _scheduleMeetingAvailableEndTime = tmp;
            }

            TimeSpan end = _scheduleMeetingEndTime.HasValue ? _scheduleMeetingEndTime.Value : new TimeSpan();
            RaisePropertyChanged("ScheduleMeetingAvailableEndTime");
            RaisePropertyChanged("ScheduleMeetingEndDate");
            if (_scheduleMeetingAvailableEndTime != null && _scheduleMeetingAvailableEndTime.Count > 0)
            {
                if (_scheduleMeetingAvailableEndTime.Contains(end.ToString("hh\\:mm")))
                {
                    ScheduleMeetingEndTimeSelectedIndex = -1;
                    ScheduleMeetingEndTimeSelectedIndex = _scheduleMeetingAvailableEndTime.IndexOf(end.ToString("hh\\:mm"));
                }
                else
                {
                    ScheduleMeetingEndTimeSelectedIndex = -1;
                    DateTime endTime = DateTime.Parse(_scheduleMeetingAvailableEndTime[0]);
                    MeetingEndTime = new TimeSpan(endTime.Hour, endTime.Minute, 0);
                    ScheduleMeetingEndTimeSelectedIndex = 0;
                }
            }

        }
        private MeetingScheduleInfo CreateMeetingScheduleInfo()
        {
            MeetingScheduleInfo scheduleInfo = new MeetingScheduleInfo();
            scheduleInfo.meeting_type = MeetingRecurring == MeetingRecurring.NoRecurring ? "reservation" : "recurrence";
            scheduleInfo.meeting_name = ScheduleMeetingName;

            scheduleInfo.meeting_description = ScheduleMeetingDescription;

            string startTime = ((long)ScheduleMeetingStartDate.Value.Date.Add(ScheduleMeetingStartTime.Value).ToUniversalTime().
                                    Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds).ToString();
            string endTime = ((long)ScheduleMeetingEndDate.Value.Date.Add(ScheduleMeetingEndTime.Value).ToUniversalTime().
                                    Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds).ToString();


            scheduleInfo.schedule_start_time = startTime;
            scheduleInfo.schedule_end_time = endTime;

            LogTool.LogHelper.Debug("schedule meeting start timestamp " + scheduleInfo.schedule_start_time);
            LogTool.LogHelper.Debug("schedule meeting end timestamp " + scheduleInfo.schedule_end_time);

            if (UsePersonalMeetingRoomSchedule && SelectedRoomSchedule != null)
                scheduleInfo.meeting_room_id = SelectedRoomSchedule.RoomID;

            scheduleInfo.invited_users = InviteeList.Where((p) => { return p.IsInvited; }).Select((p) => { return p.Info.user_id; }).ToArray();
            scheduleInfo.call_rate_type = SelectedCallRate;
            if (string.IsNullOrEmpty(SelectedCallRate))
            {
                scheduleInfo.call_rate_type = CallRateOptions[3];
            }

            scheduleInfo.time_to_join = JoinMeetingInAdvanceTime.InAdvanceTime;

            scheduleInfo.mute_upon_entry = MuteMicWhenJoin ? "ENABLE" : "DISABLE";
            scheduleInfo.guest_dial_in = EnableGuestCall;
            scheduleInfo.watermark = EnableWaterMark;
            scheduleInfo.watermark_type = "single";

            if (!UsePersonalMeetingRoomSchedule)
            {
                if (EnableMeetingPWD)
                {
                    if (string.IsNullOrEmpty(MeetingPWDInDetailData))
                    {
                        Random generator = new Random();
                        scheduleInfo.meeting_password = generator.Next(0, 1000000).ToString("D6");
                    }
                    else
                    {
                        scheduleInfo.meeting_password = MeetingPWDInDetailData;
                    }
                }
                else
                    scheduleInfo.meeting_password = string.Empty;
            }
            else
                scheduleInfo.meeting_password = null;

            if (MeetingRecurring != MeetingRecurring.NoRecurring)
            {
                string type = "DAILY";
                int interval = RecurringFrequence;
                if (MeetingRecurring == MeetingRecurring.Weekly)
                {
                    type = "WEEKLY";
                    int dofW = (int)ScheduleMeetingStartDate.Value.Date.DayOfWeek + 1;
                    if (!_recurringWeekDays.Contains(dofW))
                    {
                        _recurringWeekDays.Add(dofW);
                    }
                    _recurringWeekDays.Sort();
                    scheduleInfo.recurrenceDaysOfWeek = _recurringWeekDays.ToArray();
                }
                else if (MeetingRecurring == MeetingRecurring.Monthly)
                {
                    type = "MONTHLY";
                    int dofM = ScheduleMeetingStartDate.Value.Date.Day;
                    if (!_recurringMonthDays.Contains(dofM))
                    {
                        _recurringMonthDays.Add(dofM);
                    }
                    _recurringMonthDays.Sort();
                    scheduleInfo.recurrenceDaysOfMonth = _recurringMonthDays.ToArray();
                }


                scheduleInfo.recurrence_type = type;
                scheduleInfo.recurrenceInterval = interval.ToString();

                scheduleInfo.recurrenceStartDay = ScheduleMeetingStartDate.Value.Date.ToUniversalTime().
                                    Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds.ToString();
                scheduleInfo.recurrenceEndDay = RecurringMeetingEndDate.Value.Date.ToUniversalTime().
                                    Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc))
                                    .Add(new TimeSpan(23, 59, 59)).TotalMilliseconds.ToString();

                scheduleInfo.recurrenceStartTime = startTime;
                scheduleInfo.recurrenceEndTime = endTime;
            }

            return scheduleInfo;
        }

        private MeetingScheduleInfo CreateUpdateRecurringMeetingSingleInfo()
        {
            MeetingScheduleInfo scheduleInfo = new MeetingScheduleInfo();
            scheduleInfo.meeting_type = MeetingRecurring == MeetingRecurring.NoRecurring ? "reservation" : "recurrence";
            scheduleInfo.meeting_name = ScheduleMeetingName;
            scheduleInfo.meeting_description = ScheduleMeetingDescription;
            string recurrenceId = CurrentSelectdScheduledMeeting?.RecurrenceId;
            if (string.IsNullOrEmpty(recurrenceId))
            {
                recurrenceId = CurrentLookupRecurringMeeting.RecurrenceId;
            }
            scheduleInfo.recurrenceId = recurrenceId;

            string startTime = ((long)ScheduleMeetingStartDate.Value.Date.Add(ScheduleMeetingStartTime.Value).ToUniversalTime().
                                    Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds).ToString();
            string endTime = ((long)ScheduleMeetingEndDate.Value.Date.Add(ScheduleMeetingEndTime.Value).ToUniversalTime().
                                    Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds).ToString();


            scheduleInfo.schedule_start_time = startTime;
            scheduleInfo.schedule_end_time = endTime;
            scheduleInfo.call_rate_type = CallRateInDetailData;
            scheduleInfo.time_to_join = JoinInAdvanceTimeInDetailData;

            scheduleInfo.mute_upon_entry = MuteMicWhenJoin ? "ENABLE" : "DISABLE";

            LogTool.LogHelper.Debug("UpdateRecurringMeeting start timestamp " + scheduleInfo.schedule_start_time);
            LogTool.LogHelper.Debug("UpdateRecurringMeeting end timestamp " + scheduleInfo.schedule_end_time);

            return scheduleInfo;
        }

        internal struct ReminderQueueItem
        {
            public DateTime RemindTime;
            public List<ScheduledMeetingDislpayData> ScheduleList;
        }

        Queue<ReminderQueueItem> _reminderQueue;
        ReminderQueueItem _tailItem;
        object _reminderQueueLockObj = new object();
        DispatcherTimer _reminderCloseTimer;
        private void CreateReminderQueue()
        {
            lock (_reminderQueueLockObj)
            {
                var reminds = from m in ScheduledMeetingList
                              where
                              m.MeetingType != "instant"
                              && !_ignoredMeeting.ContainsKey(m.ReserveId)
                              && UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(m.BeginTime), true) > DateTime.Now.ToUniversalTime()
                              orderby long.Parse(m.BeginTime)
                              select ((ScheduledMeetingDislpayData)m.Clone()).UseDetailMeetingTime();

                if (reminds.Count() > 0)
                {
                    _reminderQueue = new Queue<ReminderQueueItem>(reminds.Count());
                    for (int i = 0; i < reminds.Count(); ++i)
                    {
                        long begin = long.Parse(reminds.ElementAt(i).BeginTime);
                        var beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(begin, true);
                        if (i == 0)
                        {
                            _tailItem = new ReminderQueueItem() { RemindTime = beginTime, ScheduleList = new List<ScheduledMeetingDislpayData>() { reminds.ElementAt(i) } };
                            _reminderQueue.Enqueue(_tailItem);
                        }
                        else if (i > 0)
                        {
                            if (beginTime == UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(reminds.ElementAt(i - 1).BeginTime), true))
                            {
                                _tailItem.ScheduleList.Add(reminds.ElementAt(i));
                            }
                            else
                            {
                                _tailItem = new ReminderQueueItem() { RemindTime = beginTime, ScheduleList = new List<ScheduledMeetingDislpayData>() { reminds.ElementAt(i) } };
                                _reminderQueue.Enqueue(_tailItem);
                            }
                        }
                    }
                }
            }
        }

        Task _reminderTask;
        CancellationTokenSource _cancel;
        object _reminderTimerLock = new object();
        private void StartReminderTimer()
        {
            lock (_reminderTimerLock)
            {
                try
                {
                    if (_reminderTask != null && _cancel != null && !_cancel.IsCancellationRequested && _cancel.Token.CanBeCanceled)
                    {
                        LogTool.LogHelper.Debug("Reminder task {0} will be cancelled", _reminderTask.Id);
                        _cancel.Cancel();
                        _reminderCloseTimer?.Stop();
                    }
                }
                catch (ObjectDisposedException e)
                {
                    LogTool.LogHelper.Exception(e);
                }
                if (_reminderQueue != null && _reminderQueue.Count > 0)
                {
                    _cancel = new CancellationTokenSource();
                    var delayTime = _reminderQueue.Peek().RemindTime.Subtract(DateTime.Now.ToUniversalTime());
                    if (delayTime.TotalMinutes <= 5)
                        delayTime = TimeSpan.FromSeconds(1);
                    else
                        delayTime = delayTime.Subtract(TimeSpan.FromMinutes(5));

                    LogHelper.Debug("Next reminder delay time: {0}", delayTime.TotalSeconds);

                    if (delayTime.TotalMilliseconds < int.MaxValue)
                    {
                        _reminderTask = Task.Delay(delayTime, _cancel.Token).ContinueWith(
                            new Action<Task>((t) =>
                            {
                                try
                                {
                                    if (_cancel.IsCancellationRequested)
                                    {
                                        LogTool.LogHelper.Debug("Reminder task {0} cancelled", t.Id);
                                        _cancel.Token.ThrowIfCancellationRequested();
                                    }
                                    else
                                    {
                                        LogTool.LogHelper.Debug("Reminder task {0} showing reminder", t.Id);
                                        ShowMeetingReminder();
                                        Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith((ttt) => StartReminderTimer());
                                    }
                                }
                                catch (OperationCanceledException) { }
                                catch (ObjectDisposedException eex)
                                { LogTool.LogHelper.Exception(eex); }
                                finally
                                {
                                    _cancel.Dispose();
                                }
                            }), TaskContinuationOptions.NotOnCanceled);
                    }
                }
            }
        }

        Dictionary<string, ScheduledMeetingDislpayData> _ignoredMeeting = new Dictionary<string, ScheduledMeetingDislpayData>();

        private void ShowMeetingReminder()
        {
            if (_reminderQueue.Count > 0)
            {
                var reminder = _reminderQueue.Dequeue();
                if (_isIncall && !string.IsNullOrEmpty(_inCallMeetingNum))
                {
                    var found = reminder.ScheduleList.Find((p) => { return p.MeetingNumber == _inCallMeetingNum; });
                    if (found != null)
                    {
                        reminder.ScheduleList.Remove(found);
                    }
                }
                if (reminder.ScheduleList.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        _reminderCloseTimer?.Stop();
                        FRTCView.FRTCGlobalReminderToast.ShowGlobalReminder(reminder.ScheduleList);
                        if (_reminderQueue.Count == 0)
                        {
                            if (_cancel != null && !_cancel.IsCancellationRequested)
                                _cancel.Cancel();
                        }
                    }));
                    foreach (var m in reminder.ScheduleList)
                    {
                        if (!_ignoredMeeting.ContainsKey(m.ReserveId))
                            _ignoredMeeting.Add(m.ReserveId, m);
                    }

                }
            }
        }

        private void SaveToCalendar(ScheduledMeetingDislpayData data)
        {
            if (data == null || data.IsEmpty())
                return;
            IICalendar iCal = new iCalendar();
            iCal.AddLocalTimeZone();


            var ev = iCal.Create<Event>();
            ev.Summary = data.MeetingName + Environment.NewLine + data.MeetingUrl;
            ev.Start = new iCalDateTime(UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(data.BeginTime)).ToLocalTime());
            ev.End = new iCalDateTime(UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(data.EndTime)).ToLocalTime());
            string description = string.Empty;

            if (data.IsRecurringMeeting)
            {
                description = UIHelper.GetMeetingInvitationText(data.OwnerName, data);

                //outlook does not support monthly multiple days recurrence event
                if (!(data.RecurringType == MeetingRecurring.Monthly && data.RecurringDaysOfMonth.Count() > 1))
                {
                    var recurrenceRule = new RecurrencePattern((FrequencyType)((int)data.RecurringType + 3), interval: data.RecurringFrequency)
                    {
                        Until = UIHelper.GetUTCDateTimeFromUTCTimestamp(data.RecurringEndDate).ToLocalTime().Date.AddHours(23).AddMinutes(59).AddSeconds(59),
                    };
                    if (data.RecurringType == MeetingRecurring.Weekly)
                    {
                        recurrenceRule.FirstDayOfWeek = DayOfWeek.Sunday;
                        recurrenceRule.ByDay = data.RecurringDaysOfWeek.Select<int, IWeekDay>((w) => { return new WeekDay((DayOfWeek)(w - 1)); }).ToList();
                    }
                    else if (data.RecurringType == MeetingRecurring.Monthly)
                    {
                        recurrenceRule.ByMonthDay = data.RecurringDaysOfMonth;
                    }
                    ev.RecurrenceRules = new List<IRecurrencePattern> { recurrenceRule };
                }
            }
            else
            {
                description = UIHelper.GetMeetingInvitationText(data.OwnerName, data.MeetingName, data.BeginTimeStr, data.MeetingNumber, data.Password, data.IsRecurringMeeting, data.EndTimeStr, data.MeetingUrl);
            }
            ev.Description = description;
            ev.Priority = 7;
            ev.Alarms.Add(new Alarm() { Action = AlarmAction.Display, Description = ev.Summary, Trigger = new DDay.iCal.Trigger(TimeSpan.FromMinutes(-5)) });

            iCalendarSerializer serializer = new iCalendarSerializer();
            string iCalFilePath = System.IO.Path.GetTempPath() + data.MeetingNumber + data.BeginTime + ".ics";

            LogTool.LogHelper.Debug("Create calendar file path {0}", iCalFilePath);
            serializer.Serialize(iCal, iCalFilePath);
            LogTool.LogHelper.Debug("start calendar file path {0}", iCalFilePath);
            Process p = Process.Start(iCalFilePath);
        }


        int totalUserCount = 0;
        int totalUsersPageCnt = 1;
        int currentUsersPageIndex = 1;

        private string _searchUserPattern = string.Empty;
        public string SearchUserPattern
        {
            get { return _searchUserPattern; }
            set
            {
                if (_searchUserPattern != value)
                {
                    _searchUserPattern = value;
                    RaisePropertyChanged("SearchUserPattern");
                    SearchUserResult.Clear();
                    currentUsersPageIndex = 1;
                    m_signInMgr.QueryUsers(currentUsersPageIndex, 50, _searchUserPattern);
                }
            }
        }

        private RelayCommand _frtcScheduleMeetingCommand;
        public RelayCommand FRTCScheduleMeetingCommand
        {
            get { return _frtcScheduleMeetingCommand; }
            set
            {
                _frtcScheduleMeetingCommand = value;
            }
        }

        private RelayCommand _frtcPopupUpdateScheduledMeetingDialog;
        public RelayCommand FRTCPopupUpdateScheduledMeetingDialog
        {
            get { return _frtcPopupUpdateScheduledMeetingDialog; }
            set
            {
                _frtcPopupUpdateScheduledMeetingDialog = value;
            }
        }

        private RelayCommand<ScheduledMeetingDislpayData> _frtcPopupUpdateScheduledMeetingDialogFromList;
        public RelayCommand<ScheduledMeetingDislpayData> FRTCPopupUpdateScheduledMeetingDialogFromList
        {
            get { return _frtcPopupUpdateScheduledMeetingDialogFromList; }
            set
            {
                _frtcPopupUpdateScheduledMeetingDialogFromList = value;
            }
        }

        private RelayCommand _frtcUpdateScheduledMeetingCommand;
        public RelayCommand FRTCUpdateScheduledMeetingCommand
        {
            get { return _frtcUpdateScheduledMeetingCommand; }
            set
            {
                _frtcUpdateScheduledMeetingCommand = value;
            }
        }

        private SQMeetingCommand<string, ScheduledMeetingDislpayData> _frtcScheduleMeetingMenuCommand;
        public SQMeetingCommand<string, ScheduledMeetingDislpayData> FRTCScheduleMeetingMenuCommand
        {
            get { return _frtcScheduleMeetingMenuCommand; }
            set
            {
                _frtcScheduleMeetingMenuCommand = value;
            }
        }

        object m_scheduleMeetingListLockObj = new object();

        private ObservableCollection<ScheduledMeetingDislpayData> _scheduledMeetingList = new ObservableCollection<ScheduledMeetingDislpayData>();
        public ObservableCollection<ScheduledMeetingDislpayData> ScheduledMeetingList
        {
            get { return _scheduledMeetingList; }
            set
            {
                _scheduledMeetingList = value;
                RaisePropertyChanged("ScheduledMeetingList");
            }
        }

        object m_recurringMeetingGroupLockObj = new object();

        private ObservableCollection<ScheduledMeetingDislpayData> _recurringMeetingGroup = new ObservableCollection<ScheduledMeetingDislpayData>();
        public ObservableCollection<ScheduledMeetingDislpayData> RecurringMeetingGroup
        {
            get { return _recurringMeetingGroup; }
            set
            {
                _recurringMeetingGroup = value;
                RaisePropertyChanged("RecurringMeetingGroup");
            }
        }

        private string _recurringFrequencyDesc = string.Empty;
        public string RecurringFrequencyDesc
        {
            get => _recurringFrequencyDesc;
            set { _recurringFrequencyDesc = value; RaisePropertyChanged(); }
        }

        private string _recurringMeetingDesc0 = string.Empty;
        public string RecurringMeetingDesc0
        {
            get => _recurringMeetingDesc0;
            set { _recurringMeetingDesc0 = value; RaisePropertyChanged(); }
        }

        private string _recurringMeetingDesc1 = string.Empty;
        public string RecurringMeetingDesc1
        {
            get => _recurringMeetingDesc1;
            set { _recurringMeetingDesc1 = value; RaisePropertyChanged(); }
        }

        private string _recurringMeetingDesc2 = string.Empty;
        public string RecurringMeetingDesc2
        {
            get => _recurringMeetingDesc2;
            set { _recurringMeetingDesc2 = value; RaisePropertyChanged(); }
        }

        private ScheduledMeetingDislpayData _currentLookupRecurringMeeting = null;
        public ScheduledMeetingDislpayData CurrentLookupRecurringMeeting
        {
            get => _currentLookupRecurringMeeting;
        }

        private RelayCommand _refreshScheduledMeetingCommand;
        public RelayCommand RefreshScheduledMeetingCommand
        {
            get => _refreshScheduledMeetingCommand;
            set => _refreshScheduledMeetingCommand = value;
        }

        private bool _isEditMeetingPage = false;
        public bool IsEditMeetingPage
        {
            get => _isEditMeetingPage;
            set
            {
                _isEditMeetingPage = value;
                RaisePropertyChanged();
            }
        }

        private ScheduledMeetingDislpayData _currentSelectdScheduledMeeting;
        public ScheduledMeetingDislpayData CurrentSelectdScheduledMeeting
        {
            get => _currentSelectdScheduledMeeting;
            set
            {
                _currentSelectdScheduledMeeting = value;
                RaisePropertyChanged("CurrentSelectdScheduledMeeting");
            }
        }


        private RelayCommand<bool> _queryScheduledMeetingNextPageCommand;
        public RelayCommand<bool> QueryScheduledMeetingNextPageCommand
        {
            get => _queryScheduledMeetingNextPageCommand;
            set => _queryScheduledMeetingNextPageCommand = value;
        }

        private RelayCommand<ScheduledMeetingDislpayData> _joinScheduledMeetingCommand;
        public RelayCommand<ScheduledMeetingDislpayData> JoinScheduledMeetingCommand
        {
            get
            {
                return _joinScheduledMeetingCommand;
            }
        }

        private RelayCommand _joinRecurringMeetingCommand;
        public RelayCommand JoinRecurringMeetingCommand
        {
            get
            {
                return _joinRecurringMeetingCommand;
            }
        }

        private RelayCommand<ScheduledMeetingDislpayData> _showScheduledMeetingDetailCommand;
        public RelayCommand<ScheduledMeetingDislpayData> ShowScheduledMeetingDetailCommand
        {
            get
            {
                return _showScheduledMeetingDetailCommand;
            }
        }

        private RelayCommand<ScheduledMeetingDislpayData> _copyScheduledMeetingDetailCommand;
        public RelayCommand<ScheduledMeetingDislpayData> CopyScheduledMeetingDetailCommand
        {
            get
            {
                return _copyScheduledMeetingDetailCommand;
            }
        }

        private RelayCommand<ScheduledMeetingDislpayData> _deleteScheduledMeetingCommand;
        public RelayCommand<ScheduledMeetingDislpayData> DeleteScheduledMeetingCommand
        {
            get
            {
                return _deleteScheduledMeetingCommand;
            }
        }

        private RelayCommand _deleteRecurringMeetingGroupCommand;
        public RelayCommand DeleteRecurringMeetingGroupCommand
        {
            get
            {
                return _deleteRecurringMeetingGroupCommand;
            }
        }

        private RelayCommand<ScheduledMeetingDislpayData> _popRecurringMeetingGroupWindowCommand;
        public RelayCommand<ScheduledMeetingDislpayData> PopRecurringMeetingGroupWindowCommand
        {
            get => _popRecurringMeetingGroupWindowCommand;
        }

        private RelayCommand<Window> _popEditRecurringMeetingGroupCommand;
        public RelayCommand<Window> PopEditRecurringMeetingGroupCommand
        {
            get
            {
                return _popEditRecurringMeetingGroupCommand;
            }
        }

        private RelayCommand _updateRecurringMeetingGroupCommand;
        public RelayCommand UpdateRecurringMeetingGroupCommand
        {
            get
            {
                return _updateRecurringMeetingGroupCommand;
            }
        }

        private RelayCommand _updateRecurringMeetingSingleCommand;
        public RelayCommand UpdateRecurringMeetingSingleCommand
        {
            get
            {
                return _updateRecurringMeetingSingleCommand;
            }
        }

        private RelayCommand<int> _copyToClipboardCommand;
        public RelayCommand<int> CopyToClipboardCommand
        {
            get => _copyToClipboardCommand;
            set => _copyToClipboardCommand = value;
        }

        private RelayCommand<ScheduledMeetingDislpayData> _scheduleListCopyToClipboardCommand;
        public RelayCommand<ScheduledMeetingDislpayData> ScheduleListCopyToClipboardCommand
        {
            get => _scheduleListCopyToClipboardCommand;
        }

        private RelayCommand<Window> _popAddInviteeWindowCommand;
        public RelayCommand<Window> PopAddInviteeWindowCommand
        {
            get => _popAddInviteeWindowCommand;
        }

        private RelayCommand<Window> _cancelInviteeCommand;
        public RelayCommand<Window> CancelInviteeCommand
        {
            get => _cancelInviteeCommand;
        }

        private RelayCommand<Window> _confirmInviteeCommand;
        public RelayCommand<Window> ConfirmInviteeCommand
        {
            get => _confirmInviteeCommand;
        }

        private ObservableCollection<ScheduleMeetingInviteeItem> _searchUserResult = new ObservableCollection<ScheduleMeetingInviteeItem>();
        public ObservableCollection<ScheduleMeetingInviteeItem> SearchUserResult
        {
            get
            {
                return _searchUserResult;
            }
            set
            {
                _searchUserResult = value;
                RaisePropertyChanged("SearchUserResult");
            }
        }

        private Dictionary<string, ScheduleMeetingInviteeItem> _invitedUsers = new Dictionary<string, ScheduleMeetingInviteeItem>();
        public Dictionary<string, ScheduleMeetingInviteeItem> InvitedUsers
        {
            get => _invitedUsers;
            set
            {
                _invitedUsers = value;
                RaisePropertyChanged("InvitedUsers");
            }
        }

        private RelayCommand<ScheduleMeetingInviteeItem> _selectInviteeCommand;
        public RelayCommand<ScheduleMeetingInviteeItem> SelectInviteeCommand
        {
            get => _selectInviteeCommand;
            set => _selectInviteeCommand = value;
        }

        private RelayCommand<ScheduleMeetingInviteeItem> _removeInviteeCommand;
        public RelayCommand<ScheduleMeetingInviteeItem> RemoveInviteeCommand
        {
            get => _removeInviteeCommand;
            set => _removeInviteeCommand = value;
        }

        private RelayCommand _queryUserNextPageCommand;
        public RelayCommand QueryUserNextPageCommand
        {
            get => _queryUserNextPageCommand;
            set => _queryUserNextPageCommand = value;
        }

        private RelayCommand<ScheduledMeetingDislpayData> _saveScheduleToCalendarCommand;
        public RelayCommand<ScheduledMeetingDislpayData> SaveScheduleToCalendarCommand
        {
            get => _saveScheduleToCalendarCommand;
            set => _saveScheduleToCalendarCommand = value;
        }

        #region MeetingScheduleProperties

        private string _scheduledMeetingNumber = string.Empty;
        public string ScheduledMeetingNumber
        {
            get { return ScheduledMeetingNumber; }
            private set { _scheduledMeetingNumber = value; RaisePropertyChanged("ScheduledMeetingNumber"); }
        }

        public string MeetingPWDInDetailData { get; set; }
        public string CallRateInDetailData { get; set; }

        public int JoinInAdvanceTimeInDetailData { get; set; }

        private string _scheduledMeetingReservationID = string.Empty;
        public string ScheduledMeetingReservationID
        {
            get { return _scheduledMeetingReservationID; }
            private set { _scheduledMeetingReservationID = value; RaisePropertyChanged("ScheduledMeetingReservationID"); }
        }

        private string _scheduleMeetingName = string.Empty;
        public string ScheduleMeetingName
        {
            get { return _scheduleMeetingName; }
            set
            {
                _scheduleMeetingName = value;
                RaisePropertyChanged("ScheduleMeetingName");
            }
        }

        private string _scheduleMeetingDescription = string.Empty;
        public string ScheduleMeetingDescription
        {
            get { return _scheduleMeetingDescription; }
            set { _scheduleMeetingDescription = value; RaisePropertyChanged("ScheduleMeetingDescription"); }
        }

        private RelayCommand _frtcPopupJoinMeetingDialog;
        public RelayCommand FRTCPopupJoinMeetingDialog
        {
            get { return _frtcPopupJoinMeetingDialog; }
            set
            {
                _frtcPopupJoinMeetingDialog = value;
            }
        }

        private bool _scheduleMeetingTimeIllegal = false;
        public bool ScheduleMeetingTimeIllegal
        {
            get
            {
                return _scheduleMeetingTimeIllegal;
            }
            set
            {
                if (_scheduleMeetingTimeIllegal != value)
                {
                    _scheduleMeetingTimeIllegal = value;
                    RaisePropertyChanged("ScheduleMeetingTimeIllegal");
                }
            }
        }

        private List<string> _scheduleMeetingAvailableStartTime;
        public List<string> ScheduleMeetingAvailableStartTime
        {
            get => _scheduleMeetingAvailableStartTime;
            set
            {
                _scheduleMeetingAvailableStartTime = value;
                RaisePropertyChanged("ScheduleMeetingAvailableStartTime");
            }
        }

        private int _scheduleMeetingStartTimeSelectedIndex = 0;
        public int ScheduleMeetingStartTimeSelectedIndex
        {
            get => _scheduleMeetingStartTimeSelectedIndex;
            set
            {
                _scheduleMeetingStartTimeSelectedIndex = value;
                RaisePropertyChanged("ScheduleMeetingStartTimeSelectedIndex");
            }
        }

        private TimeSpan? _meetingStartTime;
        public TimeSpan? MeetingStartTime
        {
            get => _meetingStartTime;
            set
            {
                if (_meetingStartTime != value)
                {
                    _meetingStartTime = value;
                    RaisePropertyChanged("MeetingStartTime");
                }
            }
        }

        private DateTime? _meetingStartDate;
        public DateTime? MeetingStartDate
        {
            get => _meetingStartDate;
            set
            {
                if (_meetingStartDate != value)
                {
                    _meetingStartDate = value;
                    RaisePropertyChanged("MeetingStartDate");
                }
            }
        }

        private TimeSpan? _meetingEndTime;
        public TimeSpan? MeetingEndTime
        {
            get => _meetingEndTime;
            set
            {
                if (_meetingEndTime != value)
                {
                    _meetingEndTime = value;
                    RaisePropertyChanged("MeetingEndTime");
                }
            }
        }

        private DateTime? _meetingEndDate;
        public DateTime? MeetingEndDate
        {
            get => _meetingEndDate;
            set
            {
                if (_meetingEndDate != value)
                {
                    _meetingEndDate = value;
                    RaisePropertyChanged("MeetingEndDate");
                }
            }
        }

        private TimeSpan? _scheduleMeetingStartTime;
        public TimeSpan? ScheduleMeetingStartTime
        {
            get => _scheduleMeetingStartTime;
            set
            {
                //if (_scheduleMeetingStartTime != value)
                {
                    _scheduleMeetingStartTime = value;
                    RaisePropertyChanged("ScheduleMeetingStartTime");
                    RenewScheduleMeetingAvailableEndTime();
                }
            }
        }

        private DateTime? _scheduleMeetingStartDate;
        public DateTime? ScheduleMeetingStartDate
        {
            get => _scheduleMeetingStartDate;
            set
            {
                if (_scheduleMeetingStartDate != value)
                {
                    if (_scheduleMeetingStartDate.HasValue && value.HasValue)
                    {
                        RenewRecurringMeetingDefaultRecurringDayInWeekAndMonth(_scheduleMeetingStartDate.Value, value.Value);
                    }
                    _scheduleMeetingStartDate = value;
                    RaisePropertyChanged("ScheduleMeetingStartDate");
                    RenewScheduleMeetingAvailableStartTime();
                    RenewRecurringMeetingGroupDefaultEndDate();
                }
            }
        }

        private List<string> _scheduleMeetingAvailableEndTime;
        public List<string> ScheduleMeetingAvailableEndTime
        {
            get => _scheduleMeetingAvailableEndTime;
            set
            {
                _scheduleMeetingAvailableEndTime = value;
                RaisePropertyChanged("ScheduleMeetingAvailableEndTime");
            }
        }

        private int _scheduleMeetingEndTimeSelectedIndex = 0;
        public int ScheduleMeetingEndTimeSelectedIndex
        {
            get => _scheduleMeetingEndTimeSelectedIndex;
            set
            {
                _scheduleMeetingEndTimeSelectedIndex = value;
                RaisePropertyChanged("ScheduleMeetingEndTimeSelectedIndex");
            }
        }

        private TimeSpan? _scheduleMeetingEndTime;
        public TimeSpan? ScheduleMeetingEndTime
        {
            get => _scheduleMeetingEndTime;
            set
            {
                _scheduleMeetingEndTime = value;
                RaisePropertyChanged("ScheduleMeetingEndTime");
            }
        }

        private DateTime? _scheduleMeetingEndDateMin;
        public DateTime? ScheduleMeetingEndDateMin
        {
            get => _scheduleMeetingEndDateMin;
            set
            {
                _scheduleMeetingEndDateMin = value;
                RaisePropertyChanged("ScheduleMeetingEndDateMin");
            }
        }

        private DateTime? _scheduleMeetingEndDateMax;
        public DateTime? ScheduleMeetingEndDateMax
        {
            get => _scheduleMeetingEndDateMax;
            set
            {
                _scheduleMeetingEndDateMax = value;
                RaisePropertyChanged("ScheduleMeetingEndDateMax");
            }
        }

        private DateTime? _scheduleMeetingEndDate;
        public DateTime? ScheduleMeetingEndDate
        {
            get => _scheduleMeetingEndDate;
            set
            {
                _scheduleMeetingEndDate = value;
                RaisePropertyChanged("ScheduleMeetingEndDate");
                RenewScheduleMeetingAvailableEndTime();
            }
        }

        private string _currentTimeZone = string.Empty;
        public string CurrentTimeZone
        {
            get => _currentTimeZone;
            set
            {
                _currentTimeZone = value;
                RaisePropertyChanged("CurrentTimeZone");
            }
        }

        private bool _usePersonalMeetingRoomSchedule = false;
        public bool UsePersonalMeetingRoomSchedule
        {
            get => _usePersonalMeetingRoomSchedule;
            set
            {
                if (_usePersonalMeetingRoomSchedule != value)
                {
                    _usePersonalMeetingRoomSchedule = value;
                    RaisePropertyChanged("UsePersonalMeetingRoomSchedule");
                    SelectedRoomSchedule = _usePersonalMeetingRoomSchedule ? RoomList?.First() : null;
                }
            }
        }

        private List<UserMeetingRoomDisplayData> _roomList;
        public List<UserMeetingRoomDisplayData> RoomList
        {
            get { return _roomList; }
            set
            { _roomList = value; RaisePropertyChanged("RoomList"); }
        }

        private UserMeetingRoomDisplayData _selectedRoomSchedule;
        public UserMeetingRoomDisplayData SelectedRoomSchedule
        {
            get { return _selectedRoomSchedule; }
            set { _selectedRoomSchedule = value; RaisePropertyChanged("SelectedRoomSchedule"); }
        }

        private List<ScheduleMeetingInviteeItem> _inviteeListTemp = new List<ScheduleMeetingInviteeItem>();
        public List<ScheduleMeetingInviteeItem> InviteeListTemp
        {
            get => _inviteeListTemp;
            set
            { _inviteeListTemp = value; RaisePropertyChanged("InviteeListTemp"); }
        }

        private List<ScheduleMeetingInviteeItem> _inviteeList = new List<ScheduleMeetingInviteeItem>();
        public List<ScheduleMeetingInviteeItem> InviteeList
        {
            get => _inviteeList;
            set
            { _inviteeList = value; RaisePropertyChanged("InviteeList"); }
        }

        private List<string> _callRateOptions = new List<string>()
        {
            "128K","512K","1024K", "2048K", "2560K", "3072K", "4096K", "6144K", "8192K"
        };
        public List<string> CallRateOptions
        {
            get => _callRateOptions;
        }

        private string _selectedCallRate = "2048K";
        public string SelectedCallRate
        {
            get => _selectedCallRate;
            set
            {
                if (value == null)
                    return;
                _selectedCallRate = value;
                RaisePropertyChanged("SelectedCallRate");
            }
        }

        private List<JoinMeetingInAdvanceTime> _joinInAdvanceTimeOptions = new List<JoinMeetingInAdvanceTime>()
        {
            new JoinMeetingInAdvanceTime(){ InAdvanceTime = 30, Name = Resources.FRTC_SDKAPP_THIRTY_MINUTES},
            new JoinMeetingInAdvanceTime(){ InAdvanceTime = -1, Name = Resources.FRTC_SDKAPP_ANY_TIME}
        };
        public List<JoinMeetingInAdvanceTime> JoinInAdvanceTimeOptions
        {
            get => _joinInAdvanceTimeOptions;
        }

        private JoinMeetingInAdvanceTime _joinMeetingInAdvanceTime;
        public JoinMeetingInAdvanceTime JoinMeetingInAdvanceTime
        {
            get => _joinMeetingInAdvanceTime;
            set
            {
                _joinMeetingInAdvanceTime = value;
                RaisePropertyChanged();
            }
        }

        private bool _muteMicWhenJoin = false;
        public bool MuteMicWhenJoin
        {
            get => _muteMicWhenJoin;
            set
            {
                _muteMicWhenJoin = value;
                RaisePropertyChanged("MuteMicWhenJoin");
            }
        }

        private bool _enableGuestCall = true;
        public bool EnableGuestCall
        {
            get => _enableGuestCall;
            set
            {
                _enableGuestCall = value;
                RaisePropertyChanged("EnableGuestCall");
                if (_enableGuestCall)
                    EnableWaterMark = false;
            }
        }

        private bool _enableWaterMark = false;
        public bool EnableWaterMark
        {
            get => _enableWaterMark;
            set
            {
                _enableWaterMark = value;
                RaisePropertyChanged("EnableWaterMark");
                if (_enableWaterMark)
                    EnableGuestCall = false;
            }
        }

        private bool _enableMeetingPWD = false;
        public bool EnableMeetingPWD
        {
            get => _enableMeetingPWD;
            set
            {
                _enableMeetingPWD = value;
                RaisePropertyChanged("EnableMeetingPWD");
            }
        }

        #endregion

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }



        private bool _processing = false;
        public bool Processing
        {
            get
            {
                return _processing;
            }
            set
            {
                if (_processing != value)
                {
                    _processing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _fetchingMeetings = false;
        public bool FetchingMeetings
        {
            get
            {
                return _fetchingMeetings;
            }
            set
            {
                if (_fetchingMeetings != value)
                {
                    _fetchingMeetings = value;
                    RaisePropertyChanged();
                }
            }
        }

        private List<MeetingRecurringOption> _meetingRecurringOptions = new List<MeetingRecurringOption>()
        {
            new MeetingRecurringOption(){ Recurring = MeetingRecurring.NoRecurring, RecurringName = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_NO},
            new MeetingRecurringOption(){ Recurring = MeetingRecurring.Daily, RecurringName = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY},
            new MeetingRecurringOption(){ Recurring = MeetingRecurring.Weekly, RecurringName = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK},
            new MeetingRecurringOption(){ Recurring = MeetingRecurring.Monthly, RecurringName = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH},
        };
        public List<MeetingRecurringOption> MeetingRecurringOptions { get => _meetingRecurringOptions; }

        private MeetingRecurring _meetingRecurring = MeetingRecurring.NoRecurring;
        public MeetingRecurring MeetingRecurring
        {
            get
            {
                return _meetingRecurring;
            }
            set
            {
                if (_meetingRecurring != value)
                {
                    _meetingRecurring = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("IsRecurringMeeting");
                    CreateRecurringFrequenceOptions(_meetingRecurring);
                    RaisePropertyChanged("RecurringFrequenceOptions");
                    RecurringFrequence = RecurringFrequenceOptions[0].Frequence;
                    if (_meetingRecurring != MeetingRecurring.Weekly)
                    {
                        RecurringWeekDays.Clear();
                        if (ScheduleMeetingStartDate.HasValue)
                        {
                            int dayOfWeek = (int)ScheduleMeetingStartDate.Value.DayOfWeek + 1;
                            RecurringWeekDays.Add(dayOfWeek);
                        }
                        RaisePropertyChanged("RecurringWeekDays");
                    }
                    if (_meetingRecurring != MeetingRecurring.Monthly)
                    {
                        RecurringMonthDays.Clear();
                        if (ScheduleMeetingStartDate.HasValue)
                        {
                            RecurringMonthDays.Add(ScheduleMeetingStartDate.Value.Date.Day);
                        }
                        RaisePropertyChanged("RecurringMonthDays");
                    }
                }
            }
        }

        private bool _isRecurringMeeting = false;
        public bool IsRecurringMeeting
        {
            get => _meetingRecurring != MeetingRecurring.NoRecurring;
        }

        private List<RecurringFrequenceOption> _recurringFrequenceOptions = new List<RecurringFrequenceOption>();
        public List<RecurringFrequenceOption> RecurringFrequenceOptions { get => _recurringFrequenceOptions; }

        private int _recurringFrequence = 1;
        public int RecurringFrequence
        {
            get
            {
                return _recurringFrequence;
            }
            set
            {
                //if (_recurringFrequence != value)
                {
                    _recurringFrequence = value;
                    RaisePropertyChanged();
                    RenewRecurringMeetingGroupDefaultEndDate();
                }
            }
        }

        private string _recurring = string.Empty;
        public string Recurring
        {
            get
            {
                return _recurring;
            }
            set
            {
                if (_recurring != value)
                {
                    _recurring = value;
                    RaisePropertyChanged();
                }
            }
        }

        private List<int> _recurringWeekDays = new List<int>();
        public List<int> RecurringWeekDays
        {
            get => _recurringWeekDays;
            set
            {
                _recurringWeekDays = value;
                RaisePropertyChanged();
            }
        }

        private SQMeetingCommand<bool, string> _setRecurringWeekDayCmd;
        public SQMeetingCommand<bool, string> SetRecurringWeekDayCmd { get => _setRecurringWeekDayCmd; }


        private List<int> _recurringMonthDays = new List<int>();
        public List<int> RecurringMonthDays { get => _recurringMonthDays; }

        private SQMeetingCommand<bool, string> _setRecurringMonthDayCmd;
        public SQMeetingCommand<bool, string> SetRecurringMonthDayCmd { get => _setRecurringMonthDayCmd; }


        private DateTime? _recurringMeetingEndDate;
        public DateTime? RecurringMeetingEndDate
        {
            get => _recurringMeetingEndDate;
            set
            {
                if (_recurringMeetingEndDate != value)
                {
                    _recurringMeetingEndDate = value;
                    RaisePropertyChanged("RecurringMeetingEndDate");
                }
            }
        }

        private DateTime? _recurringMeetingEndDateMax;
        public DateTime? RecurringMeetingEndDateMax
        {
            get => _recurringMeetingEndDateMax;
            set
            {
                _recurringMeetingEndDateMax = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _updateRecurringMeetingSingleFirstStartDate;
        public DateTime? UpdateRecurringMeetingSingleFirstStartDate
        {
            get => _updateRecurringMeetingSingleFirstStartDate;
            set
            {
                if (_updateRecurringMeetingSingleFirstStartDate != value)
                {
                    _updateRecurringMeetingSingleFirstStartDate = value;
                    RaisePropertyChanged("UpdateRecurringMeetingSingleFirstStartDate");
                }
            }
        }

        private DateTime? _updateRecurringMeetingSingleLastStartDate;
        public DateTime? UpdateRecurringMeetingSingleLastStartDate
        {
            get => _updateRecurringMeetingSingleLastStartDate;
            set
            {
                if (_updateRecurringMeetingSingleLastStartDate != value)
                {
                    _updateRecurringMeetingSingleLastStartDate = value;
                    RaisePropertyChanged("UpdateRecurringMeetingSingleLastStartDate");
                }
            }
        }


        private DateTime? _updateRecurringMeetingSingleLastEndDate;
        public DateTime? UpdateRecurringMeetingSingleLastEndDate
        {
            get => _updateRecurringMeetingSingleLastEndDate;
            set
            {
                if (_updateRecurringMeetingSingleLastEndDate != value)
                {
                    _updateRecurringMeetingSingleLastEndDate = value;
                    RaisePropertyChanged("UpdateRecurringMeetingSingleLastEndDate");
                }
            }
        }

        private MeetingScheduleResult[] _recurringMeetingReservedGroup = { };
        public MeetingScheduleResult[] RecurringMeetingReservedGroup
        {
            get => _recurringMeetingReservedGroup;
            set
            {
                if (_recurringMeetingReservedGroup != value)
                {
                    _recurringMeetingReservedGroup = value;
                }
            }
        }

        void CreateRecurringFrequenceOptions(MeetingRecurring Recurring)
        {
            int cnt = 0;
            if (Recurring == MeetingRecurring.NoRecurring)
                return;
            else if (Recurring == MeetingRecurring.Daily)
            {
                cnt = 99;
                this.Recurring = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
            }
            else if (Recurring == MeetingRecurring.Weekly)
            {
                cnt = 12;
                this.Recurring = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
            }
            else if (Recurring == MeetingRecurring.Monthly)
            {
                cnt = 12;
                this.Recurring = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
            }

            _recurringFrequenceOptions = new List<RecurringFrequenceOption>();
            for (int i = 0; i < cnt; ++i)
            {
                _recurringFrequenceOptions.Add(new RecurringFrequenceOption() { Frequence = i + 1, FrequenceName = Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + (i + 1).ToString() });
            }
        }

        void SetUpdateRecurringMeetingSingleAvailableStartDate(ScheduledMeetingDislpayData data)
        {
            if (RecurringMeetingGroup != null)
            {
                DateTime startDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(data.BeginTime)).ToLocalTime().Date;

                _updateRecurringMeetingSingleFirstStartDate = DateTime.Today;
                _updateRecurringMeetingSingleLastStartDate = DateTime.Today;

                int index = RecurringMeetingGroup.IndexOf(data);
                if (index == -1 || index == 0)
                {
                    if (RecurringMeetingGroup.Count > 1)
                    {
                        DateTime recurrenceNextMeetingStartDateTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[1].BeginTime)).ToLocalTime();
                        _updateRecurringMeetingSingleLastStartDate = recurrenceNextMeetingStartDateTime.Date.Subtract(new TimeSpan(1, 0, 0, 0));
                    }
                }
                else if (index > 0)
                {
                    DateTime recurrencePrevMeetingEndDateTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[index - 1].EndTime)).ToLocalTime().Date;
                    _updateRecurringMeetingSingleFirstStartDate = recurrencePrevMeetingEndDateTime.Date.AddDays(1);
                    _updateRecurringMeetingSingleLastStartDate = startDate.AddDays(1);
                    if (index < RecurringMeetingGroup.Count - 1)
                    {
                        DateTime recurrenceNextMeetingStartDateTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(long.Parse(RecurringMeetingGroup[index + 1].BeginTime)).ToLocalTime();
                        _updateRecurringMeetingSingleLastStartDate = recurrenceNextMeetingStartDateTime.Date.Subtract(new TimeSpan(1, 0, 0, 0));
                    }
                    else if (index == RecurringMeetingGroup.Count - 1)
                    {
                        _updateRecurringMeetingSingleLastStartDate = UIHelper.GetUTCDateTimeFromUTCTimestamp(data.RecurringEndDate).ToLocalTime().Date;
                    }
                }
                RaisePropertyChanged("UpdateRecurringMeetingSingleFirstStartDate");
                RaisePropertyChanged("UpdateRecurringMeetingSingleLastStartDate");
            }
        }

        void RenewRecurringMeetingGroupDefaultEndDate()
        {
            if (ScheduleMeetingStartDate.HasValue)
            {
                RecurringMeetingEndDateMax = ScheduleMeetingStartDate.Value.Date.AddDays(364);
                DateTime end = RecurringMeetingEndDateMax.Value.Date;
                if (MeetingRecurring == MeetingRecurring.Daily)
                {
                    end = ScheduleMeetingStartDate.Value.Date.AddDays(6 * RecurringFrequence);

                }
                else if (MeetingRecurring == MeetingRecurring.Weekly)
                {
                    end = ScheduleMeetingStartDate.Value.Date.AddDays(6 * 7 * RecurringFrequence);

                }
                else if (MeetingRecurring == MeetingRecurring.Monthly)
                {
                    end = ScheduleMeetingStartDate.Value.AddMonths(6 * RecurringFrequence);
                }

                if (end.Date.CompareTo(RecurringMeetingEndDateMax.Value) > 0)
                {
                    end = RecurringMeetingEndDateMax.Value.Date;
                }
                RecurringMeetingEndDate = end;
            }
        }

        void RenewRecurringMeetingDefaultRecurringDayInWeekAndMonth(DateTime oldDate, DateTime newDate)
        {
            int dofWOld = (int)oldDate.Date.DayOfWeek + 1;
            if (_recurringWeekDays.Contains(dofWOld))
            {
                _recurringWeekDays.Remove(dofWOld);
            }
            int dofWNew = (int)newDate.DayOfWeek + 1;
            if (!_recurringWeekDays.Contains(dofWNew))
            {
                _recurringWeekDays.Add(dofWNew);
            }
            RaisePropertyChanged("RecurringWeekDays");

            if (_recurringMonthDays.Contains(oldDate.Date.Day))
            {
                _recurringMonthDays.Remove(oldDate.Date.Day);
            }
            if (!_recurringMonthDays.Contains(newDate.Date.Day))
            {
                _recurringMonthDays.Add(newDate.Date.Day);
            }
            RaisePropertyChanged("RecurringMonthDays");
        }

        void ShowRecurringGroupWnd(ScheduledMeetingDislpayData data, bool fetchGroupInfo = true)
        {
            if (data != null && data.IsRecurringMeeting)
            {
                if (recurringMeetingGroupWnd != null && _currentLookupRecurringMeeting != null && _currentLookupRecurringMeeting.RecurrenceId == data.RecurrenceId)
                {
                    recurringMeetingGroupWnd.Show();
                    recurringMeetingGroupWnd.Activate();
                }
                else
                {
                    recurringMeetingGroupWnd?.Close();
                    _currentLookupRecurringMeeting = data;
                    if (fetchGroupInfo)
                    {
                        m_scheduleMgr.GetRecurringMeetingGroup(data.RecurrenceId, 1, 365, string.Empty);
                    }
                    recurringMeetingGroupWnd = new RecurringMeetingSeriesWindow();
                    recurringMeetingGroupWnd.Owner = Application.Current.MainWindow;
                    recurringMeetingGroupWnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    recurringMeetingGroupWnd.Closed += (s, e) =>
                    {
                        recurringMeetingGroupWnd = null;
                        _currentLookupRecurringMeeting = null;
                        App.Current.MainWindow.Show();
                        App.Current.MainWindow.Activate();
                    };
                    recurringMeetingGroupWnd.Show();
                    recurringMeetingGroupWnd.Activate();
                }
            }
        }
    }
}
