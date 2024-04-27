using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQMeeting.MvvMMessages;
using SQMeeting.Model;
using System.Windows.Input;
using SQMeeting.Commands;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using SQMeeting.Model.DataObj;
using SQMeeting.LogTool;
using Newtonsoft.Json.Linq;
using SQMeeting.FRTCView;

namespace SQMeeting.ViewModel
{
    public enum SingInStage
    {
        CheckUserName,
        SetServerAddress,
        AdminUser,
        ChangePassword,
        SignInWithPassword,
    }

    public class UserMeetingRoomDisplayData : ObservableObject
    {
        private string _roomNumber;
        public string RoomNumber
        {
            get { return _roomNumber; }
            set { _roomNumber = value; RaisePropertyChanged("RoomNumber"); }
        }

        private string _roomName;
        public string RoomName
        {
            get { return _roomName; }
            set { _roomName = value; RaisePropertyChanged("RoomName"); }
        }

        private string _roomPWD;
        public string RoomPWD
        {
            get => _roomPWD;
            set { _roomPWD = value; RaisePropertyChanged("RoomPWD"); }
        }

        private string _roomID;
        public string RoomID
        {
            get => _roomID;
            set { _roomID = value; RaisePropertyChanged("RoomID"); }
        }
    }
    public class MeetingHistoryDislpayData : ObservableObject
    {
        private string _meetingNumber;
        public string MeetingNumber
        {
            get { return _meetingNumber; }
            set { _meetingNumber = value; RaisePropertyChanged("MeetingNumber"); }
        }

        private string _meetingName;
        public string MeetingName
        {
            get { return _meetingName; }
            set { _meetingName = value; RaisePropertyChanged("MeetingName"); }
        }

        private string _meetingTime;
        public string MeetingTime
        {
            get { return _meetingTime; }
            set { _meetingTime = value; RaisePropertyChanged("MeetingTime"); }
        }

        private string _beginTime;
        public string BeginTime
        {
            get { return _beginTime; }
            set { _beginTime = value; RaisePropertyChanged("BeginTime"); }
        }

        private string _endTime;
        public string EndTime
        {
            get { return _endTime; }
            set { _endTime = value; RaisePropertyChanged("EndTime"); }
        }

        public string uuid { get; set; }

        private string _meetingPWD;
        public string MeetingPWD
        {
            get => _meetingPWD;
            set { _meetingPWD = value; RaisePropertyChanged("MeetingPWD"); }
        }

        private string _meetingDuration;
        public string MeetingDuration
        {
            get => _meetingDuration;
            set { _meetingDuration = value; RaisePropertyChanged("MeetingDuration"); }
        }

        private string _meetingOwnerId;
        public string MeetingOwnerId
        {
            get => _meetingOwnerId;
            set { _meetingOwnerId = value; RaisePropertyChanged("MeetingOwnerId"); }
        }

        private string _meetingHostEmail;
        public string MeetingHostEmail
        {
            get => _meetingHostEmail;
            set { _meetingHostEmail = value; RaisePropertyChanged("MeetingHostEmail"); }
        }

        private string _meetingOwnerName;
        public string MeetingOwnerName
        {
            get => _meetingOwnerName;
            set { _meetingOwnerName = value; RaisePropertyChanged("MeetingOwnerName"); }
        }

        public bool IsEmpty()
        {
            return MeetingName == string.Empty &&
                MeetingNumber == string.Empty &&
                MeetingPWD == string.Empty &&
                BeginTime == string.Empty &&
                EndTime == string.Empty;
        }
    }

    public class ScheduledMeetingDislpayData : ObservableObject, ICloneable
    {
        private string _meetingType;
        public string MeetingType
        {
            get { return _meetingType; }
            set { _meetingType = value; RaisePropertyChanged("MeetingType"); }
        }

        private string _meetingNumber;
        public string MeetingNumber
        {
            get { return _meetingNumber; }
            set { _meetingNumber = value; RaisePropertyChanged("MeetingNumber"); }
        }

        private string _meetingRoomId;
        public string MeetingRoomId
        {
            get { return _meetingRoomId; }
            set { _meetingRoomId = value; RaisePropertyChanged(); }
        }

        private string _meetingName;
        public string MeetingName
        {
            get { return _meetingName; }
            set { _meetingName = value; RaisePropertyChanged("MeetingName"); }
        }

        private string _meetingTime;
        public string MeetingTime
        {
            get { return _meetingTime; }
            set { _meetingTime = value; RaisePropertyChanged("MeetingTime"); }
        }

        private string _beginTime;
        public string BeginTime
        {
            get { return _beginTime; }
            set { _beginTime = value; RaisePropertyChanged("BeginTime"); }
        }

        private string _endTime;
        public string EndTime
        {
            get { return _endTime; }
            set { _endTime = value; RaisePropertyChanged("EndTime"); }
        }

        private string _callRate;
        public string CallRate
        {
            get { return _callRate; }
            set { _callRate = value; RaisePropertyChanged(); }
        }

        private bool _enableGuestCall = false;
        public bool EnableGuestCall
        {
            get { return _enableGuestCall; }
            set
            {
                _enableGuestCall = value; RaisePropertyChanged();
            }
        }

        private bool _enableWaterMark = false;
        public bool EnableWaterMark
        {
            get { return _enableWaterMark; }
            set
            {
                _enableWaterMark = value; RaisePropertyChanged();
            }
        }

        private bool _enableMeetingPWD = false;
        public bool EnableMeetingPWD
        {
            get { return _enableMeetingPWD; }
            set
            {
                _enableMeetingPWD = value; RaisePropertyChanged();
            }
        }

        private bool _isInvited = false;
        public bool IsInvited
        {
            get { return _isInvited; }
            set
            {
                _isInvited = value; RaisePropertyChanged("IsInvited");
            }
        }

        private bool _isAboutToBegin = false;
        public bool IsAboutToBegin
        {
            get { return _isAboutToBegin; }
            set
            {
                _isAboutToBegin = value; RaisePropertyChanged("IsAboutToBegin");
            }
        }

        private bool _isInProgress = false;
        public bool IsInProgress
        {
            get { return _isInProgress; }
            set
            {
                _isInProgress = value; RaisePropertyChanged("IsInProgress");
            }
        }

        private bool _isAvailable = false;
        public bool IsAvailable
        {
            //get { return _isAvailable; }
            get { return true; }
            set
            {
                _isAvailable = value; RaisePropertyChanged("IsAvailable");
            }
        }

        private bool _authorized = false;
        public bool Authorized
        {
            get { return _authorized; }
            set
            {
                _authorized = value; RaisePropertyChanged("Authorized");
            }
        }

        private bool _isManuallyAdded = false;
        public bool IsManuallyAdded
        {
            get { return _isManuallyAdded; }
            set
            {
                _isManuallyAdded = value; RaisePropertyChanged("IsManuallyAdded");
            }
        }

        public string BeginTimeStr { get; set; }
        public string EndTimeStr { get; set; }
        public string Duration { get; set; }
        public string MeetingDayOfWeek { get; set; }

        private string _meetingDescription = string.Empty;
        public string MeetingDescription
        {
            get => _meetingDescription;
            set
            {
                _meetingDescription = value; RaisePropertyChanged("MeetingDescription");
            }
        }
        public string OwnerId { get; set; }

        public string OwnerName { get; set; }
        public string Password { get; set; }
        public string ReserveId { get; set; }

        public string MeetingUrl { get; set; }

        public string GroupMeetingUrl { get; set; }

        public string RecurrenceId { get; set; }

        private bool _isRecurringMeeting = false;
        public bool IsRecurringMeeting
        {
            //get { return _isAvailable; }
            get { return _isRecurringMeeting; }
            set
            {
                _isRecurringMeeting = value; RaisePropertyChanged("IsRecurringMeeting");
            }
        }

        private long _recurringBeginDate;
        public long RecurringBeginDate
        {
            get { return _recurringBeginDate; }
            set { _recurringBeginDate = value; RaisePropertyChanged("RecurringBeginDate"); }
        }

        private long _recurringEndDate;
        public long RecurringEndDate
        {
            get { return _recurringEndDate; }
            set { _recurringEndDate = value; RaisePropertyChanged("RecurringEndDate"); }
        }

        private MeetingRecurring _recurringType = MeetingRecurring.NoRecurring;
        public MeetingRecurring RecurringType
        {
            get => _recurringType;
            set { _recurringType= value; RaisePropertyChanged(); }
        }

        private int _recurringFrequency = 0;
        public int RecurringFrequency
        {
            get => _recurringFrequency;
            set { _recurringFrequency = value; RaisePropertyChanged(); }
        }

        private int[] _recurringDaysOfWeek = { };
        public int[] RecurringDaysOfWeek
        {
            get => _recurringDaysOfWeek;
            set { _recurringDaysOfWeek = value; RaisePropertyChanged(); }
        }

        private int[] _recurringDaysOfMonth = { };
        public int[] RecurringDaysOfMonth
        {
            get => _recurringDaysOfMonth;
            set { _recurringDaysOfMonth = value; RaisePropertyChanged(); }
        }

        private MeetingScheduleResult[] _recurringMeetingGroup = { };
        public MeetingScheduleResult[] RecurringMeetingGroup
        {
            get => _recurringMeetingGroup;
            set { _recurringMeetingGroup = value; RaisePropertyChanged(); }
        }

        public List<string> ParticipantUsers { get; set; }

        public string MeetingInfoKey { get; set; }
        public string GroupInfoKey { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(MeetingNumber);
        }

        public object Clone()
        {
            ScheduledMeetingDislpayData clone = new ScheduledMeetingDislpayData();
            clone.MeetingType = MeetingType;
            clone.MeetingNumber = MeetingNumber;
            clone.MeetingTime = MeetingTime;
            clone.MeetingName = MeetingName;
            clone.IsAboutToBegin = IsAboutToBegin;
            clone.IsAvailable = IsAvailable;
            clone.IsInProgress = IsInProgress;
            clone.IsRecurringMeeting = IsRecurringMeeting;
            clone.IsInvited = IsInvited;
            clone.MeetingDescription = MeetingDescription;
            clone.OwnerId = OwnerId;
            clone.OwnerName = OwnerName;
            clone.Password = Password;
            clone.ReserveId = ReserveId;
            clone.MeetingUrl = MeetingUrl;
            clone.GroupMeetingUrl = GroupMeetingUrl;
            clone.EndTime = EndTime;
            clone.EndTimeStr = EndTimeStr;
            clone.Duration = Duration;
            clone.BeginTime = BeginTime;
            clone.BeginTimeStr = BeginTimeStr;
            clone.RecurringBeginDate = RecurringBeginDate;
            clone.RecurringEndDate = RecurringEndDate;
            clone.RecurringType = RecurringType;
            clone.RecurringFrequency = RecurringFrequency;
            clone.RecurringDaysOfWeek = (int[])RecurringDaysOfWeek?.Clone();
            clone.RecurringDaysOfMonth = (int[])RecurringDaysOfMonth?.Clone();
            clone.Authorized = Authorized;
            clone.IsManuallyAdded = IsManuallyAdded;
            clone.ParticipantUsers = ParticipantUsers;
            clone.MeetingInfoKey = MeetingInfoKey;
            clone.GroupInfoKey = GroupInfoKey;
            return clone;
        }

        public ScheduledMeetingDislpayData UseDetailMeetingTime()
        {
            long startTimestamp = long.Parse(BeginTime);
            DateTime beginTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(startTimestamp);

            long endTimestamp = long.Parse(EndTime);
            DateTime endTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTimestamp);

            try
            {
                string displayBeginTime = string.Empty, displayEndTime = string.Empty, displayMeetingTime = string.Empty;
                displayBeginTime = beginTime.ToLocalTime().ToString("MM-dd HH:mm");

                if (endTime.Date == DateTime.UtcNow.Date)
                {
                    if (endTime.Date == beginTime.Date)
                        displayEndTime = endTime.ToLocalTime().ToString("HH:mm");
                    else
                        displayEndTime = endTime.ToLocalTime().ToString("MM-dd HH:mm");
                }
                else if (endTime.Date.Year == DateTime.UtcNow.Year)
                {
                    displayEndTime = endTime.ToLocalTime().ToString("MM-dd HH:mm");
                }
                else
                {
                    displayEndTime = endTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }

                displayMeetingTime = displayBeginTime + " - " + displayEndTime;
                MeetingTime = displayMeetingTime;
            }
            catch (Exception ex) { throw ex; }
            return this;
        }
    }

    public class ScheduleMeetingInviteeItem : ObservableObject
    {
        private bool _isInvited = false;
        public bool IsInvited
        {
            get => _isInvited;
            set
            {
                _isInvited = value;
                RaisePropertyChanged("IsInvited");
            }
        }

        public string WholeName
        {
            get
            {
                return string.Format("{0} ({1})", Info.real_name, Info.username);
            }
        }


        public UserInfo Info { get; set; }
    }

    public class JoinMeetingInAdvanceTime : ObservableObject
    {
        private int _inAdvanceTime = -1;
        public int InAdvanceTime
        {
            get => _inAdvanceTime;
            set
            {
                _inAdvanceTime = value;
                RaisePropertyChanged("InAdvanceTime");
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }
    }

    public class FRTCUserViewModel : FRTCViewModelBase
    {
        FRTCUserManager m_signInMgr;
        MeetingScheduleManager m_scheduleMgr;
        MeetingHistoryManager m_historyMgr;

        string _newPwd = string.Empty;

        public ScheduleMeetingViewModel ScheduleMeeting { get; set; }

        public FRTCUserViewModel()
        {
            //DateTime t0 = DateTime.Now;
            m_signInMgr = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<FRTCUserManager>();
            MessengerInstance.Register<FRTCCallStateChangeMessage>(this, OnCallStateChanged);
            MessengerInstance.Register<FRTCAPIResultMessage>(this, OnUserSingInResult);
            MessengerInstance.Register<NotificationMessage>(this, OnNotificationMsg);
            MessengerInstance.Register<OnFRTCViewShownMessage>(this, OnShow);
            MessengerInstance.Register<FRTCMeetingScheduledMessage>(this, (msg) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Processing = false;
                });
            });

            MessengerInstance.Register<MeetingScheduleAPIErrorMessage>(this, (msg) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Processing = false;
                });
            });


            ScheduleMeeting = new ScheduleMeetingViewModel();

            m_scheduleMgr = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<MeetingScheduleManager>();
            m_historyMgr = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<MeetingHistoryManager>();

            _cancelSignInCommand = new RelayCommand(() =>
            {
                MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                { SourceView = FrtcMeetingViews.SignInView, TargetView = FrtcMeetingViews.LaunchView });
            });

            _signOutCommand = new RelayCommand(() =>
            {
                m_signInMgr.SignOut();
            });

            _frtcSignInCommand = new RelayCommand<PasswordBox>((p) =>
            {
                if (string.IsNullOrEmpty(p.Password))
                {
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                    { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_EMPTY });
                    return;
                }
                if (string.IsNullOrEmpty(UserName))
                {
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                    {
                        TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_USERNAME_EMPTY
                    });
                    return;
                }

                m_signInMgr.SignIn(_userName, p.Password);
            });

            _frtcSetupMeetingCommand = new RelayCommand(() =>
            {
                Processing = true;
                if (UsePersonalMeetingRoom)
                {
                    JoinMeetingFromHistoryOrScheduleList msg = new JoinMeetingFromHistoryOrScheduleList(new MeetingScheduleResult()
                    {
                        meeting_number = SelectedRoom.RoomNumber,
                        meeting_password = SelectedRoom.RoomPWD,
                        owner_id = m_signInMgr.UserData.user_id,
                    });
                    MessengerInstance.Send<JoinMeetingFromHistoryOrScheduleList>(msg);
                    Processing = false;
                }
                else
                {
                    string meetingNameTail = "的会议";
                    if (Properties.Resources.Culture.Name.ToLower() == "en-us")
                    {
                        meetingNameTail = "'s Meeting";
                    }
                    else if (Properties.Resources.Culture.Name.ToLower() == "zh-cht")
                    {
                        meetingNameTail = "的會議";
                    }
                    m_scheduleMgr.SetupMeeting(this.UserName + meetingNameTail);
                }
            });

            _frtcPopupJoinMeetingDialog = new RelayCommand(() =>
            {
                string servAddr = ConfigurationManager.AppSettings["FRTCServerAddress"];
                bool NoServerAddress = string.IsNullOrEmpty(servAddr);
                if (!NoServerAddress)
                {
                    ShowMask = true;
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCJoinMeeting, null);
                    ShowMask = false;
                }
            });

            _frtcPopupScheduleMeetingDialog = new RelayCommand(() =>
              {
                  ScheduleMeeting.PrepareForNewSchedule();
                  FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCScheduleMeeting, null);
                  ScheduleMeeting.ClearInviteeTempData();
              });

            _frtcPopupDisplayNameDialog = new RelayCommand(() =>
            {
                FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCChangeDisplayName, null);
            });

            _frtcChangeDisplayNameCommand = new RelayCommand<string>((name) =>
            {
                this.DisplayName = name;
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
            });

            _frtcPopupChangePWDDialog = new RelayCommand(() =>
            {
                bool? ret = FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCChangePassword, null);
                if (ret.HasValue && ret.Value == false)
                {
                    MessengerInstance.Send<NotificationMessage>(new NotificationMessage("CleanupChangePassword"));
                }
            });

            _frtcShowRecordingCommand = new RelayCommand(async () =>
            {
                await Task.Run(() => System.Diagnostics.Process.Start("https://" + CommonServiceLocator.ServiceLocator.Current.GetInstance<SettingViewModel>().ServerAddress));
            });

            _joinFromHistoryCommand = new RelayCommand<int>((i) =>
            {
                ShowMask = true;
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                JoinMeetingFromHistoryOrScheduleList msg = new JoinMeetingFromHistoryOrScheduleList(new MeetingScheduleResult()
                {
                    meeting_number = MeetingHistoryList[i].MeetingNumber,
                    meeting_password = MeetingHistoryList[i].MeetingPWD,
                    owner_id = MeetingHistoryList[i].MeetingOwnerId,
                    schedule_start_time = MeetingHistoryList[i].BeginTime
                });
                MessengerInstance.Send<JoinMeetingFromHistoryOrScheduleList>(msg);
            });

            _removeHistoryCommand = new RelayCommand<int>((i) =>
            {
                if (FRTCView.FRTCMessageBox.ShowConfirmMessage(Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_SCHEDULE, Properties.Resources.FRTC_MEETING_DELETE_MEETING_MSG))
                {
                    m_historyMgr.DeleteHisotryRecordByID(MeetingHistoryList[i].uuid);
                    MeetingHistoryList.RemoveAt(i);
                }
            });

            _removeAllHistoryCommand = new RelayCommand(() =>
            {
                if (FRTCView.FRTCMessageBox.ShowConfirmMessage(Properties.Resources.FRTC_MEETING_SDKAPP_CLEAR_HISTORY, Properties.Resources.FRTC_MEETING_CLEAR_HISTORY_MSG))
                {
                    m_historyMgr.DeleteAllHisotry();
                    MeetingHistoryList.Clear();
                }
            });

            _copyToClipboardCommand = new RelayCommand<int>((i) =>
            {
                try
                {
                    Clipboard.SetText(MeetingHistoryList[i].MeetingNumber);
                }
                catch (Exception ex)
                {
                    LogTool.LogHelper.Exception(ex);
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_FAILED });
                    return;
                }
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE });
            });

            _showHistoryDetailCommand = new RelayCommand<MeetingHistoryDislpayData>((p) =>
            {
                LogTool.LogHelper.DebugMethodEnter();
                LogTool.LogHelper.Debug(Environment.StackTrace);
                LogTool.LogHelper.Debug("Selected history is {0}", p == null ? "null" : "not null");
                if (p != null)
                {
                    LogTool.LogHelper.Debug("Selected history is {0}", p.IsEmpty() ? "empty" : "not empty");
                    if (!p.IsEmpty())
                    {
                        FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCMeetingHistoryDetail, null);
                    }
                }
                LogTool.LogHelper.Debug("Set selected history to null manually");
                CurrentSelectedHistoryRecord = null;
                LogTool.LogHelper.DebugMethodExit();
            });

            _joinFromHistoryDetailCommand = new RelayCommand(() =>
            {
                if (CurrentSelectedHistoryRecord != null)
                {
                    ShowMask = true;
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                    JoinMeetingFromHistoryOrScheduleList msg = new JoinMeetingFromHistoryOrScheduleList(new MeetingScheduleResult()
                    {
                        meeting_number = CurrentSelectedHistoryRecord.MeetingNumber,
                        meeting_password = CurrentSelectedHistoryRecord.MeetingPWD,
                        owner_id = CurrentSelectedHistoryRecord.MeetingOwnerId,
                        schedule_start_time = CurrentSelectedHistoryRecord.BeginTime
                    });
                    MessengerInstance.Send<JoinMeetingFromHistoryOrScheduleList>(msg);
                    CurrentSelectedHistoryRecord = null;
                }
                else
                {
                    LogTool.LogHelper.Error("Trying to join null history detail data");
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                }
            });

            _removeFromHistoryDetailCommand = new RelayCommand(() =>
            {
                m_historyMgr.DeleteHisotryRecordByID(CurrentSelectedHistoryRecord.uuid);
                MeetingHistoryList.Remove(CurrentSelectedHistoryRecord);
                FRTCView.FRTCPopupViewManager.CurrentPopup.Close();
                CurrentSelectedHistoryRecord = null;
            });

            this._serverAddress = ConfigurationManager.AppSettings["FRTCServerAddress"];

            CheckSignInToken();
        }

        private void OnShow(OnFRTCViewShownMessage msg)
        {
            if (msg.View == FrtcMeetingViews.AccountView)
            {
                RaisePropertyChanged("Status");
            }
            else if (msg.View == FrtcMeetingViews.FRTCMainWindow)
            {
                if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["FRTCServerAddress"]))
                {
                    //this.CurrentStage = SingInStage.SetServerAddress;
                }
                else
                {
                    this._serverAddress = ConfigurationManager.AppSettings["FRTCServerAddress"];
                }
            }
        }

        private void OnNotificationMsg(NotificationMessage msg)
        {
            if (msg.Notification == "SignInViewBackward")
            {
                //switch (CurrentStage)
                {
                    //case SingInStage.SetServerAddress:
                    //case SingInStage.CheckUserName:
                    MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                    { SourceView = FrtcMeetingViews.SignInView, TargetView = FrtcMeetingViews.LaunchView });
                    //break;
                    //case SingInStage.ChangePassword:
                    //case SingInStage.SignInWithPassword:
                    //    CurrentStage = SingInStage.CheckUserName;
                    //    break;
                }
            }
            else if (msg.Notification == "HistoryUpdated")
            {
                GetUserMeetingHistory();
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
                        this.Status = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_ONLINE;
                        if (m_signInMgr.UserData != null)
                        {
                            this.FirstName = m_signInMgr.UserData.firstname;
                            this.LastName = m_signInMgr.UserData.lastname;
                            this.RealName = m_signInMgr.UserData.real_name;
                            this.Email = m_signInMgr.UserData.email;
                            this.Department = m_signInMgr.UserData.department;
                            this.Mobile = m_signInMgr.UserData.mobile;
                            this.UserName = m_signInMgr.UserData.username;

                            string resourceCultureName = UIHelper.GetResourceCultureName();
                            if (string.IsNullOrEmpty(m_signInMgr.UserData.real_name))
                            {

                                if ("zh-CHS" == resourceCultureName || "zh-CHT" == resourceCultureName)
                                    this.DisplayName = m_signInMgr.UserData.lastname + m_signInMgr.UserData.firstname;
                                else
                                    this.DisplayName = m_signInMgr.UserData.firstname + " " + m_signInMgr.UserData.lastname;
                            }
                            else
                            {
                                this.DisplayName = m_signInMgr.UserData.real_name;
                            }

                            ScheduleMeeting.DisplayName = this.DisplayName;

                            if (m_signInMgr.Role == UserRole.MeetingOperator || m_signInMgr.Role == UserRole.SystemAdmin)
                            {
                                IsOperatorRole = true;
                            }
                            else
                                IsOperatorRole = false;
                        }

                        if (IsOperatorRole)
                            ScheduleMeeting.SetScheduleMeetingCallRateOptions(new List<string>() { "128K", "512K", "1024K", "2048K", "2560K", "3072K", "4096K", "6144K", "8192K" });
                        else
                            ScheduleMeeting.SetScheduleMeetingCallRateOptions(new List<string>() { "128K", "512K", "1024K", "2048K", "2560K", "3072K", "4096K" });

                        GetUserMeetingHistory();
                        MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                        { SourceView = FrtcMeetingViews.FRTCSignInView, TargetView = FrtcMeetingViews.FRTCMainWindow, Param = msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN ? ((App)Application.Current).StartupArgSchemaString : null });
                        m_signInMgr.GetUserMeetingRoom();
                        break;
                    case FRTC_API_RESULT.USER_ROOM_SUCCESS:
                        if (m_signInMgr.UserMeetingRoomList != null && m_signInMgr.UserMeetingRoomList.meeting_rooms != null)
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
                                SelectedRoom = RoomList[0];
                                //SelectedRoomSchedule = RoomList[0];
                            }
                        }
                        break;
                    case FRTC_API_RESULT.USER_NOT_EXIST:
                        MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                        {
                            TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_USER_NOT_EXIST
                        });
                        break;
                    case FRTC_API_RESULT.PASSWORD_RESET_SUCCESS:
                        FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_PWD_CHANGED, Properties.Resources.FRTC_MEETING_PWD_CHANGED_MSG);
                        m_signInMgr.SignOut();
                        break;
                    case FRTC_API_RESULT.PASSWORD_RESET_FAILED:
                        MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                        {
                            TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_RESETPWD_FAILED
                        });
                        break;
                    case FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD:
                    case FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR:
                        if (App.Current.MainWindow is FRTCView.LaunchingWindow)
                        {
                            MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                            {
                                SourceView = FrtcMeetingViews.FRTCLaunchingWindow,
                                TargetView = FrtcMeetingViews.FRTCGuestWindow,
                                Param = ((App)Application.Current).StartupArgSchemaString
                            });
                        }
                        MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                        {
                            TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_INVALID_PWD
                        });
                        this.Status = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_OFFLINE;
                        break;
                    case FRTC_API_RESULT.SIGNIN_FAILED_PWD_FREEZED:
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_LOGIN_FAILED, Properties.Resources.FRTC_MEETING_LOGIN_FAILED_PWD_ERROR_MAX);
                        this.Status = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_OFFLINE;
                        break;
                    case FRTC_API_RESULT.SIGNIN_FAILED_PWD_LOCKED:
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_LOGIN_FAILED, Properties.Resources.FRTC_MEETING_LOGIN_FAILED_LOCKED);
                        this.Status = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_OFFLINE;
                        break;
                    case FRTC_API_RESULT.SIGNIN_FAILED_INVALID_TOKEN:
                        MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                        {
                            SourceView = FrtcMeetingViews.FRTCLaunchingWindow,
                            TargetView = FrtcMeetingViews.FRTCGuestWindow,
                            Param = ((App)Application.Current).StartupArgSchemaString
                        });
                        break;
                    case FRTC_API_RESULT.CONNECTION_FAILED:
                        //if (msg.Result == SIGNIN_RESULT.CONNECTION_FAILED )
                        {
                            MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                            {
                                TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_CONNECTION_ERROR
                            });
                        }
                        MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                        {
                            SourceView = FrtcMeetingViews.FRTCLaunchingWindow,
                            TargetView = FrtcMeetingViews.FRTCGuestWindow,
                            Param = ((App)Application.Current).StartupArgSchemaString
                        });

                        Processing = false;
                        break;
                    case FRTC_API_RESULT.SIGNOUT_SUCCESS:
                        this.Status = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_OFFLINE;
                        this.FirstName = string.Empty;
                        this.LastName = string.Empty;
                        this.RealName = string.Empty;
                        this.Email = string.Empty;
                        this.Department = string.Empty;
                        this.Mobile = string.Empty;
                        this.DisplayName = string.Empty;
                        this.UserName = string.Empty;
                        this.UsePersonalMeetingRoom = false;
                        this.MeetingHistoryList.Clear();
                        Processing = false;
                        MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                        {
                            SourceView = FrtcMeetingViews.FRTCMainWindow,
                            TargetView = FrtcMeetingViews.FRTCGuestWindow
                        });
                        break;
                    case FRTC_API_RESULT.API_SESSION_INVALID:
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED, Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED_TIP);
                            }
                            catch (Exception ex) { LogHelper.Exception(ex); }
                        });
                        m_signInMgr.SignOut();
                        break;
                    default:
                        break;
                }
                Processing = false;
            }));
        }

        private void OnCallStateChanged(FRTCCallStateChangeMessage msg)
        {
            if (msg.callState == FrtcCallState.CONNECTED || msg.callState == FrtcCallState.DISCONNECTED)
            {
                ShowMask = false;
                if (msg.callState == FrtcCallState.CONNECTED)
                {
                    this.UsePersonalMeetingRoom = false;
                    this.MuteMicWhenJoin = false;
                }
            }
        }

        private string GetDisplayMeetingTimeString(DateTime StartTime, DateTime EndTime, ref string DisplayStartTime, ref string DisplayEndTime)
        {
            string displayBeginTime = string.Empty, displayEndTime = string.Empty, displayMeetingTime = string.Empty;
            try
            {
                if (StartTime.Date == DateTime.Now.Date)
                {
                    displayBeginTime = Properties.Resources.FRTC_MEETING_SDKAPP_TODAY + " " + StartTime.ToString("HH:mm");
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
                        displayEndTime = Properties.Resources.FRTC_MEETING_SDKAPP_TODAY + " " + EndTime.ToString("HH:mm");
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

        private void CheckSignInToken()
        {
            string token = ConfigurationManager.AppSettings["FRTCUserToken"];
            string autoSignIn = ConfigurationManager.AppSettings["AutoSignIn"];
            if(!string.IsNullOrEmpty(autoSignIn))
            {
                AutoSignIn = bool.Parse(autoSignIn.ToLower());
            }
            string serverAddr = ConfigurationManager.AppSettings["FRTCServerAddress"];

            if (AutoSignIn && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(serverAddr))
            {
                m_signInMgr.SignInViaToken();
            }
            else
            {
                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(1000);
                    MessengerInstance.Send<FRTCViewNavigatorMessage>(new FRTCViewNavigatorMessage()
                    { SourceView = FrtcMeetingViews.FRTCLaunchingWindow, TargetView = FrtcMeetingViews.FRTCGuestWindow, Param = ((App)Application.Current).StartupArgSchemaString });
                });
            }
        }

        private void GetUserMeetingHistory()
        {
            MeetingHistoryList = new ObservableCollection<MeetingHistoryDislpayData>();
            foreach (var item in m_historyMgr.GetMeetingHistoryByUserId(this.Email))
            {
                string displayBeginTime = string.Empty;
                string displayEndTime = string.Empty;
                string displayMeetingTime = string.Empty;
                string duration = string.Empty;
                try
                {
                    DateTime beginTime = DateTime.Parse(item.begin_time);
                    DateTime endTime = DateTime.Parse(item.end_time);
                    displayMeetingTime = GetDisplayMeetingTimeString(beginTime, endTime, ref displayBeginTime, ref displayEndTime);
                    duration = endTime.Subtract(beginTime).ToString();
                }
                catch { }
                MeetingHistoryList.Add(new MeetingHistoryDislpayData()
                {
                    MeetingNumber = item.meeting_number,
                    MeetingName = item.meeting_name,
                    BeginTime = displayBeginTime,
                    EndTime = displayEndTime,
                    MeetingTime = displayMeetingTime,
                    uuid = item.uuid,
                    MeetingPWD = item.meeting_pwd,//string.IsNullOrEmpty(item.owner_id) ? string.Empty : item.meeting_pwd,
                    MeetingDuration = duration,
                    MeetingOwnerId = item.owner_id,
                    MeetingHostEmail = string.IsNullOrEmpty(item.owner_id) ? string.Empty : m_signInMgr.UserData.email,
                    MeetingOwnerName = item.owner_name,
                });
            }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _autoSignIn = false;
        public bool AutoSignIn
        {
            get
            {
                return _autoSignIn;
            }
            set
            {
                if (_autoSignIn != value)
                {
                    _autoSignIn = value;
                    RaisePropertyChanged();
                    try
                    {
                        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        if (config.AppSettings.Settings["AutoSignIn"] != null)
                        {
                            config.AppSettings.Settings["AutoSignIn"].Value = AutoSignIn.ToString();
                        }
                        else
                        {
                            config.AppSettings.Settings.Add("AutoSignIn", AutoSignIn.ToString());
                        }
                        config.Save(ConfigurationSaveMode.Modified, true);
                        ConfigurationManager.RefreshSection("appSettings");
                        config = null;
                    }
                    catch (ConfigurationErrorsException e)
                    {
                        LogHelper.Exception(e);
                    }
                }
            }
        }

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _serverAddress = string.Empty;
        public string ServerAddress
        {
            get
            {
                return _serverAddress;
            }
            set
            {
                if (_serverAddress != value)
                {
                    _serverAddress = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _changeOldPWD = false;
        public bool ChangeOldPWD
        {
            get
            {
                return _changeOldPWD;
            }
            set
            {
                if (_changeOldPWD != value)
                {
                    _changeOldPWD = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _status = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_OFFLINE;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _isOperatorRole = false;
        public bool IsOperatorRole
        {
            get { return _isOperatorRole; }
            set { _isOperatorRole = value; RaisePropertyChanged("IsOperatorRole"); }
        }

        private bool _usePersonalMeetingRoom = false;
        public bool UsePersonalMeetingRoom
        {
            get { return _usePersonalMeetingRoom; }
            set { _usePersonalMeetingRoom = value; RaisePropertyChanged("UsePersonalMeetingRoom"); }
        }

        private UserMeetingRoomDisplayData _selectedRoom;
        public UserMeetingRoomDisplayData SelectedRoom
        {
            get { return _selectedRoom; }
            set { _selectedRoom = value; RaisePropertyChanged("SelectedRoom"); }
        }

        private List<UserMeetingRoomDisplayData> _roomList;
        public List<UserMeetingRoomDisplayData> RoomList
        {
            get { return _roomList; }
            set
            { _roomList = value; RaisePropertyChanged("RoomList"); }
        }

        private bool _muteMicWhenJoin = true;
        public bool MuteMicWhenJoin
        {
            get => _muteMicWhenJoin;
            set { _muteMicWhenJoin = value; RaisePropertyChanged(nameof(MuteMicWhenJoin)); }
        }

        private ObservableCollection<MeetingHistoryDislpayData> _meetingHistoryList;
        public ObservableCollection<MeetingHistoryDislpayData> MeetingHistoryList
        {
            get { return _meetingHistoryList; }
            set { _meetingHistoryList = value; RaisePropertyChanged("MeetingHistoryList"); }
        }

        private RelayCommand<int> _joinFromHistoryCommand;
        public RelayCommand<int> JoinFromHistoryCommand
        {
            get
            {
                return _joinFromHistoryCommand;
            }
        }

        private RelayCommand<int> _removeHistoryCommand;
        public RelayCommand<int> RemoveHistoryCommand
        {
            get
            {
                return _removeHistoryCommand;
            }
        }

        private RelayCommand _removeAllHistoryCommand;
        public RelayCommand RemoveAllHistoryCommand
        {
            get
            {
                return _removeAllHistoryCommand;
            }
        }

        private MeetingHistoryDislpayData _currentSelectedHistoryRecord;
        public MeetingHistoryDislpayData CurrentSelectedHistoryRecord
        {
            get => _currentSelectedHistoryRecord;
            set { _currentSelectedHistoryRecord = value; RaisePropertyChanged("CurrentSelectedHistoryRecord"); }
        }

        private RelayCommand<MeetingHistoryDislpayData> _showHistoryDetailCommand;
        public RelayCommand<MeetingHistoryDislpayData> ShowHistoryDetailCommand
        {
            get
            {
                return _showHistoryDetailCommand;
            }
        }

        private RelayCommand _joinFromHistoryDetailCommand;
        public RelayCommand JoinFromHistoryDetailCommand
        {
            get
            {
                return _joinFromHistoryDetailCommand;
            }
        }

        private RelayCommand _removeFromHistoryDetailCommand;
        public RelayCommand RemoveFromHistoryDetailCommand
        {
            get
            {
                return _removeFromHistoryDetailCommand;
            }
        }

        private RelayCommand _cancelSignInCommand;
        public RelayCommand CancelSignInCommand
        {
            get
            {
                return _cancelSignInCommand;
            }
        }

        private RelayCommand<PasswordBox> _frtcSignInCommand;
        public RelayCommand<PasswordBox> FRTCSignInCommand
        {
            get { return _frtcSignInCommand; }
            set
            {
                _frtcSignInCommand = value;
            }
        }

        private RelayCommand _frtcSetupMeetingCommand;
        public RelayCommand FRTCSetupMeetingCommand
        {
            get => _frtcSetupMeetingCommand;
            set
            {
                _frtcSetupMeetingCommand = value;
            }
        }

        private RelayCommand<int> _copyToClipboardCommand;
        public RelayCommand<int> CopyToClipboardCommand
        {
            get => _copyToClipboardCommand;
            set => _copyToClipboardCommand = value;
        }

        private string _currentScrollList = string.Empty;
        public string CurrentScrollList
        {
            get { return _currentScrollList; }
            set
            {
                if (_currentScrollList != value)
                {
                    _currentScrollList = value;
                    RaisePropertyChanged("CurrentScrollList");
                }
            }
        }

        private RelayCommand<ScrollViewer> _resetScrollPositionCommand
            = new RelayCommand<ScrollViewer>((sv) => { sv?.ScrollToVerticalOffset(0); });
        public RelayCommand<ScrollViewer> ResetScrollPositionCommand
        {
            get => _resetScrollPositionCommand;
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

        private RelayCommand _frtcPopupScheduleMeetingDialog;
        public RelayCommand FRTCPopupScheduleMeetingDialog
        {
            get { return _frtcPopupScheduleMeetingDialog; }
            set
            {
                _frtcPopupScheduleMeetingDialog = value;
            }
        }

        private RelayCommand _frtcPopupChangePWDDialog;
        public RelayCommand FRTCPopupChangePWDDialog
        {
            get { return _frtcPopupChangePWDDialog; }
            set
            {
                _frtcPopupChangePWDDialog = value;
            }
        }

        private RelayCommand _frtcShowRecordingCommand;
        public RelayCommand FRTCShowRecordingCommand
        {
            get { return _frtcShowRecordingCommand; }
            set
            {
                _frtcShowRecordingCommand = value;
            }
        }

        private RelayCommand _frtcPopupDisplayNameDialog;
        public RelayCommand FRTCPopupDisplayNameDialog
        {
            get { return _frtcPopupDisplayNameDialog; }
            set
            {
                _frtcPopupDisplayNameDialog = value;
            }
        }

        private RelayCommand<string> _frtcChangeDisplayNameCommand;
        public RelayCommand<string> FRTCChangeDisplayNameCommand
        {
            get { return _frtcChangeDisplayNameCommand; }
            set
            {
                _frtcChangeDisplayNameCommand = value;
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

        private string _email = string.Empty;
        public string Email
        {
            get
            {
                return _email;
            }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _firstName = string.Empty;
        public string FirstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _lastName = string.Empty;
        public string LastName
        {
            get
            {
                return _lastName;
            }
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _realName = string.Empty;
        public string RealName
        {
            get
            {
                return _realName;
            }
            set
            {
                if (_realName != value)
                {
                    _realName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _mobile = string.Empty;
        public string Mobile
        {
            get
            {
                return _mobile;
            }
            set
            {
                if (_mobile != value)
                {
                    _mobile = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get
            {
                return _phone;
            }
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _department = string.Empty;
        public string Department
        {
            get
            {
                return _department;
            }
            set
            {
                if (_department != value)
                {
                    _department = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _country = string.Empty;
        public string Country
        {
            get
            {
                return _country;
            }
            set
            {
                if (_country != value)
                {
                    _country = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RelayCommand _signOutCommand;
        public RelayCommand SignOutCommand
        {
            get
            {
                return _signOutCommand;
            }
        }
    }
}
