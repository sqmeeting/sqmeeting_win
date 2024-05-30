using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using SQMeeting.MvvMMessages;
using GalaSoft.MvvmLight.Ioc;
using SQMeeting.Model;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using SQMeeting.Model.DataObj;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Drawing;
using System.Windows;
using SQMeeting.Utilities;
using System.Net;
using GalaSoft.MvvmLight.Messaging;
using SQMeeting.LogTool;
using SQMeeting.FRTCView;
using System.Windows.Navigation;
using System.Windows.Documents;

namespace SQMeeting.ViewModel
{
    public class RosterItem : ObservableObject
    {

        private string _remark = string.Empty;
        public string Remark { get => _remark; set { _remark = value; RaisePropertyChanged("Remark"); } }
        public string UUID { get; set; }

        private string _muteAuido = string.Empty;
        public string MuteAudio { get => _muteAuido; set { _muteAuido = value; RaisePropertyChanged("MuteAudio"); } }

        private string _muteVideo = string.Empty;
        public string MuteVideo { get => _muteVideo; set { _muteVideo = value; RaisePropertyChanged("MuteVideo"); } }

        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; RaisePropertyChanged("Name"); } }

        private bool _isLecturer = false;
        public bool IsLecturer
        {
            get => _isLecturer;
            set
            {
                _isLecturer = value;
                RaisePropertyChanged("IsLecturer");
            }
        }

        private bool _isPinned = false;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                _isPinned = value;
                RaisePropertyChanged("IsPinned");
            }
        }

        private bool _isSelf = false;
        public bool IsSelf
        {
            get => _isSelf;
            set
            {
                _isSelf = value;
                RaisePropertyChanged("IsSelf");
            }
        }
    }

    public class LecturerComparer : IComparer<RosterItem>
    {
        public int Compare(RosterItem left, RosterItem right)
        {
            if (left.IsLecturer && !right.IsLecturer)
            {
                return -1;
            }
            else if (!left.IsLecturer && right.IsLecturer)
            {
                return 1;
            }
            else
            {
                if (left.IsPinned && !right.IsPinned)
                {
                    return -1;
                }
                else if (!left.IsPinned && right.IsPinned)
                {
                    return 1;
                }
            }
            return 0;
        }
    }

    public class ContentSourceItem : ObservableObject
    {
        //0 - monitor,  1 - window
        public int SourceType { get; set; }
        public IntPtr Handle { get; set; }
        public string Name { get; set; }
        public long Left { get; set; }
        public long Top { get; set; }
        public long Right { get; set; }
        public long Bottom { get; set; }
        public string MonitorDeviceName { get; set; }
        public int MonitorIndex { get; set; }
        public bool IsPrimaryMonitor { get; set; }

        private Bitmap _screenShot = null;
        public Bitmap Screenshot
        {
            get => _screenShot;
            set { _screenShot = value; RaisePropertyChanged(); }
        }

        public ContentSourceItem(FRTCMonitor Monitor)
        {
            SourceType = 0;
            Handle = (IntPtr)Monitor.handle;
            Name = Monitor.monitorName;
            MonitorDeviceName = Monitor.deviceName;
            MonitorIndex = Monitor.index;
            IsPrimaryMonitor = Monitor.isPrimary;
            Left = Monitor.left;
            Top = Monitor.top;
            Right = Monitor.right;
            Bottom = Monitor.bottom;
            LogTool.LogHelper.Debug("Create ContentSourceItem monitor, {0}", Monitor.ToString());
        }

        public ContentSourceItem(int hwnd, string title)
        {
            SourceType = 1;
            Handle = (IntPtr)hwnd;
            Name = title;
        }

        private ThumbHelper _thumbHelper = null;
        public void StartThumb(IntPtr TargetWnd, Win32API.RECT ThumbRect)
        {
            _thumbHelper = new ThumbHelper();
            IntPtr hwnd = IntPtr.Zero;
            if (SourceType == 0)
            {
                hwnd = Win32API.GetDesktopWindow();
            }
            else
            {
                hwnd = Handle;
            }
            _thumbHelper.Init(TargetWnd, hwnd, ThumbRect);
            _thumbHelper.Update();
        }

        public void UpdateThumbLocation(Win32API.RECT ThumbRect)
        {
            if (_thumbHelper != null)
            {
                _thumbHelper.Update(ThumbRect);
            }
        }

        public void StartScreenThumb()
        {
            if (SourceType != 0)
                return;
            try
            {
                Screenshot = ThumbHelper.GetSreenshot(new Rect(Left, Top, Right - Left, Bottom - Top));
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
                LogTool.LogHelper.Debug("Start screen thumb left is {0}, top is {1}, right is {2}, bottom is {3}", Left, Top, Right, Bottom);
                Screenshot = null;
                return;
            }
        }

        public void StopThumb()
        {
            _thumbHelper?.Stop();
        }
    }

    public class MeetingMessageInfo : ViewModelBase2
    {
        private string msgColor;
        public string MsgColor
        {
            get
            {
                return msgColor;
            }
            set
            {
                if (msgColor != value)
                {
                    msgColor = value;
                    OnPropertyChanged("MsgColor");
                }
            }
        }
        private string msgContent;
        public string MsgContent
        {
            get
            {
                return msgContent;
            }
            set
            {
                if (msgContent != value)
                {
                    msgContent = value;
                    OnPropertyChanged("MsgContent");
                }
            }
        }
        private string msgDisplaySpeed;
        public string MsgDisplaySpeed
        {
            get
            {
                return msgDisplaySpeed;
            }
            set
            {
                if (msgDisplaySpeed != value)
                {
                    msgDisplaySpeed = value;
                    OnPropertyChanged("MsgDisplaySpeed");
                }
            }
        }

        private string msgFont;
        public string MsgFont
        {
            get
            {
                return msgFont;
            }
            set
            {
                if (msgFont != value)
                {
                    msgFont = value;
                    OnPropertyChanged("MsgFont");
                }
            }
        }
        private int msgBackgroundTransparency;
        public int MsgBackgroundTransparency
        {
            get
            {
                return msgBackgroundTransparency;
            }
            set
            {
                if (msgBackgroundTransparency != value)
                {
                    msgBackgroundTransparency = value;
                    OnPropertyChanged("MsgBackgroundTransparency");
                }
            }
        }
        private int msgDisplayRepetition;
        public int MsgDisplayRepetition
        {
            get
            {
                return msgDisplayRepetition;
            }
            set
            {
                if (msgDisplayRepetition != value)
                {
                    msgDisplayRepetition = value;
                    OnPropertyChanged("MsgDisplayRepetition");
                }
            }
        }

        private int msgFontSize;
        public int MsgFontSize
        {
            get
            {
                return msgFontSize;
            }
            set
            {
                if (msgFontSize != value)
                {
                    msgFontSize = value;
                    OnPropertyChanged("MsgFontSize");
                }
            }
        }

        private int msgVerticalPosition;
        public int MsgVerticalPosition
        {
            get
            {
                return msgVerticalPosition;
            }
            set
            {
                if (msgVerticalPosition != value)
                {
                    msgVerticalPosition = value;
                    OnPropertyChanged("MsgVerticalPosition");
                }
            }
        }

        private bool enabledMeetingMessage;
        public bool EnabledMeetingMessage
        {
            get
            {
                return enabledMeetingMessage;
            }
            set
            {
                if (enabledMeetingMessage != value)
                {
                    enabledMeetingMessage = value;
                    OnPropertyChanged("EnabledMeetingMessage");
                }
            }
        }

        private bool _showMeetingMessage;
        public bool ShowMeetingMessage
        {
            get
            {
                return _showMeetingMessage;
            }
            set
            {
                if (_showMeetingMessage != value)
                {
                    _showMeetingMessage = value;
                    OnPropertyChanged("ShowMeetingMessage");
                }
            }
        }
    }

    public class SelfStatisticsInfo : ObservableObject
    {
        private string _delay = string.Empty;
        public string Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                RaisePropertyChanged("Delay");
            }
        }
        private int _totalUploadRate = 0;
        public int TotalUploadRate
        {
            get => _totalUploadRate;
            set
            {
                _totalUploadRate = value;
                RaisePropertyChanged("TotalUploadRate");
            }
        }
        private int _totalDownloadRate = 0;
        public int TotalDownloadRate
        {
            get => _totalDownloadRate;
            set
            {
                _totalDownloadRate = value;
                RaisePropertyChanged("TotalDownloadRate");
            }
        }

        private string _audioUploadRate = string.Empty;
        public string AudioUploadRate
        {
            get => _audioUploadRate;
            set
            {
                _audioUploadRate = value;
                RaisePropertyChanged("AudioUploadRate");
            }
        }

        private string _audioDownloadRate = string.Empty;
        public string AudioDownloadRate
        {
            get => _audioDownloadRate;
            set
            {
                _audioDownloadRate = value;
                RaisePropertyChanged("AudioDownloadRate");
            }
        }

        private string _videoUploadRate = string.Empty;
        public string VideoUploadRate
        {
            get => _videoUploadRate;
            set
            {
                _videoUploadRate = value;
                RaisePropertyChanged("VideoUploadRate");
            }
        }

        private string _videoDownloadRate = string.Empty;
        public string VideoDownloadRate
        {
            get => _videoDownloadRate;
            set
            {
                _videoDownloadRate = value;
                RaisePropertyChanged("VideoDownloadRate");
            }
        }

        private string _contentUploadRate = string.Empty;
        public string ContentUploadRate
        {
            get => _contentUploadRate;
            set
            {
                _contentUploadRate = value;
                RaisePropertyChanged("ContentUploadRate");
            }
        }
        private string _contentDownloadRate = string.Empty;
        public string ContentDownloadRate
        {
            get => _contentDownloadRate;
            set
            {
                _contentDownloadRate = value;
                RaisePropertyChanged("ContentDownloadRate");
            }
        }
    }
    public class FRTCMeetingVideoViewModel : FRTCViewModelBase
    {
        public FRTCMeetingVideoViewModel()
        {
            m_MeetingMsgLockObj = new object();
            m_unmuteApplicationsLockObj = new object();
            m_recordingStreamingLockObj = new object();
            m_rosterListLockObj = new object();

            CurSharingWndHwnd = IntPtr.Zero;

            MessengerInstance.Register<JoinFRTCMeetingMsg>(this, OnJoinMeeting);
            MessengerInstance.Register<FRTCCallStateChangeMessage>(this, OnCallStateChanged);
            MessengerInstance.Register<NotificationMessage<FrtcLayout>>(this, OnLayoutChanged);

            MessengerInstance.Register<MeetingControlOperationResultMessage>(this, new Action<MeetingControlOperationResultMessage>((msg) =>
            {
                HandleMeetingControlErrorMsg(msg.StatusCode, msg.ErrorCode, msg.APIName, msg.ResultParam);
            }));

            MessengerInstance.Register<NotificationMessage<string>>(this, (msg) =>
            {
                if (msg.Notification == "join_other_meeting_notify")
                {
                    Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                    {
                        _meetingVideoWnd.Show();
                        _meetingVideoWnd.Activate();
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(string.Empty, Properties.Resources.FRTC_MEETING_JOIN_OTHER_MEETING_TIP, Properties.Resources.FRTC_MEETING_SDKAPP_OK, _meetingVideoWnd);
                    }));
                }
                else if (msg.Notification == "add_meeting_to_list_notify")
                {
                    Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                    {
                        _meetingVideoWnd.Show();
                        _meetingVideoWnd.Activate();
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(string.Empty, msg.Content, Properties.Resources.FRTC_MEETING_SDKAPP_OK, _meetingVideoWnd);
                    }));
                }
            });

            this.ShutdownAfterClose = false;

            this.EnableRecordingAndStreaming = SimpleIoc.Default.GetInstance<SettingViewModel>().EnableRecordingAndStreaming;

            InitCommands();
        }

        ~FRTCMeetingVideoViewModel()
        {
        }

        public static OnMeetingControlMsgCallback pfnMeetingControlMsgCallback = new OnMeetingControlMsgCallback(OnMeetingControlMessage);
        public static OnContentSendingState pfnContentSendingState = new OnContentSendingState(OnContentSendingState);
        public static OnWndMouseEvent pfnVideoEventCallBack = new OnWndMouseEvent(OnWndMouseEventCB);
        public static OnEncryptedStateNotifyCallback pfnEncryptedStateNotifyCallback = new OnEncryptedStateNotifyCallback(OnEncryptedStateNotifyCB);
        public static OnReminderNotify pfnOnReminderNotifyCallback = new OnReminderNotify(OnReminderNotify);

        object m_MeetingMsgLockObj;

        object m_unmuteApplicationsLockObj;

        object m_recordingStreamingLockObj;

        object m_rosterListLockObj;


        public Thread m_meetingWndThread = null;
        public MeetingWindowHost _videoWndHost = null;

        Queue<string> m_unHandledMeetingControlMsg = new Queue<string>();

        string _meeting_id = string.Empty;


        private string _tmpDisplayName = string.Empty;
        private string _renameUserUUID = string.Empty;
        private string _displayName = string.Empty;
        public string DisplayName { get => _displayName; set { _displayName = value; RaisePropertyChanged("DisplayName"); } }
        string _selfUUID = string.Empty;
        int _callRate = 0;

        FRTCCallManager m_callManager = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCCallManager>();

        bool _shareContentEnable = true;

        bool _contentPeopleWndCollapsed = false;

        public View.MeetingVideoWindow _meetingVideoWnd = null;
        public View.StatisticsWindow statisticsWnd = null;
        public View.RosterListWindow rosterListWindow = null;
        public FRTCView.MeetingInviteInfoWindow inviteInfoWindow = null;
        public FRTCView.PeopleVideoCollapsedWindow peopleVideoCollapsedWindow = null;
        public FRTCView.FRTCContentSourceWindow frtcContentSourceWindow = null;

        public FRTCView.StatusWidgetWindow recordingStatusWidget = null;
        public FRTCView.StatusWidgetWindow streamingStatusWidget = null;
        public FRTCView.ShareStreamingURLWindow shareStreamingInfoWindow = null;

        FRTCView.MeetingControlApplicationsWindow meetingControlApplicationsWindow = null;

        DateTime _startTimer;
        DispatcherTimer updateStatisticsTimer = null;
        DispatcherTimer UpdateTitleTimer = null;
        DispatcherTimer ToolbarShowTimer = null;
        DispatcherTimer ShowTipsTimer = null;
        DispatcherTimer HideSharingBarTimer = null;
        DispatcherTimer ShowPersonMessageTimer = null;

        public void MuteLocalAudio(bool bMute)
        {
            LogHelper.DebugMethodEnter();
            FRTCSDK.frtc_local_audio_mute(bMute);
        }

        private void InitCommands()
        {
            _copyMeetingInfoCommand = new RelayCommand(() =>
            {
                StringBuilder info = new StringBuilder();
                info.Append(MeetingName);
                info.Append(Environment.NewLine);
                info.Append(Environment.NewLine);
                info.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGID);
                info.Append("\t\t");
                info.Append(MeetingID);
                info.Append(Environment.NewLine);
                info.Append(Environment.NewLine);
                info.Append(Properties.Resources.FRTC_MEETING_SDKAPP_HOST);
                info.Append("\t\t");
                info.Append(MeetingOwnerName);
                if (!string.IsNullOrEmpty(MeetingPassCode))
                {
                    info.Append(Environment.NewLine);
                    info.Append(Environment.NewLine);
                    info.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGPASSCODE);
                    info.Append("\t\t");
                    info.Append(MeetingPassCode);
                }

                try
                {
                    System.Windows.Clipboard.SetText(info.ToString());
                }
                catch (Exception ex)
                {
                    LogTool.LogHelper.Exception(ex);
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_FAILED });
                    return;
                }
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE });
            });

            this._copyMeetingInviteInfoCommand = new RelayCommand(() =>
            {
                try
                {
                    System.Windows.Clipboard.SetText(MeetingInviteString);
                }
                catch (Exception ex)
                {
                    LogTool.LogHelper.Exception(ex);
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_FAILED });
                    return;
                }
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE });
            });

            _muteLocalVideo = new RelayCommand<bool>((isChecked) =>
            {
                if (isChecked)
                {
                    FRTCSDK.frtc_local_video_stop();
                }
                else
                {
                    FRTCSDK.frtc_local_video_start();
                }
            });

            _muteMic = new RelayCommand<bool>((isChecked) =>
            {
                LogTool.LogHelper.Debug("Mute Mic button clicked, isChecked {0}, AllowUnmute {1}", isChecked, AllowUnmute);
                if (!AllowUnmute && !IsMeetingOwner && !IsOperatorRole && !isChecked)
                {
                    Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                    {
                        if (DateTime.Now.Subtract(_unmuteApplicationTime) > TimeSpan.FromSeconds(60))
                        {
                            if (FRTCView.FRTCMessageBox.ShowConfirmMessage(
                                Properties.Resources.FRTC_MEETING_CURRENTLY_MUTED,
                                Properties.Resources.FRTC_MEETING_APPLY_UNMUTE_TIP,
                                Properties.Resources.FRTC_MEETING_APPLY_UNMUTE,
                                Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL,
                                false, _meetingVideoWnd, true))
                            {
                                m_callManager.ApplyForUnmute(MeetingID);
                            }
                            MicMuted = true;
                        }
                        else
                        {
                            FRTCView.FRTCMessageBox.ShowNotificationMessage(
                                Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_SENT,
                                Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_SENT_TIP);
                            MicMuted = true;
                        }

                    }));
                }
                else
                {
                    MuteLocalAudio(isChecked);
                    if (isChecked)
                        ShowTips(FrtcReminderType.MICROPHONEMUTED);
                    else
                        ShowTips(FrtcReminderType.MICROPHONE_UNMUTED);
                }
            });

            MeetingMsgInfo = new MeetingMessageInfo();

            _fullScreenCommand = new RelayCommand(() =>
            {
                HandleFullScreen();
            });

            _startStatiticsTimerCommand = new RelayCommand(() =>
            {
                if (updateStatisticsTimer == null)
                {
                    return;
                }
                updateStatisticsTimer.Stop();
                updateStatisticsTimer.Start();
            });

            _stopStatiticsTimerCommand = new RelayCommand(() =>
            {
                if (updateStatisticsTimer == null || (statisticsWnd != null && statisticsWnd.IsVisible))
                {
                    return;
                }
                updateStatisticsTimer.Stop();
            });

            _startShareDesk = new RelayCommand(() =>
            {
                ShowFRTCContentSourceWnd();
            });

            _popupInviteWnd = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    if (inviteInfoWindow != null)
                    {
                        inviteInfoWindow.Close();
                        inviteInfoWindow = null;
                    }

                    if (inviteInfoWindow == null)
                    {
                        inviteInfoWindow = new FRTCView.MeetingInviteInfoWindow();
                    }
                    inviteInfoWindow.Owner = this._meetingVideoWnd;
                    inviteInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    inviteInfoWindow.Show();
                });
            });

            _popupStreamingInfoWindow = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    if (shareStreamingInfoWindow != null)
                    {
                        shareStreamingInfoWindow.Close();
                        shareStreamingInfoWindow = null;
                    }

                    if (shareStreamingInfoWindow == null)
                    {
                        shareStreamingInfoWindow = new FRTCView.ShareStreamingURLWindow();
                    }
                    shareStreamingInfoWindow.Owner = this._meetingVideoWnd;
                    shareStreamingInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    shareStreamingInfoWindow.Show();
                });
            });

            _popupRosterWnd = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    ShowRosterListWindow();
                });
            });

            _popupMuteDialogCommand = new RelayCommand<RosterItem>((item) =>
            {
                if (item == null)
                    return;
                if (item.UUID != FRTCUIUtils.GetFRTCDeviceUUID())
                {
                    if (IsGuestMeeting || (!IsOperatorRole && !IsMeetingOwner))
                        return;
                }
                if (CurrentSelectedParticipant == null || SelectedParticipantCopy != null)
                    return;
                SelectedParticipantCopy = new RosterItem()
                {
                    Name = item.Name,
                    MuteAudio = item.MuteAudio,
                    MuteVideo = item.MuteVideo,
                    UUID = item.UUID,
                    IsLecturer = item.IsLecturer,
                    IsPinned = item.IsPinned,
                    IsSelf = item.IsSelf,
                };
                object[] p = new object[1];
                p[0] = _meetingVideoWnd;
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCMuteOneParticipant, p);

                    SelectedParticipantCopy = null;
                    CurrentSelectedParticipant = null;
                }));
            });

            _muteOneParticipant = new RelayCommand<RosterItem>((p) =>
            {
                if (p == null)
                    return;
                if (p.UUID == FRTCUIUtils.GetFRTCDeviceUUID())
                {
                    try
                    {
                        bool mute = !bool.Parse(p.MuteAudio);
                        if (!AllowUnmute && !IsOperatorRole && !mute)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                if (DateTime.Now.Subtract(_unmuteApplicationTime) > TimeSpan.FromSeconds(60))
                                {
                                    if (FRTCView.FRTCMessageBox.ShowConfirmMessage(
                                        Properties.Resources.FRTC_MEETING_CURRENTLY_MUTED,
                                        Properties.Resources.FRTC_MEETING_APPLY_UNMUTE_TIP,
                                        Properties.Resources.FRTC_MEETING_APPLY_UNMUTE,
                                        Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL,
                                        false, _meetingVideoWnd, true))
                                    {
                                        m_callManager.ApplyForUnmute(MeetingID);
                                    }
                                    MicMuted = true;
                                }
                                else
                                {
                                    FRTCView.FRTCMessageBox.ShowNotificationMessage(
                                        Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_SENT,
                                        Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_SENT_TIP);
                                    MicMuted = true;
                                }
                            }));
                        }
                        else
                        {
                            MuteLocalAudio(mute);
                            if (mute)
                                ShowTips(FrtcReminderType.MICROPHONEMUTED);
                            else
                                ShowTips(FrtcReminderType.MICROPHONE_UNMUTED);
                            MicMuted = mute;
                        }
                    }
                    catch { }
                }
                else
                {
                    if (p.MuteAudio == "true")
                    {
                        m_callManager.UnmuteUsers(this.MeetingID, new List<string>() { p.UUID });
                    }
                    else if (p.MuteAudio == "false")
                    {
                        m_callManager.MuteUsers(this.MeetingID, new List<string>() { p.UUID }, AllowUnmute);
                    }
                }
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                SelectedParticipantCopy = null;
                CurrentSelectedParticipant = null;
            });

            _popupRenameDlgCommand = new RelayCommand<RosterItem>((p) =>
            {
                RosterItem tmp = SelectedParticipantCopy;
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                FRTCView.ChangeDisplayNameWindow renameWnd = new FRTCView.ChangeDisplayNameWindow();
                renameWnd.Owner = rosterListWindow;
                SelectedParticipantCopy = tmp;
                renameWnd.ShowDialog();
                if (renameWnd.DialogResult.HasValue && renameWnd.DialogResult.Value)
                {
                    if (SelectedParticipantCopy != null)
                    {
                        string newName = renameWnd.tbName.Text.Trim();
                        if (p.UUID == FRTCUIUtils.GetFRTCDeviceUUID())
                            _tmpDisplayName = newName;
                        _renameUserUUID = p.UUID;
                        m_callManager.RenameUser(
                            this.MeetingID,
                            p.UUID,
                            newName,
                            IsGuestMeeting ? string.Empty : CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCUserManager>().UserData?.user_token);
                    }
                }
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                CurrentSelectedParticipant = null;
                SelectedParticipantCopy = null;
            });

            _setAsLecturerCommand = new RelayCommand<RosterItem>((p) =>
            {
                if (!p.IsLecturer)
                {
                    m_callManager.SetLecturer(this.MeetingID, p.UUID);
                }
                else
                    m_callManager.UnsetLecturer(this.MeetingID);
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
            });

            _pinVideoCommand = new RelayCommand<RosterItem>((p) =>
            {
                if (!p.IsPinned)
                {
                    m_callManager.PinVideo(this.MeetingID, new string[] { p.UUID });
                }
                else
                    m_callManager.UnpinVideo(this.MeetingID);
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
            });

            _removeFromMeetingCommand = new RelayCommand<RosterItem>((p) =>
            {
                if (FRTCView.FRTCMessageBox.ShowConfirmMessage(Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_FROM_MEETING, Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_FROM_MEETING_TIP))
                {
                    m_callManager.KickOutParticipants(this.MeetingID, new List<string>() { p.UUID }, false);
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                }
            });

            _popupMuteAllDialogCommand = new RelayCommand<string>((action) =>
            {
                MuteAllState = action == "mute";
                object[] p = new object[1];
                p[0] = _meetingVideoWnd;
                bool? ret = FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCMuteAll, p);
            });

            _muteAllCommand = new RelayCommand<bool>((allowSelfUnmute) =>
            {
                FRTCCallManager cm = m_callManager;
                if (MuteAllState)
                {
                    cm.MuteAll(_meeting_id, allowSelfUnmute);
                }
                else
                {
                    cm.UnmuteAll(_meeting_id);
                }
                if (FRTCView.FRTCPopupViewManager.CurrentPopup != null)
                    FRTCView.FRTCPopupViewManager.CurrentPopup.DialogResult = true;
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();

                //do unmute self after mute all -- defined product behavior
                if (MicMuted)
                {
                    MuteLocalAudio(false);
                    MicMuted = false;
                }
            });

            _popHangUpDialogCommand = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                {
                    object[] p = new object[1];
                    p[0] = _meetingVideoWnd;
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCHangUp, p);
                }), null);
            });

            this._finishCallCommand = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                {
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Hide();
                    if (FRTCView.FRTCMessageBox.ShowConfirmMessage(Properties.Resources.FRTC_MEETING_END_CONFIRM, Properties.Resources.FRTC_MEETING_END_CONFIRM_MSG))
                    {
                        m_callManager.EndCall(this.MeetingID);
                    }
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                }), null);
            });

            this._dropCallCommand = new RelayCommand<UIElement>((sender) =>
            {
                LogHelper.DebugMethodEnter();
                LogHelper.Debug("UI drop call in meeting, sender is {0}", (sender as FrameworkElement).Name);
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    m_callManager.DropCall();
                });
            });

            this._reconnectCallCommand = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                    m_callManager.ReconnectMeeting();
                });
            });

            this._shareContentCommand = new RelayCommand<ContentSourceItem>(StartShareContent);

            this._stopContentCommand = new RelayCommand(() =>
            {
                StopShareContent();
            });

            this._showContentPeopleCommand = new RelayCommand(() =>
            {
                this._meetingVideoWnd.Show();
                this.ShowContentPeopleCollapsedWindow(false);
            });

            this._foldContentPeopleCommand = new RelayCommand(() =>
            {
                this._meetingVideoWnd.Hide();
                this.ShowContentPeopleCollapsedWindow(true);
            });

            _hideSharingBarCommand = new RelayCommand(() =>
            {
                _sharingToolBar?.StartShowOrHide(false);
                HideSharingBarTimer?.Stop();
            });

            _CloseWnd = new RelayCommand(() =>
            {
                if (m_meetingWndThread != null)
                {
                    Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                    {
                        if (_meetingVideoWnd != null)
                        {
                            _meetingVideoWnd.Close();
                        }
                    });
                }
            });

            _showSettingViewCommand = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCSetting, new object[] { _meetingVideoWnd });
                });
            });

            _meetingMsgSetPosCommand = new RelayCommand<string>((pos) =>
            {
                meetingMsgVerticalPos = int.Parse(pos);
            });

            _meetingMsgAddRepeatTimesCommand = new RelayCommand<string>((diff) =>
            {
                int n = int.Parse(diff);
                if (string.IsNullOrEmpty(MeetingMessageRepeatTimes))
                    MeetingMessageRepeatTimes = "1";
                else
                {
                    int times = int.Parse(MeetingMessageRepeatTimes) + n;
                    if (times < 1)
                        times = 1;
                    if (times > 999)
                        times = 999;

                    MeetingMessageRepeatTimes = times.ToString();
                }
            });

            _sendMeetingMsgCommand = new RelayCommand<string>((p) =>
            {
                bool start = bool.Parse(p);
                if (start)
                {
                    SendMeetingMsgText = Properties.Resources.FRTC_MEETING_TEXTOVERLAY_DEFAULT_TXT;
                    MeetingMsgEnableScroll = true;
                    MeetingMessageRepeatTimes = "3";
                    meetingMsgVerticalPos = 0;
                    bool? ret = FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCSendMeetingMsg, new object[] { _meetingVideoWnd });
                    if (ret.HasValue && ret.Value)
                    {
                        int repeatTimes = int.Parse(MeetingMessageRepeatTimes);
                        m_callManager.SendMeetingMessage(this.MeetingID, SendMeetingMsgText, repeatTimes, meetingMsgVerticalPos, MeetingMsgEnableScroll);
                    }
                    else
                    {
                        SendMeetingMsgEnabled = false;
                    }
                }
                else
                {
                    m_callManager.StopMeetingMessage(this.MeetingID);
                }
            });

            _stopStreamingOrRecording = new RelayCommand<string>((p) =>
            {
                lock (m_recordingStreamingLockObj)
                {
                    if (p == "Recording")
                    {
                        if (FRTCView.FRTCMessageBoxBig.ShowStopRecordingConfirmWindow())
                            m_callManager.StopRecording(this.MeetingID);
                    }
                    else if (p == "Streaming")
                    {
                        if (FRTCView.FRTCMessageBoxBig.ShowStopStreamingConfirmWindow())
                            m_callManager.StopStreaming(this.MeetingID);
                    }
                }
            });

            _enableRecordingCommand = new RelayCommand<bool>((p) =>
            {
                if (p)
                {
                    {
                        if (FRTCView.FRTCMessageBoxBig.ShowRecordingConfirmWindow())
                            m_callManager.StartRecording(this.MeetingID);
                        else
                            IsRecording = _isRecording;
                    }
                }
                else
                {
                    if (FRTCView.FRTCMessageBoxBig.ShowStopRecordingConfirmWindow())
                        m_callManager.StopRecording(this.MeetingID);
                    else
                        IsRecording = _isRecording;
                }
            });

            _enableStreamingCommand = new RelayCommand<bool>((p) =>
            {
                if (p)
                {
                    {
                        bool enableStreamingPWD = false;
                        if (FRTCView.FRTCMessageBoxBig.ShowStreamingConfirmWindow(out enableStreamingPWD))
                        {
                            string streamingPWD = string.Empty;
                            if (enableStreamingPWD)
                            {
                                Random generator = new Random();
                                streamingPWD = generator.Next(0, 1000000).ToString("D6");
                            }
                            m_callManager.StartStreaming(this.MeetingID, streamingPWD);
                        }
                        else
                            IsStreaming = _isStreaming;
                    }
                }
                else
                {
                    if (FRTCView.FRTCMessageBoxBig.ShowStopStreamingConfirmWindow())
                        m_callManager.StopStreaming(this.MeetingID);
                    else
                        IsStreaming = _isStreaming;
                }
            });

            _popupUnmuteApplicationList = new RelayCommand(() =>
            {
                FRTCView.FRTCReminderToast.CloseReminder();
                if (m_meetingWndThread != null && UnmuteApplicationsList != null)
                {
                    Dispatcher.FromThread(m_meetingWndThread).Invoke(new Action(() =>
                    {
                        if (meetingControlApplicationsWindow == null)
                        {
                            meetingControlApplicationsWindow = new FRTCView.MeetingControlApplicationsWindow();
                            meetingControlApplicationsWindow.Owner = rosterListWindow == null ? _meetingVideoWnd as Window : rosterListWindow;
                            meetingControlApplicationsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            meetingControlApplicationsWindow.Closed += (s, e) => meetingControlApplicationsWindow = null;
                            meetingControlApplicationsWindow.ShowDialog();
                            if (UnmuteApplicationsList.Count > 0)
                            {
                                NewUnmuteApplicationsNotify = String.Format(Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_NOTIFY, UnmuteApplicationsList.Last().Name);
                            }
                            NewUnmuteApplications = false;
                        }
                    }));
                }
            });

            _approveUmuteApplication = new RelayCommand<string>((id) =>
            {
                if (!string.IsNullOrEmpty(id))
                {
                    m_callManager.ApproveUnmuteApplication(MeetingID, new List<string>() { id });
                }
            });

            _approveAllUmuteApplications = new RelayCommand(() =>
            {
                lock (m_unmuteApplicationsLockObj)
                {
                    if (UnmuteApplicationsList.Count > 0)
                    {
                        m_callManager.ApproveUnmuteApplication(MeetingID, UnmuteApplicationsList.Select(x => x.UUID));
                    }
                }
            });

            _ignoreUmuteApplication = new RelayCommand<string>((id) =>
            {
                lock (m_unmuteApplicationsLockObj)
                {
                    if (UnmuteApplicationsList != null && UnmuteApplicationsList.Count > 0)
                    {
                        var ret = UnmuteApplicationsList.FirstOrDefault((r) => { return r.UUID == id; });
                        if (ret != null)
                        {
                            UnmuteApplicationsList.Remove(ret);
                        }
                    }
                }
            });

            _switchLayoutCommand = new RelayCommand<string>((v) =>
            {
                FRTCSDK.frtc_layout_config(v == "0" ? FrtcLayout.LAYOUT_GALLERY : FrtcLayout.LAYOUT_AUTO);
                CurrentLayout = v == "0" ? FrtcLayout.LAYOUT_GALLERY : FrtcLayout.LAYOUT_AUTO;
                MessengerInstance.Send(new NotificationMessage<string>(CurrentLayout.ToString(), "set_video_layout_incall"));
            });

            _hideMyVideoCommand = new RelayCommand(() =>
            {
                FRTCSDK.frtc_local_preview_hide(_localVideoEnabled);
                LocalVideoEnabled = !_localVideoEnabled;
            });

            _popupStatisticsWnd = new RelayCommand(() =>
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    if (statisticsWnd != null)
                    {
                        statisticsWnd.Close();
                        statisticsWnd = null;
                    }
                    if (this.MediaStatistics != null)
                    {
                        this.MediaStatistics.Clear();
                        this.CallInfo = string.Empty;
                    }
                    statisticsWnd = new View.StatisticsWindow();
                    statisticsWnd.Owner = _meetingVideoWnd;
                    statisticsWnd.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                    statisticsWnd.Closed += new EventHandler((s, a) =>
                    {
                        if (updateStatisticsTimer != null)
                        {
                            updateStatisticsTimer.Stop();
                        }
                    });

                    statisticsWnd.Show();
                    updateStatisticsTimer.Start();
                });
            });
        }

        private void OnLayoutChanged(NotificationMessage<FrtcLayout> msg)
        {
            if (msg.Notification.ToLower() == "set_video_layout")
            {
                CurrentLayout = msg.Content;
                IsGalleryView = CurrentLayout == FrtcLayout.LAYOUT_GALLERY;
            }
        }

        void GetParticipantList()
        {
            IntPtr participants = FRTCSDK.frtc_participants_collect();
            if (participants != IntPtr.Zero)
            {
                string jStr = FRTCUIUtils.StringFromNativeUtf8(participants);
                if (!string.IsNullOrEmpty(jStr))
                {
                    try
                    {
                        JObject token = JObject.Parse(jStr);
                        if (token == null || !token.ContainsKey("participant_list"))
                        {
                            return;
                        }
                        var jToken = token["participant_list"];
                        if (jToken == null)
                        {
                            return;
                        }
                        bool bFullList = jToken.Value<bool>("fullList");
                        var rosters = jToken["rosters"];
                        if (rosters == null || !rosters.HasValues)
                        {
                            RosterList = new List<RosterItem>();

                            RosterItem selfItem = new RosterItem();
                            selfItem.UUID = _selfUUID;
                            selfItem.Name = DisplayName;
                            selfItem.IsLecturer = HasLecture && _currentLecturerList.Contains(_selfUUID);
                            selfItem.Remark = Properties.Resources.FRTC_MEETING_ROSTER_ME;
                            selfItem.IsPinned = PinnedUUID == _selfUUID;
                            selfItem.MuteAudio = MicMuted.ToString().ToLower(); ;
                            selfItem.MuteVideo = CameraMuted.ToString().ToLower();
                            selfItem.IsSelf = true;
                            RosterList.Insert(0, selfItem);

                            RaisePropertyChanged("RosterList");
                            return;
                        }
                        string strRoster = rosters.ToString();
                        JArray jlist = JArray.Parse(strRoster);
                        if (jlist == null)
                            return;
                        if (jlist.Count == 1)
                        {
                            LocalOnly = true;
                        }
                        else if (jlist.Count > 1)
                        {
                            LocalOnly = false;
                        }

                        List<RosterItem> RosterItem = new List<RosterItem>();
                        for (int i = 0; i < jlist.Count; ++i)
                        {
                            RosterItem item = new RosterItem();
                            JObject tempo = JObject.Parse(jlist[i].ToString());
                            if (tempo == null)
                                continue;
                            if (tempo.ContainsKey("UUID"))
                                item.UUID = tempo["UUID"].ToString();
                            if (tempo.ContainsKey("muteAudio"))
                                item.MuteAudio = tempo["muteAudio"].ToString();
                            if (tempo.ContainsKey("muteVideo"))
                                item.MuteVideo = tempo["muteVideo"].ToString();
                            if (tempo.ContainsKey("name"))
                                item.Name = tempo["name"].ToString();
                            if (!string.IsNullOrEmpty(item.UUID))
                            {
                                if (_currentLecturerList.Contains(item.UUID))
                                {
                                    item.IsLecturer = true;
                                }
                            }

                            RosterItem.Add(item);
                        }

                        if (RosterItem.Count() == 0)
                        {
                            return;
                        }

                        List<RosterItem> userlist = rosters.ToObject<List<RosterItem>>();
                        if (bFullList)
                        {
                            CreateList(RosterItem);
                        }
                        else
                        {
                            UpdateList(RosterItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogTool.LogHelper.Exception(ex);
                    }
                }
            }
        }

        public FRTCMonitor GetMonitorList(int index)
        {
            IntPtr monitors = FRTCSDK.frtc_monitors_collect();
            if (monitors != IntPtr.Zero)
            {
                string jStr = FRTCUIUtils.StringFromNativeUtf8(monitors);
                JObject jObj = JObject.Parse(jStr);
                JArray array = (JArray)jObj["monitors"];

                return findMonitor(array, index);
            }

            return null;
        }

        private void ShowFRTCContentSourceWnd()
        {
            try
            {
                IntPtr monitors = FRTCSDK.frtc_monitors_collect();
                if (monitors != IntPtr.Zero)
                {
                    string jStr = FRTCUIUtils.StringFromNativeUtf8(monitors);
                    JObject jObj = JObject.Parse(jStr);
                    JArray array = (JArray)jObj["monitors"];
                    MonitorList = CreateMonitorList(array);
                    ContentSourceList = new ObservableCollection<ContentSourceItem>(MonitorList.Select<FRTCMonitor, ContentSourceItem>((m) =>
                    {
                        return new ContentSourceItem(m);
                    }));
                }
            }
            catch (Exception e)
            {
                LogTool.LogHelper.Exception(e);
            }
            try
            {
                IntPtr windows = FRTCSDK.frtc_windows_collect();
                if (windows != IntPtr.Zero)
                {
                    string jStr = FRTCUIUtils.StringFromNativeUtf8(windows);
                    JObject jObj = JObject.Parse(jStr);
                    JArray array = (JArray)jObj["windows"];
                    WindowList = CreateWindowList(array);
                    if (ContentSourceList != null)
                    {
                        ContentSourceList = new ObservableCollection<ContentSourceItem>(ContentSourceList.Concat(windowList));
                    }
                    else
                    {
                        ContentSourceList = windowList;
                    }
                }
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (frtcContentSourceWindow != null)
                {
                    frtcContentSourceWindow.Close();
                    frtcContentSourceWindow = null;
                }
                frtcContentSourceWindow = new FRTCView.FRTCContentSourceWindow();
                frtcContentSourceWindow.Owner = _meetingVideoWnd;
                frtcContentSourceWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                frtcContentSourceWindow.Closed += (s, e) => { frtcContentSourceWindow = null; };
                frtcContentSourceWindow.Show();
            });
        }

        private void StartShareContent(ContentSourceItem Source)
        {
            if (Source != null)
            {
                if (IsFullScreen)
                    HandleFullScreen();
                if (Source.SourceType == 0)
                {
                    CurMonInfo = new FRTCMonitor()
                    {
                        monitorName = Source.Name,
                        handle = Source.Handle.ToInt32(),
                        index = Source.MonitorIndex,
                        deviceName = Source.MonitorDeviceName,
                        isPrimary = Source.IsPrimaryMonitor,
                        left = (int)Source.Left,
                        top = (int)Source.Top,
                        right = (int)Source.Right,
                        bottom = (int)Source.Bottom
                    };
                    FRTCSDK.frtc_desktop_share(Source.MonitorIndex, IsShareAudio);
                }
                else
                {
                    CurSharingWndHwnd = Source.Handle;
                    LogTool.LogHelper.Debug("set current sharing wnd as {0}", Source.Handle);
                    FRTCSDK.frtc_window_share(Source.Handle.ToInt32(), IsShareAudio);
                }
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    if (frtcContentSourceWindow != null)
                    {
                        frtcContentSourceWindow.Close();
                        frtcContentSourceWindow = null;
                    }
                });
            }
            else
            {
                LogTool.LogHelper.Error("StartShareContent Failed, content source item is null");
            }
        }

        public void StopShareContent()
        {
            if (m_meetingWndThread == null)
                return;
            ShowContentPeopleCommand.Execute(null);
            ResolutionTools.SetContentSharing(false);
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                FRTCSDK.frtc_content_stop();
            });
        }

        private void ShowContentPeopleCollapsedWindow(bool show)
        {
            try
            {
                _contentPeopleWndCollapsed = show;
                if (show)
                {
                    recordingStatusWidget?.Hide();
                    streamingStatusWidget?.Hide();
                    if (peopleVideoCollapsedWindow != null)
                    {
                        peopleVideoCollapsedWindow.Close();
                    }
                    peopleVideoCollapsedWindow = new FRTCView.PeopleVideoCollapsedWindow();
                    peopleVideoCollapsedWindow.Show();
                    WindowInteropHelper windowInteropHelper = new WindowInteropHelper(peopleVideoCollapsedWindow);
                    Screen screen = Screen.FromHandle(windowInteropHelper.Handle);
                    if (this._meetingVideoWnd != null)
                    {
                        peopleVideoCollapsedWindow.Top = this._meetingVideoWnd.Top;
                    }
                    else
                    {
                        peopleVideoCollapsedWindow.Top = screen.Bounds.Bottom - peopleVideoCollapsedWindow.ActualHeight - 100;
                    }
                    var source = PresentationSource.FromVisual(peopleVideoCollapsedWindow);
                    if (source != null)
                    {
                        double scalerate = source.CompositionTarget.TransformToDevice.M11;
                        peopleVideoCollapsedWindow.Left = (screen.Bounds.Right - peopleVideoCollapsedWindow.ActualWidth) / scalerate;
                    }
                    peopleVideoCollapsedWindow.Topmost = true;
                }
                else
                {
                    peopleVideoCollapsedWindow?.Close();
                    peopleVideoCollapsedWindow = null;
                    recordingStatusWidget?.Show();
                    recordingStatusWidget?.Activate();
                    streamingStatusWidget?.Show();
                    streamingStatusWidget?.Activate();
                    AdjustStreamingWidget();
                    AdjustRecordingWidget();
                }
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }
        }

        WindowState _stateBeforeFullScreen = WindowState.Normal;
        public void HandleFullScreen()
        {
            LogTool.LogHelper.DebugMethodEnter();
            if (m_meetingWndThread == null)
                return;
            FRTCSDK.frtc_full_screen_switch(!_isFullScreen);
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (!_isFullScreen)
                {
                    _stateBeforeFullScreen = _meetingVideoWnd.WindowState;
                    _meetingVideoWnd.WindowStyle = System.Windows.WindowStyle.None;
                    _meetingVideoWnd.WindowState = System.Windows.WindowState.Normal;
                    _meetingVideoWnd.ResizeMode = System.Windows.ResizeMode.NoResize;
                    _meetingVideoWnd.WindowState = WindowState.Maximized;
                }
                else
                {
                    _meetingVideoWnd.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                    _meetingVideoWnd.ResizeMode = System.Windows.ResizeMode.CanResize;
                    _meetingVideoWnd.WindowState = _stateBeforeFullScreen;
                }

                IsFullScreen = !_isFullScreen;

                _meetingVideoWnd.adjustToolTips();
                _meetingVideoWnd.adjustStateMsg();
                _meetingVideoWnd.adjustToobarMsg(_isFullScreen);
                AdjustMeetingMsgWnd();
                _meetingVideoWnd.adjustRecordingStatusWidget();
                _meetingVideoWnd.adjustStreamingStatusWidget();
            });
            LogTool.LogHelper.DebugMethodExit();
        }

        public void AdjustMeetingMsgWnd()
        {
            lock (m_MeetingMsgLockObj)
            {
                _meetingVideoWnd?.adjustGloablMsg();
            }
        }

        private void OnFullScreen()
        {
            if (!_shareContentEnable)
            {
                return;
            }

            if (!_isFullScreen && _shareContentEnable)
            {
                HandleFullScreen();
            }
        }

        public void DBCLKVideo()
        {
            OnFullScreen();
        }

        public void LBtnCLKVideo()
        {
            if (_isShowToolBar)
            {
                IsShowToolBar = false;
                FRTCSDK.frtc_name_card_switch(false);
                OnToolbarHide();
            }
            else
            {
                IsShowToolBar = true;
                FRTCSDK.frtc_name_card_switch(true);
                OnToolbarShow();
            }
            if (SendMeetingMsgEnabled)
            {
                AdjustMeetingMsgWnd();
            }
        }

        public void PressEsc()
        {
            if (_isFullScreen)
            {
                HandleFullScreen();
            }
        }

        public void OnChangeNetintensity(SDKIntensityType type)
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (IsEncrypted)
                {
                    if (SDKIntensityType.SIGNALINTENSITYLOW == type)
                    {
                        IntensityImageIndex = 0;
                    }
                    else if (SDKIntensityType.SIGNALINTENSITYMEDIAN == type)
                    {
                        IntensityImageIndex = 1;
                    }
                    else
                    {
                        IntensityImageIndex = 2;
                    }
                }
                else
                {
                    if (SDKIntensityType.SIGNALINTENSITYLOW == type)
                    {
                        IntensityImageIndex = 3;
                    }
                    else if (SDKIntensityType.SIGNALINTENSITYMEDIAN == type)
                    {
                        IntensityImageIndex = 4;
                    }
                    else
                    {
                        IntensityImageIndex = 5;
                    }
                }
            });
        }

        public void OnToolbarShow()
        {
            OnToolbarHide();
            if (ToolbarShowTimer == null)
            {
                ToolbarShowTimer = new DispatcherTimer(
                 TimeSpan.FromSeconds(10), DispatcherPriority.Normal,
                 new EventHandler((o, ev) =>
                 {
                     try
                     {
                         IsShowToolBar = false;
                         FRTCSDK.frtc_name_card_switch(false);
                         OnToolbarHide();
                     }
                     catch (Exception ex)
                     {

                     }
                 }), Dispatcher.FromThread(m_meetingWndThread));
            }

            ToolbarShowTimer.Start();
        }

        public void OnToolbarHide()
        {
            if (ToolbarShowTimer != null)
            {
                ToolbarShowTimer.Stop();
                ToolbarShowTimer = null;
            }
        }

        private void OnJoinMeeting(JoinFRTCMeetingMsg msg)
        {
            LogHelper.DebugMethodEnter();
            IsMeetingOwner = msg.isSelfOwnedMeeting;
            _meeting_id = msg.confAlias;
            DisplayName = msg.displayName;
            _callRate = msg.callRate;
            CameraMuted = msg.preMuteCamera;
            MicMuted = msg.preMuteMic;
            IsShowToolBar = true;
            IsFullScreen = false;
            _IntensityImageIndex = 2;
            FRTCSDK.frtc_name_card_switch(true);
            ShowMeetingMsgWnd = false;
            _shareContentEnable = true;
            _IsShareAudio = false;
            _rosterNum = "1";
            IsSmallWnd = false;
            _isShowMoreBtn = true;

            if (ShowTipsTimer != null)
            {
                ShowTipsTimer.Stop();
            }
            ShowTipsTimer = null;
            _IsShowTips = false;
            _TipsContent = "";

            SettingViewModel setting = SimpleIoc.Default.GetInstance<SettingViewModel>();
            if (_callRate > 0 && _callRate <= 64)
            {
                _IsMonitorList = false;
                _IsShowCamralBtn = false;
            }
            else
            {
                _IsMonitorList = true;
                _IsShowCamralBtn = true;
            }
            IsAudioOnly = msg.isVoiceOnlyMeeting;
            if (msg.isPlainTextURLJoin && !string.IsNullOrEmpty(msg.serverAddress))
            {
                m_callManager.JoinMeetingPlainTextURL(msg.serverAddress, _meeting_id, DisplayName, _callRate, CameraMuted, MicMuted, msg.userToken, msg.passCode);
            }
            else
            {
                m_callManager.JoinMeeting(_meeting_id, DisplayName, _callRate, CameraMuted, MicMuted, msg.userToken, msg.passCode);
            }
            SelfOwnedMeeting = msg.isSelfOwnedMeeting;
            IsMeetingOwner = SelfOwnedMeeting;

            IsOperatorRole = false;
        }

        public void OnWindowLoad()
        {
            if (m_meetingWndThread != null)
            {
                _startTimer = DateTime.Now;

                UpdateTitleTimer = new DispatcherTimer(
                        TimeSpan.FromSeconds(1), DispatcherPriority.Normal,
                        new EventHandler((s, a) =>
                        {
                            try
                            {
                                DateTime endtime = DateTime.Now;
                                TimeSpan span = endtime - _startTimer;
                                string interval = span.ToString(@"hh\:mm\:ss");

                                this.MainTitle = interval;
                            }
                            catch (Exception ex)
                            {

                            }
                        }), Dispatcher.FromThread(m_meetingWndThread));
                UpdateTitleTimer.Start();

                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    SettingViewModel self = SimpleIoc.Default.GetInstance<SettingViewModel>();
                    self.InCall = true;

                    while (m_unHandledMeetingControlMsg.Count() != 0)
                    {
                        HandleMeetingControlMessage(m_unHandledMeetingControlMsg.Dequeue(), true);
                    }
                });
            }
        }

        private void OnCallStateChanged(FRTCCallStateChangeMessage msg)
        {
            if (msg.callState == FrtcCallState.DISCONNECTED)
            {

                if (m_meetingWndThread != null)
                {

                    Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                    {
                        recordingStatusWidget?.Close();
                        recordingStatusWidget = null;
                        streamingStatusWidget?.Close();
                        streamingStatusWidget = null;
                        shareStreamingInfoWindow?.Close();
                        shareStreamingInfoWindow = null;
                        UnmuteApplicationsList?.Clear();
                        UnmuteApplicationsList = null;
                        MeetingToolTips?.Close();
                        MeetingToolTips = null;
                        if (_meetingVideoWnd != null)
                        {
                            _meetingVideoWnd.Close();
                        }
                    });
                }

                _shareContentEnable = true;
                IsSendingContent = false;
                _contentPeopleWndCollapsed = false;
                _sharingToolBar = null;
                _SharingFrame = null;
                IsReceivingContent = false;
                IsRecording = false;
                IsStreaming = false;
                IsAudioOnly = false;
                NewUnmuteApplications = false;
                NewUnmuteApplicationsNotify = string.Empty;
                _unmuteApplicationTime = DateTime.MinValue;
                LastReconnectState = 0;

                if (msg.reason == FrtcCallReason.CALL_MEETING_END_ABNORMAL)
                {
                    DisplayName = string.Empty;
                    _tmpDisplayName = string.Empty;
                }


                if (UpdateTitleTimer != null)
                {
                    UpdateTitleTimer.Stop();
                }

                if (HideSharingBarTimer != null)
                {
                    HideSharingBarTimer.Stop();
                    HideSharingBarTimer = null;
                }

                SettingViewModel setting = SimpleIoc.Default.GetInstance<SettingViewModel>();
                setting.InCall = false;
                m_callManager.SetAPIBaseUrl(setting.ServerAddress);

                rosterListWindow = null;
                statisticsWnd = null;
                inviteInfoWindow = null;
            }
            else if (msg.callState == FrtcCallState.CONNECTED)
            {
                this.LocalVideoEnabled = true;
                this.MeetingID = msg.meetingId;
                this.MeetingName = msg.meetingName;
                this.MainTitle = " ";
                this.MeetingPassCode = CommonServiceLocator.ServiceLocator.Current.GetInstance<JoinMeetingViewModel>().FRTCMeetingPWD;

                this._selfUUID = FRTCUIUtils.GetFRTCDeviceUUID();

                if (msg.reason == FrtcCallReason.CALL_SUCCESS_BUT_UNSECURE)
                {
                    IsEncrypted = false;
                }
                else
                {
                    IsEncrypted = true;
                }
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
                self.IsEncrypted = IsEncrypted;
                self.EnableRecordingAndStreaming = SimpleIoc.Default.GetInstance<SettingViewModel>().EnableRecordingAndStreaming;

                m_meetingWndThread = new Thread(new ThreadStart(() =>
                {
                    _videoWndHost = new MeetingWindowHost(JoinMeeting);
                    _meetingVideoWnd = new View.MeetingVideoWindow(_videoWndHost);
                    FRTCUIUtils.SetMeetingWindow(_meetingVideoWnd);
                    _meetingVideoWnd.Closing += (s, e) =>
                    {
                        if (m_callManager.CurrentCallState == FrtcCallState.DISCONNECTED)
                        {
                            e.Cancel = false;
                            FRTCUIUtils.SetMeetingWindow(null);
                        }
                        else
                        {
                            e.Cancel = true;
                            PopHangUpDialog.Execute(null);
                        }
                    };

                    _meetingVideoWnd.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                    _meetingVideoWnd.Init();

                    _meetingVideoWnd.Show();

                    _meetingVideoWnd.Topmost = true;
                    _meetingVideoWnd.Topmost = false;

                    _meetingVideoWnd.Activate();

                    System.Windows.Threading.Dispatcher.Run();

                    if (_meetingVideoWnd != null)
                    {
                        _meetingVideoWnd.Close();
                    }
                }));
                m_meetingWndThread.SetApartmentState(ApartmentState.STA);

                m_meetingWndThread.Start();

                while (Dispatcher.FromThread(m_meetingWndThread) == null)
                {
                    Thread.Sleep(50);
                }

                updateStatisticsTimer = new DispatcherTimer(
                        TimeSpan.FromSeconds(2), DispatcherPriority.Normal,
                        new EventHandler((s, a) =>
                        {
                            try
                            {
                                IntPtr szStatistics = FRTCSDK.frtc_statistics_collect();
                                List<byte> buffer = new List<byte>();
                                byte b = Marshal.ReadByte(szStatistics);
                                while (b != 0)
                                {
                                    buffer.Add(b);
                                    szStatistics = IntPtr.Add(szStatistics, 1);
                                    b = Marshal.ReadByte(szStatistics);
                                }
                                string strStatistics = Encoding.UTF8.GetString(buffer.ToArray());
                                this.ParseStatisticsReport(strStatistics);
                            }
                            catch (Exception ex)
                            {

                            }
                        }), Dispatcher.FromThread(m_meetingWndThread));
            }
        }


        private static void OnContentSendingState(bool isSending)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.ShareContentEnable = !isSending;
            self.IsSendingContent = isSending;
            ResolutionTools.SetContentSharing(isSending);
            if (isSending)
            {
                self.ShowSharingToolBar(isSending);
            }
            else
            {
                self._meetingVideoWnd.Show();
                self.ShowContentPeopleCollapsedWindow(false);
                self.ShowSharingToolBar(isSending);
                self.CurMonInfo = null;
                self.CurSharingWndHwnd = IntPtr.Zero;
            }
        }

        public void TimerShowSharingBar()
        {
            if (_sharingToolBar == null || HideSharingBarTimer == null)
            {
                return;
            }
            HideSharingBarTimer.Stop();
            _sharingToolBar.StartShowOrHide(true);
            HideSharingBarTimer.Start();
        }

        public View.ContentSharingToolBar _sharingToolBar = null;
        public void ShowSharingToolBar(bool isSending)
        {
            if (m_meetingWndThread != null)
            {
                if (isSending)
                {
                    Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                    {
                        ShowSharingFrame();

                        _sharingToolBar = new View.ContentSharingToolBar();
                        _sharingToolBar.Show();
                        var source = PresentationSource.FromVisual(_sharingToolBar);
                        double scalerate = source.CompositionTarget.TransformToDevice.M11;

                        _sharingToolBar.StartShowOrHide(true);

                        if (HideSharingBarTimer == null)
                        {
                            HideSharingBarTimer = new DispatcherTimer(
                            TimeSpan.FromSeconds(2), DispatcherPriority.Normal,
                            new EventHandler((o, ev) =>
                            {
                                if (_sharingToolBar != null && !_sharingToolBar.IsMouseOver)
                                {
                                    _sharingToolBar.StartShowOrHide(false);
                                    if (HideSharingBarTimer != null)
                                    {
                                        HideSharingBarTimer.Stop();
                                    }
                                }
                            }), Dispatcher.FromThread(m_meetingWndThread));
                        }

                        HideSharingBarTimer.Start();
                        ShrinkWindow();
                    });
                }
                else
                {
                    if (HideSharingBarTimer != null)
                    {
                        HideSharingBarTimer.Stop();
                        HideSharingBarTimer = null;
                    }
                    Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                    {
                        if (_sharingToolBar != null)
                        {
                            _sharingToolBar.Hide();
                        }

                        HideSharingFrame();
                        RestoreWindow();

                    });
                }
            }
        }

        public View.FRTCMeetingStateBar MeetingStateBar = null;
        public View.FRTCMeetingToolBar MeetingToolBar = null;
        public View.MeetingToolTips MeetingToolTips = null;

        public void AdjustMeetingMsgPos()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                AdjustMeetingMsgWnd();
            });
        }

        public void AdjustRecordingWidget()
        {
            if (m_meetingWndThread == null || recordingStatusWidget == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                _meetingVideoWnd.adjustRecordingStatusWidget();
            });
        }
        public void AdjustStreamingWidget()
        {
            if (m_meetingWndThread == null || streamingStatusWidget == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                _meetingVideoWnd.adjustStreamingStatusWidget();
            });
        }

        private void HandleMeetingControlErrorMsg(HttpStatusCode StatusCode, string errorCode, string APIName, object ResultParam)
        {
            bool showGeneralError = false;
            if (StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (m_meetingWndThread != null && m_meetingWndThread.IsAlive)
                {
                    Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                    {
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED, Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED_TIP, Properties.Resources.FRTC_MEETING_SDKAPP_OK, _meetingVideoWnd);
                    }), null);
                }
            }
            else if (StatusCode == HttpStatusCode.NotFound)
            {
                if (m_meetingWndThread != null && m_meetingWndThread.IsAlive)
                {
                    Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                    {
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_OPERATION_FAILED, Properties.Resources.FRTC_MEETING_SDKAPP_CONNECTION_ERROR, Properties.Resources.FRTC_MEETING_SDKAPP_OK, _meetingVideoWnd);
                        if (APIName == "streaming")
                        {
                            IsStreaming = !_isStreaming;
                        }
                        else if (APIName == "recording")
                        {
                            IsRecording = !_isRecording;
                        }
                    }), null);
                }
            }
            else
            {
                switch (APIName)
                {
                    case "rename":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                if (_renameUserUUID != FRTCUIUtils.GetFRTCDeviceUUID())
                                {
                                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = string.Format(Properties.Resources.FRTC_MEETING_SDKAPP_RENAME_SUCCESS) });
                                }
                                else
                                {
                                    DisplayName = _tmpDisplayName;
                                }
                            }));
                        }
                        else
                        {
                            _tmpDisplayName = DisplayName;
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "mute_all":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                ShowTips(FrtcReminderType.ENABLE_MUTE_ALL);
                            }));
                        }
                        else
                        {
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "unmute_all":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                ShowTips(FrtcReminderType.DISABLE_MUTE_ALL);
                                UnmuteApplicationsList?.Clear();
                                NewUnmuteApplications = false;
                                NewUnmuteApplicationsNotify = string.Empty;
                                _unmuteApplicationTime = DateTime.MinValue;
                            }));
                        }
                        else
                        {
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "set_lecturer":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_SET_LECTURER_SUCCESS });
                            }));
                        }
                        else
                        {
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "unset_lecturer":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_LECTURER_SUCCESS });
                            }));
                        }
                        else
                        {
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "kickout":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_REMOVE_FROM_MEETING_SUCCESS });
                            }));
                        }
                        else
                        {
                            showGeneralError = true;
                        }
                        break;
                    case "start_text_overlay":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            ShowTips(FrtcReminderType.TEXT_OVERLAY_STARTED);
                        }
                        else
                        {
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "stop_text_overlay":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            ShowTips(FrtcReminderType.TEXT_OVERLAY_STOPPED);
                        }
                        else
                        {
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "recording":
                        if (StatusCode != HttpStatusCode.OK)
                        {
                            if (!HandleRESTAPIGeneralError(errorCode))
                            {
                                HandleRecordingError(errorCode);
                            }
                            if (ResultParam != null && ResultParam is bool)
                                IsRecording = !((bool)ResultParam);
                        }
                        break;
                    case "streaming":
                        if (StatusCode != HttpStatusCode.OK)
                        {
                            if (!HandleRESTAPIGeneralError(errorCode))
                            {
                                HandleStreamingError(errorCode);
                            }
                            if (ResultParam != null && ResultParam is bool)
                                IsStreaming = !((bool)ResultParam);
                        }
                        break;
                    case "apply_for_unmute":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                _unmuteApplicationTime = DateTime.Now;
                                ShowTips(FrtcReminderType.UNMUTE_APPLIED);
                            }));
                        }
                        else
                        {
                            _unmuteApplicationTime = DateTime.MinValue;
                            HandleRESTAPIGeneralError(errorCode);
                        }
                        break;
                    case "approve_unmute":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).Invoke(new Action(() =>
                            {
                                lock (m_unmuteApplicationsLockObj)
                                {
                                    if (UnmuteApplicationsList != null && UnmuteApplicationsList.Count > 0)
                                    {
                                        List<string> approvedApplications = new List<string>(ResultParam as IEnumerable<string>);
                                        foreach (string id in approvedApplications)
                                        {
                                            var ret = UnmuteApplicationsList.FirstOrDefault((r) => { return r.UUID == id; });
                                            if (ret != null)
                                            {
                                                UnmuteApplicationsList.Remove(ret);
                                            }
                                        }
                                    }
                                }
                            }));
                        }
                        else
                            HandleRESTAPIGeneralError(errorCode);
                        break;
                    case "pin_for_meeting":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            ShowTips(FrtcReminderType.PIN_VIDEO_SUCCESS);
                        }
                        else
                            HandleRESTAPIGeneralError(errorCode);
                        break;
                    case "unpin_for_meeting":
                        if (StatusCode == HttpStatusCode.OK)
                        {
                            Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                            {
                                List<RosterItem> ret = null;
                                LogHelper.Debug("Enter lock m_rosterListLockObj");
                                lock (m_rosterListLockObj)
                                {
                                    ret = RosterList.FindAll((r) => r.IsPinned);
                                }
                                if (ret != null && ret.Count > 0)
                                {
                                    ret.ForEach(p => p.IsPinned = false);
                                }
                            }));
                            ShowTips(FrtcReminderType.UNPIN_VIDEO_SUCCESS);
                        }
                        else
                            HandleRESTAPIGeneralError(errorCode);
                        break;
                    default:
                        break;
                }
            }
            if (showGeneralError)
            {
                ShowTips(FrtcReminderType.MEETING_CONTROL_OPERATION_GENERAL_ERROR);
            }
        }

        private bool HandleRESTAPIGeneralError(string errorCode)
        {
            bool handled = true;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                switch (errorCode)
                {
                    case "0x10000000":
                        ShowTips(FrtcReminderType.MEETING_CONTROL_OPERATION_GENERAL_ERROR);
                        break;
                    case "0x10000001":
                        ShowTips(FrtcReminderType.START_RECORDING_PARAMS_ERROR);
                        break;
                    case "0x10000002":
                        ShowTips(FrtcReminderType.VIDEO_PINNED);
                        break;
                    case "0x10000003":
                        ShowTips(FrtcReminderType.MEETING_CONTROL_OPERATION_GENERAL_ERROR);
                        break;
                    case "0x10000004":
                        ShowTips(FrtcReminderType.MEETING_CONTROL_OPERATION_GENERAL_ERROR);
                        break;
                    case "0x10000005":
                        ShowTips(FrtcReminderType.START_RECORDING_NO_SERVICE);
                        break;
                    case "0x10000006":
                        ShowTips(FrtcReminderType.START_RECORDING_MULTIPLY);
                        break;
                    case "0x10000007":
                        ShowTips(FrtcReminderType.START_RECORDING_NO_SERVICE);
                        break;
                    case "0x10002001":
                        ShowTips(FrtcReminderType.MEETING_CONTROL_OPERATION_RECORDING_ERROR);
                        break;
                    default:
                        handled = false;
                        break;
                }
            });
            return handled;
        }


        private void HandleRecordingError(string errorCode)
        {
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                switch (errorCode)
                {
                    case "0x0000f000":
                        ShowTips(FrtcReminderType.START_RECORDING_ERROR);
                        break;
                    case "0x0000f001":
                        ShowTips(FrtcReminderType.START_RECORDING_PARAMS_ERROR);
                        break;
                    case "0x0000f002":
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_RECORDING_ERROR_TITLE, Properties.Resources.FRTC_MEETING_RECORDING_NO_LICENSE);
                        break;
                    case "0x0000f003":
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_RECORDING_ERROR_TITLE, Properties.Resources.FRTC_MEETING_RECORDING_LICENSE_LIMITATION_ERROR);
                        break;
                    case "0x0000f004":
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_RECORDING_ERROR_TITLE, Properties.Resources.FRTC_MEETING_RECORDING_INSUFFICIENT_RESOURCE);
                        break;
                    case "0x0000f005":
                        ShowTips(FrtcReminderType.START_RECORDING_NO_SERVICE);
                        break;
                    case "0x0000f006":
                        ShowTips(FrtcReminderType.START_RECORDING_MULTIPLY);
                        break;
                    case "0x0000f007":
                        ShowTips(FrtcReminderType.START_RECORDING_NO_SERVICE);
                        break;
                    case "0x0000f100":
                        ShowTips(FrtcReminderType.STOP_RECORDING_ERROR);
                        break;
                    case "0x0000f101":
                        ShowTips(FrtcReminderType.STOP_RECORDING_PARAMS_ERROR);
                        break;
                    default:
                        break;
                }
            });
        }

        private void HandleStreamingError(string errorCode)
        {
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                switch (errorCode)
                {
                    case "0x00010000":
                        ShowTips(FrtcReminderType.START_STREAMING_ERROR);
                        break;
                    case "0x00010001":
                        ShowTips(FrtcReminderType.START_STREAMING_PARAMS_ERROR);
                        break;
                    case "0x00010002":
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_STREAMING_ERROR_TITLE, Properties.Resources.FRTC_MEETING_STREAMING_NO_LICENSE);
                        break;
                    case "0x00010003":
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_STREAMING_ERROR_TITLE, Properties.Resources.FRTC_MEETING_STREAMING_LICENSE_LIMITATION_ERROR);
                        break;
                    case "0x00010004":
                        FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_STREAMING_ERROR_TITLE, Properties.Resources.FRTC_MEETING_STREAMING_INSUFFICIENT_RESOURCE);
                        break;
                    case "0x00010005":
                        ShowTips(FrtcReminderType.START_STREAMING_NO_SERVICE);
                        break;
                    case "0x00010006":
                        ShowTips(FrtcReminderType.START_STREAMING_MULTIPLY);
                        break;
                    case "0x00010008":
                        ShowTips(FrtcReminderType.START_STREAMING_NO_SERVICE);
                        break;
                    case "0x00011000":
                        ShowTips(FrtcReminderType.STOP_STREAMING_ERROR);
                        break;
                    case "0x00011001":
                        ShowTips(FrtcReminderType.STOP_STREAMING_PARAMS_ERROR);
                        break;
                    default:
                        break;
                }
            });
        }

        public void ShowToolbar()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (MeetingToolBar != null)
                {
                    MeetingToolBar.Close();
                    MeetingToolBar = null;
                }

                MeetingToolBar = new View.FRTCMeetingToolBar();
                MeetingToolBar.VerticalOffset = 0;
                MeetingToolBar.dWidth = this._meetingVideoWnd.ActualWidth - (2 * (SystemParameters.BorderWidth + SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth));
                MeetingToolBar.Owner = _meetingVideoWnd;

                Win32API.SetParent(new WindowInteropHelper(MeetingToolBar).Handle, FRTCUIUtils.MeetingWindowHandle);

                _meetingVideoWnd.adjustToobarMsg();
                MeetingToolBar.Show();
                MeetingToolBar.Activate();
                _meetingVideoWnd.adjustToobarMsg();

                //3.1.0 always show
                OnToolbarShow();

                _meetingVideoWnd.adjustToobarMsg();

            });
        }

        public void HideToolTips()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (MeetingToolBar != null)
                {
                    MeetingToolBar.Close();
                    MeetingToolBar = null;
                }
            });
        }

        public void HideToolbar()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (MeetingToolTips != null)
                {
                    MeetingToolTips.Close();
                    MeetingToolTips = null;
                }
            });
        }

        public void ShowToolTips()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (MeetingToolTips != null)
                {
                    MeetingToolTips.Close();
                    MeetingToolTips = null;
                }

                MeetingToolTips = new View.MeetingToolTips();
                MeetingToolTips.Owner = _meetingVideoWnd;
                _meetingVideoWnd.adjustToolTips();
                MeetingToolTips.Show();
                MeetingToolTips.Activate();
                _meetingVideoWnd.adjustToolTips();

            });
        }

        public void ShowMeetingStateBar()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (MeetingStateBar != null)
                {
                    MeetingStateBar.Close();
                    MeetingStateBar = null;
                }

                MeetingStateBar = new View.FRTCMeetingStateBar();
                MeetingStateBar.Owner = _meetingVideoWnd;
                _meetingVideoWnd.adjustStateMsg();
                MeetingStateBar.Show();
                MeetingStateBar.Activate();
                _meetingVideoWnd.adjustStateMsg();

            });
        }

        public void HideMeetingSateBar()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                if (MeetingStateBar != null)
                {
                    MeetingStateBar.Close();
                    MeetingStateBar = null;
                }
            });
        }

        public View.MeetingMsgWnd meetingMsgWnd = null;

        public void ShowMeetingMsg()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                lock (m_MeetingMsgLockObj)
                {
                    if (meetingMsgWnd != null)
                    {
                        meetingMsgWnd.Close();
                        meetingMsgWnd = null;
                    }

                    meetingMsgWnd = new View.MeetingMsgWnd();
                    meetingMsgWnd.Closed += (s, e) => { this.meetingMsgWnd = null; this.SendMeetingMsgEnabled = false; };
                    meetingMsgWnd.Owner = _meetingVideoWnd;
                    Win32API.SetParent(new WindowInteropHelper(meetingMsgWnd).Handle, FRTCUIUtils.MeetingWindowHandle);
                    meetingMsgWnd.Show();
                    meetingMsgWnd.Activate();
                    AdjustMeetingMsgWnd();
                    SendMeetingMsgEnabled = true;
                }
            });
        }

        public void HideMeetingMSg()
        {
            if (m_meetingWndThread == null)
                return;
            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                lock (m_MeetingMsgLockObj)
                {
                    if (meetingMsgWnd != null)
                    {
                        meetingMsgWnd.Close();
                        meetingMsgWnd = null;
                    }
                }
            });
        }

        public void ShrinkWindow()
        {
            _meetingVideoWnd.ShrinkWindow(_isFullScreen);
        }

        public void RestoreWindow()
        {
            _meetingVideoWnd.RestoreWindow();
        }

        private bool _IsShareAudio = false;
        public bool IsShareAudio
        {
            get
            {
                return _IsShareAudio;
            }
            set
            {
                if (_IsShareAudio != value)
                {

                    _IsShareAudio = value;
                    RaisePropertyChanged();
                }

                if (_IsShareAudio)
                {
                    ContentAudioIndex = 1;
                }
                else
                {
                    ContentAudioIndex = 0;
                }
            }
        }

        private bool _isAudioOnly = false;
        public bool IsAudioOnly
        {
            get { return _isAudioOnly; }
            set
            {
                _isAudioOnly = value;
                RaisePropertyChanged("IsAudioOnly");
            }
        }

        private bool _localOnly = true;
        public bool LocalOnly
        {
            get => _localOnly;
            set { _localOnly = value; RaisePropertyChanged("LocalOnly"); }
        }


        private bool _isFullScreen = false;
        public bool IsFullScreen
        {
            get { return _isFullScreen; }
            set { _isFullScreen = value; RaisePropertyChanged("IsFullScreen"); }
        }

        private FrtcLayout _currentLayout = FrtcLayout.LAYOUT_AUTO;
        public FrtcLayout CurrentLayout
        {
            get { return _currentLayout; }
            set { _currentLayout = value; RaisePropertyChanged("CurrentLayout"); IsGalleryView = _currentLayout == FrtcLayout.LAYOUT_GALLERY; }
        }

        private bool _isGalleryView = false;
        public bool IsGalleryView
        {
            get => _isGalleryView;
            set { _isGalleryView = value; RaisePropertyChanged("IsGalleryView"); }
        }

        private System.Windows.Forms.Screen _frtcMeetingWndScreen = null;
        public System.Windows.Forms.Screen FRTCMeetingWndScreen
        {
            get
            {
                var TargethwndSource = (PresentationSource.FromVisual(_meetingVideoWnd)) as HwndSource;
                if (TargethwndSource == null)
                {
                    return null;
                }
                var hwnd = TargethwndSource.Handle;
                Screen curScreen = Screen.FromHandle(hwnd);

                _frtcMeetingWndScreen = curScreen;
                return _frtcMeetingWndScreen;
            }
            set
            {
                if (_frtcMeetingWndScreen != value)
                {
                    _frtcMeetingWndScreen = value;
                }
            }
        }

        public View.SharingFrame _SharingFrame = null;
        public void ShowSharingFrame()
        {
            if (_SharingFrame == null)
            {
                _SharingFrame = new View.SharingFrame();
            }
            _SharingFrame.Show();
            _SharingFrame.UpdateFrame();
        }

        public void adjustSharingFrame()
        {
            if (_SharingFrame == null)
            {
                return;
            }

            _SharingFrame.UpdateFrame();
        }

        public void HideSharingFrame()
        {
            if (_SharingFrame != null)
            {
                _SharingFrame.Hide();

            }
        }

        public List<RosterItem> Userlist;

        private static void OnMeetingControlMessage(IntPtr message)
        {
            string msgStr = FRTCUIUtils.StringFromNativeUtf8(message);
            if (string.IsNullOrEmpty(msgStr))
            {
                return;
            }

            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.HandleMeetingControlMessage(msgStr);
        }

        private void HandleMeetingControlMessage(string msgStr, bool isUnhandledQueueMsg = false)
        {
            var jObj = JObject.Parse(msgStr);
            var token = jObj.GetValue("meeting_controll_msg");
            if (token != null)
            {
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
                string msgType = token.Value<string>("message_type");

                if (msgType == "on_mic_test")
                {
                    token = token["message_context"];
                    if (token != null)
                    {
                        string peakValue = token["peak_value"].ToString();
                        MessengerInstance.Send(new NotificationMessage<string>(peakValue, "on_mic_test"));
                        float fPeakValue = 0.0f;
                        if (float.TryParse(peakValue, out fPeakValue))
                        {
                            MicMeterLevel = (int)(long)Math.Round((double)fPeakValue * 200);
                        }
                    }
                }
                else
                {
                    LogTool.LogHelper.Debug("Receive meeting control msg {0}", msgType);
                    if (self.m_meetingWndThread != null && Dispatcher.FromThread(self.m_meetingWndThread) != null)
                    {
                        if (msgType == "on_audio_mute_changed")
                        {
                            token = token["message_context"];

                            bool mute = token.Value<bool>("muted");
                            if (mute)
                            {
                                self.MicMuted = mute;
                                self.AllowUnmute = token.Value<bool>("allowSelfUnmute");
                                LogTool.LogHelper.Debug("mic muted set allow self unmute to {0}", AllowUnmute);

                                self.ShowTips(FrtcReminderType.MICROPHONE_MUTED_BY_ADMIN);
                            }
                            else
                            {
                                self.AllowUnmute = token.Value<bool>("allowSelfUnmute");
                                LogTool.LogHelper.Debug("mic unmute set allow self unmute to {0}", AllowUnmute);

                                if (self.MicMuted && !isUnhandledQueueMsg)
                                {
                                    self.HandleServerAskUnmute();
                                }
                                _unmuteApplicationTime = DateTime.MinValue;
                            }
                        }
                        else if (msgType == "on_text_overlay_received")
                        {
                            token = token["message_context"];

                            MeetingMessageInfo info = new MeetingMessageInfo();

                            info.MsgDisplaySpeed = token.Value<string>("displaySpeed");
                            if (info.MsgDisplaySpeed == "static")
                            {
                                info.MsgDisplayRepetition = 0;
                            }
                            else
                            {
                                info.MsgDisplayRepetition = token.Value<int>("displayRepetition");
                            }

                            info.MsgColor = token.Value<string>("color");

                            string strC = token.Value<string>("text");
                            string strType = token.Value<string>("type");
                            string str = "";
                            if (strType == "global")
                            {
                                str = strC.Replace("\n", "").Replace("\t", "").Replace("\r", "");
                            }
                            else
                            {
                                str = strC;
                            }
                            info.MsgContent = str;

                            info.MsgFont = token.Value<string>("font");
                            info.MsgBackgroundTransparency = token.Value<int>("backgroundTransparency");
                            info.MsgFontSize = token.Value<int>("fontSize");
                            info.MsgVerticalPosition = token.Value<int>("verticalPosition");
                            info.EnabledMeetingMessage = token.Value<bool>("enabled");


                            self.OnReciveMeetingMsg(info, strType);
                        }
                        else if (msgType == "on_network_intensity_changed")
                        {
                            token = token["message_context"];
                            int intensity = token.Value<int>("intensity");
                            SDKIntensityType type = (SDKIntensityType)intensity;

                            self.OnChangeNetintensity(type);

                        }
                        else if (msgType == "on_participantsnum_changed")
                        {
                            token = token["message_context"];
                            int num = token.Value<int>("participant_num");
                            self.OnParticipant(num);
                        }
                        else if (msgType == "on_participantslist_changed")
                        {
                            LogTool.LogHelper.Debug(msgStr);
                            token = token["message_context"];
                            if (token != null)
                            {
                                var jToken = token["participant_list"];
                                if (jToken != null)
                                {
                                    try
                                    {
                                        bool bFullList = jToken.Value<bool>("fullList");
                                        var rosters = jToken["rosters"];
                                        if (rosters != null)
                                        {
                                            string strRoster = rosters.ToString();
                                            JArray jlist = JArray.Parse(strRoster);
                                            self.HandleParticipantListChange(jlist, bFullList);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogTool.LogHelper.Exception(ex);
                                    }
                                }
                            }
                        }
                        else if (msgType == "on_camera_state_changed")
                        {
                            LogTool.LogHelper.Debug(msgStr);
                            token = token["message_context"];
                            if (token != null)
                            {
                                bool isCamereOn = token.Value<bool>("camera_state");
                                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                                {
                                    CameraMuted = !isCamereOn;
                                }));
                            }
                        }
                        else if (msgType == "on_audio_device_reset")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                string deviceType = token.Value<string>("device_type");
                                string deviceId = token.Value<string>("device_id");
                                if (!string.IsNullOrEmpty(deviceType) && !string.IsNullOrEmpty(deviceId))
                                    self.MessengerInstance.Send(new MediaDeviceResetMessage(deviceType, deviceId));
                            }
                        }
                        else if (msgType == "on_camera_device_reset")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                string deviceId = token.Value<string>("device_id");
                                self.MessengerInstance.Send(new MediaDeviceResetMessage("camera", deviceId));
                            }
                        }
                        else if (msgType == "on_send_content__audio_changed")
                        {
                            //FRTC 暂时不用这个消息
                            //token = token["message_context"];
                            //if (token != null)
                            //{
                            //    bool bEnable = token.Value<bool>("sendContentAudio");
                            //    self.OnEnableContentAudio(bEnable);
                            //}
                        }
                        else if (msgType == "on_meeting_info")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                try
                                {
                                    self.HandleMeetingInfo(token);
                                    if(FRTCPopupViewManager.CurrentPopup != null && FRTCPopupViewManager.CurrentPopup is FRTCReconnectingWindow)
                                    {
                                        FRTCPopupViewManager.CurrentPopup.Close();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogTool.LogHelper.Exception(ex);
                                }
                            }
                        }
                        else if (msgType == "on_content_receiving_state")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                self.IsReceivingContent = token.Value<bool>("content_receiving");
                            }
                        }
                        else if (msgType == "reconnect_state")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                int state = token.Value<int>("state");
                                self.LastReconnectState = state;
                                self.HandleCallReconnect(state);
                            }
                        }
                        else if (msgType == "on_lecturers_changed")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                var jToken = token["lectures"];
                                if (jToken != null)
                                {
                                    try
                                    {
                                        string strLecturers = jToken.ToString();
                                        JArray jlist = JArray.Parse(strLecturers);
                                        HandleLecturerChange(jlist.Select<JToken, string>(t => { return t.ToString(); }));
                                    }
                                    catch (Exception lectureEx)
                                    {
                                        LogTool.LogHelper.Exception(lectureEx);
                                    }
                                }
                            }
                        }
                        else if (msgType == "on_recording_streaming_status_changed")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                string recordingStatus = token.Value<string>("recording_status");
                                string liveStatus = token.Value<string>("live_status");
                                bool recording = recordingStatus == "STARTED";
                                bool streaming = liveStatus == "STARTED";
                                LogTool.LogHelper.Debug("on_recording_streaming_status_changed recording status is {0}, streaming status is {1}", recordingStatus, liveStatus);
                                LogTool.LogHelper.Debug("on_recording_streaming_status_changed last recording status is {0}, last streaming status is {1}", self.IsRecording, self.IsStreaming);
                                string streamingUrl = string.Empty;
                                string streamingPwd = string.Empty;
                                if (streaming)
                                {
                                    streamingUrl = token.Value<string>("live_url");
                                    streamingPwd = token.Value<string>("live_pwd");
                                }
                                HandleRecordingStreamingStatusChange(recording, streaming, streamingUrl, streamingPwd);
                            }
                        }
                        else if (msgType == "on_receive_unmute_applications")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                var jToken = token["partiList"];
                                if (jToken != null && jToken.Type == JTokenType.Array)
                                {
                                    JArray jlist = jToken as JArray;
                                    if (jlist.Count > 0)
                                    {
                                        int msgThreadID = Dispatcher.CurrentDispatcher.Thread.ManagedThreadId;
                                        LogTool.LogHelper.Debug("on_receive_unmute_applications message thread id is {0}", msgThreadID);
                                        Dispatcher.FromThread(m_meetingWndThread).BeginInvoke((Action)delegate ()
                                        {
                                            int videoWndThreadID = Dispatcher.CurrentDispatcher.Thread.ManagedThreadId;
                                            LogTool.LogHelper.Debug("on_receive_unmute_applications message thread id is {0}", videoWndThreadID);
                                            HandleUnmuteApplications(jlist.Select(x => x["id"].ToString()));
                                        });
                                    }
                                }
                            }
                        }
                        else if (msgType == "on_receive_unmute_approved")
                        {
                            Dispatcher.FromThread(m_meetingWndThread).Invoke((Action)delegate ()
                            {
                                if (MicMuted)
                                {
                                    if (FRTCView.FRTCMessageBox.ShowConfirmMessage(
                                        Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_APPROVED,
                                        Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_APPROVED_MSG,
                                        Properties.Resources.FRTC_MEETING_SDKAPP_UNMUTE,
                                        Properties.Resources.FRTC_MEETING_SDKAPP_ASK_UNMUTE_STAYMUTE,
                                        false, _meetingVideoWnd))
                                    {
                                        LogHelper.Debug("Server approved unmute, do unmute");
                                        MuteLocalAudio(false);
                                        MicMuted = false;
                                    }
                                }
                                _unmuteApplicationTime = DateTime.MinValue;
                            });
                        }
                        else if (msgType == "on_participant_pin_state_changed")
                        {
                            token = token["message_context"];
                            if (token != null)
                            {
                                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke((Action)delegate ()
                                {
                                    LogHelper.Debug("Enter lock m_rosterListLockObj");
                                    lock (m_rosterListLockObj)
                                    {
                                        try
                                        {
                                            if (RosterList != null)
                                            {
                                                string uuid = token["uuid"].ToString();
                                                PinnedUUID = uuid;
                                                RosterList.ForEach((p) =>
                                                {
                                                    if (p.IsPinned && p.UUID != uuid)
                                                    {
                                                        p.IsPinned = false;
                                                        if (p.UUID == _selfUUID)
                                                        {
                                                            ShowTips(FrtcReminderType.VIDEO_UNPINNED);
                                                        }
                                                    }
                                                    else if (p.UUID == uuid && !p.IsPinned)
                                                    {
                                                        p.IsPinned = true;
                                                        if (p.UUID == _selfUUID)
                                                        {
                                                            ShowTips(FrtcReminderType.VIDEO_PINNED);
                                                        }
                                                    }
                                                });
                                                if (RosterList.Count > 1)
                                                {
                                                    this.RosterList?.Sort(1, (RosterList.Count - 1), new LecturerComparer());
                                                    RosterList = new List<RosterItem>(RosterList);
                                                }
                                            }
                                        }
                                        catch(Exception ex)
                                        {
                                            LogHelper.Exception(ex);
                                        }
                                    }
                                });
                            }
                        }
                        else if (msgType == "on_network_statue_changed")
                        {

                        }
                    }
                    else
                    {
                        self.m_unHandledMeetingControlMsg.Enqueue(msgStr);
                    }
                }
            }
        }

        void HandleMeetingInfo(JToken token)
        {
            string owner_id = token.Value<string>("meeting_owner_id");
            MeetingOwnerId = owner_id;
            bool isGuestCall = token.Value<bool>("is_guest");
            IsGuestMeeting = isGuestCall;
            FRTCUserManager signInMgr = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCUserManager>();
            IsOperatorRole = IsGuestMeeting ? false : (signInMgr.IsUserSignIn && (signInMgr.Role == UserRole.MeetingOperator || signInMgr.Role == UserRole.SystemAdmin));
            if (!isGuestCall && signInMgr.IsUserSignIn && signInMgr.UserData.user_id == owner_id)
            {
                SelfOwnedMeeting = true;
                IsMeetingOwner = true;
            }
            else
            {
                SelfOwnedMeeting = false;
                IsMeetingOwner = false;
            }
            MeetingOwnerName = token.Value<string>("meeting_owner_name");
            string meeting_url = token.Value<string>("meeting_url");
            string groupMeetingUrl = token.Value<string>("group_meeting_url");
            if (!string.IsNullOrEmpty(groupMeetingUrl))
                meeting_url = groupMeetingUrl;

            if (isGuestCall && !string.IsNullOrEmpty(meeting_url))
            {
                try
                {
                    Uri meetingUri = new Uri(meeting_url);
                    m_callManager.SetAPIBaseUrl(meetingUri.Host + ":" + meetingUri.Port.ToString());
                }
                catch (Exception ex)
                {
                    LogTool.LogHelper.Exception(ex);
                }
            }
            else
                m_callManager.SetAPIBaseUrl(CommonServiceLocator.ServiceLocator.Current.GetInstance<SettingViewModel>().ServerAddress);

            string meeting_displayname = token.Value<string>("display_name");
            this.DisplayName = meeting_displayname;
            string meeting_id = token.Value<string>("meeting_id");
            this._meeting_id = meeting_id;
            string meeting_name = token.Value<string>("meeting_name");
            long startTime = token.Value<long>("schedule_start_time");
            long endTime = token.Value<long>("schedule_end_time");
            JoinMeetingViewModel vm = CommonServiceLocator.ServiceLocator.Current.GetInstance<JoinMeetingViewModel>();
            string strStartTime = string.Empty;
            if (startTime > 0)
                strStartTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(startTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            string strEndTime = string.Empty;
            if (endTime > 0)
                strEndTime = UIHelper.GetUTCDateTimeFromUTCTimestamp(endTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            this.MeetingInviteString = UIHelper.GetMeetingInvitationText(isGuestCall ? string.Empty : meeting_displayname, meeting_name, strStartTime, meeting_id, vm.FRTCMeetingPWD, false, strEndTime, meeting_url);
            if (!isGuestCall && signInMgr.IsUserSignIn && signInMgr.UserData != null && LastReconnectState != 1)
                AddHistoryRecord(meeting_name, meeting_id, meeting_displayname, vm.FRTCMeetingPWD, MeetingOwnerName);

            if (IsAudioOnly)
            {
                ShowTips(FrtcReminderType.VIOCE_ONLY_MEETING_NOTIFICATION);
            }
        }

        void HandleParticipantListChange(JArray jlist, bool bFullList)
        {
            if (jlist.Count == 1)
            {
                LocalOnly = true;
            }
            else if (jlist.Count > 1)
            {
                LocalOnly = false;
            }

            List<RosterItem> RosterItem = new List<RosterItem>();
            for (int i = 0; i < jlist.Count; ++i)
            {
                JObject tempo = JObject.Parse(jlist[i].ToString());
                RosterItem item = new RosterItem();
                item.UUID = tempo["UUID"].ToString();
                if (tempo["muteAudio"] != null)
                {
                    item.MuteAudio = tempo["muteAudio"].ToString();
                }

                if (tempo["muteVideo"] != null)
                {
                    item.MuteVideo = tempo["muteVideo"].ToString();
                }

                item.Name = tempo["name"].ToString();

                if (_currentLecturerList.Contains(item.UUID))
                {
                    item.IsLecturer = true;
                }

                RosterItem.Add(item);
            }

            if (bFullList)
            {
                CreateList(RosterItem);
            }
            else
            {
                UpdateList(RosterItem);
            }

            if (Searching)
            {
                LogHelper.Debug("Enter lock m_rosterListLockObj");
                lock (m_rosterListLockObj)
                {
                    SearchUserResult = RosterList.FindAll((r) => { return r.Name.ToLower().Contains(_searchUserPattern.ToLower()); });
                }
                NoResult = !(SearchUserResult != null && SearchUserResult.Count() > 0);
            }
            if (_unmuteApplicationsList != null && _unmuteApplicationsList.Count > 0)
            {
                LogHelper.Debug("Enter lock m_rosterListLockObj");
                lock (m_unmuteApplicationsLockObj)
                {
                    List<RosterItem> toRemove = new List<RosterItem>();
                    lock (m_rosterListLockObj)
                    {
                        for (int i = _unmuteApplicationsList.Count - 1; i >= 0; --i)
                        {
                            var found = RosterList.Find(p => p.UUID == _unmuteApplicationsList[i].UUID);
                            if (found != null)
                            {
                                if (_unmuteApplicationsList[i].Name != found.Name)//renamed participant
                                {
                                    _unmuteApplicationsList[i].Name = found.Name;
                                }
                            }
                            else//exited participant
                            {
                                _unmuteApplicationsList.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        private void HandleLecturerChange(IEnumerable<string> LectureID)
        {
            if (LectureID.Count() == 0)
            {
                _currentLecturerList.Clear();
                HasLecture = false;
                LogHelper.Debug("Enter lock m_rosterListLockObj");
                lock (m_rosterListLockObj)
                {
                    this.RosterList?.ForEach(r =>
                    {
                        if (r.IsLecturer)
                        {
                            r.IsLecturer = false;
                            r.Remark = r.UUID == _selfUUID ? Properties.Resources.FRTC_MEETING_ROSTER_ME : string.Empty;
                        }
                    });
                }

                if (SelectedParticipantCopy != null)
                {
                    SelectedParticipantCopy.IsLecturer = false;
                    SelectedParticipantCopy.Remark = SelectedParticipantCopy.UUID == _selfUUID ? Properties.Resources.FRTC_MEETING_ROSTER_ME : string.Empty;
                }
            }
            else
            {
                _currentLecturerList.Clear();
                foreach (string lectureID in LectureID)
                {
                    _currentLecturerList.Add(lectureID);
                    Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                    {
                        LogHelper.Debug("Enter lock m_rosterListLockObj");
                        lock (m_rosterListLockObj)
                        {
                            this.RosterList?.ForEach(r =>
                            {
                                r.IsLecturer = r.UUID == lectureID;
                                if (r.IsLecturer)
                                {
                                    r.Remark = r.UUID == _selfUUID ? Properties.Resources.FRTC_MEETING_SDKAPP_LECTURE_LABEL_SELF
                                    : Properties.Resources.FRTC_MEETING_SDKAPP_LECTURE_LABEL;
                                }
                                else
                                {
                                    r.Remark = r.UUID == _selfUUID ? Properties.Resources.FRTC_MEETING_ROSTER_ME : string.Empty;
                                }
                            });
                            this.RosterList?.Sort(1, (RosterList.Count - 1), new LecturerComparer());
                            RosterList = new List<RosterItem>(RosterList);
                        }
                    });
                    if (SelectedParticipantCopy != null && SelectedParticipantCopy.UUID == lectureID)
                    {
                        SelectedParticipantCopy.IsLecturer = true;
                        SelectedParticipantCopy.Remark = SelectedParticipantCopy.UUID == _selfUUID ? Properties.Resources.FRTC_MEETING_SDKAPP_LECTURE_LABEL_SELF
                            : Properties.Resources.FRTC_MEETING_SDKAPP_LECTURE_LABEL;
                    }
                    if (lectureID == FRTCUIUtils.GetFRTCDeviceUUID())
                    {
                        Dispatcher.FromThread(m_meetingWndThread).BeginInvoke((Action)delegate ()
                        {
                            ShowTips(FrtcReminderType.YOU_ARE_LECTURER);
                        });
                    }
                }
                HasLecture = _currentLecturerList.Count > 0;
            }
        }

        private void HandleUnmuteApplications(IEnumerable<string> UserID)
        {
            lock (m_unmuteApplicationsLockObj)
            {
                if (UnmuteApplicationsList == null)
                    UnmuteApplicationsList = new ObservableCollection<RosterItem>();
                foreach (var id in UserID)
                {
                    RosterItem ret = null;
                    if (UnmuteApplicationsList != null && UnmuteApplicationsList.Count > 0)
                    {
                        ret = UnmuteApplicationsList.FirstOrDefault((r) => { return r != null && r.UUID == id; });
                    }
                    if (ret == null)
                    {
                        RosterItem found = RosterList.Find((r) => { return r != null && r.UUID == id; });
                        if (found != null)
                            UnmuteApplicationsList.Add(found);
                    }
                    else
                    {
                        UnmuteApplicationsList.Remove(ret);
                        UnmuteApplicationsList.Add(ret);
                    }
                }
                if (UnmuteApplicationsList.Count > 0)
                {
                    NewUnmuteApplications = true;
                    NewUnmuteApplicationsNotify = string.Format(Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_NOTIFY, UnmuteApplicationsList.Last().Name);
                    FRTCView.FRTCReminderToast.ShowUnmuteApplicationReminder(UnmuteApplicationsList.Last().Name);
                }
                else
                {
                    NewUnmuteApplications = false;
                    NewUnmuteApplicationsNotify = string.Empty;
                }
            }
        }

        private void HandleRecordingStreamingStatusChange(bool recording, bool streaming, string streamingUrl, string streamPwd)
        {
            lock (m_recordingStreamingLockObj)
            {
                if (m_meetingWndThread == null)
                {
                    LogTool.LogHelper.Warn("on_recording_streaming_status_changed not handled, video wnd thread is null");
                    return;
                }
                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                {
                    if (IsRecording != recording)
                    {
                        IsRecording = recording;
                        if (IsRecording)
                        {
                            LogTool.LogHelper.Debug("on_recording_streaming_status_changed try to show recording status widget");
                            if (recordingStatusWidget == null)
                            {
                                LogTool.LogHelper.Debug("on_recording_streaming_status_changed show a new recording status widget");
                                recordingStatusWidget = new FRTCView.StatusWidgetWindow();
                                recordingStatusWidget.Tag = "Recording";
                                recordingStatusWidget.Owner = _meetingVideoWnd;
                                Win32API.SetParent(new WindowInteropHelper(recordingStatusWidget).Handle, FRTCUIUtils.MeetingWindowHandle);
                                if (_meetingVideoWnd.WindowState == WindowState.Minimized)
                                {
                                    _meetingVideoWnd.WindowState = WindowState.Normal;
                                }
                                _meetingVideoWnd?.adjustRecordingStatusWidget();
                                if (!(IsSendingContent && _contentPeopleWndCollapsed))
                                {
                                    recordingStatusWidget.Show();
                                    recordingStatusWidget.Activate();
                                    _meetingVideoWnd?.adjustRecordingStatusWidget();
                                    if (streamingStatusWidget != null)
                                        _meetingVideoWnd?.adjustStreamingStatusWidget();
                                }

                                ShowTips((IsGuestMeeting || (!IsOperatorRole && !IsMeetingOwner)) ?
                                    FrtcReminderType.MEETING_RECORDING_START : FrtcReminderType.MEETING_RECORDING_START_OPERATOR);
                                FRTCView.FRTCReminderToast.ShowRecordingStartReminder(_isSendingContent);
                            }
                        }
                        else
                        {
                            LogTool.LogHelper.Debug("on_recording_streaming_status_changed close recording status widget");
                            recordingStatusWidget?.Close();
                            recordingStatusWidget = null;
                            if (streamingStatusWidget != null)
                                _meetingVideoWnd?.adjustStreamingStatusWidget();
                            FRTCView.FRTCReminderToast.CloseReminder();
                            ShowTips((IsGuestMeeting || (!IsOperatorRole && !IsMeetingOwner)) ?
                                    FrtcReminderType.MEETING_RECORDING_STOP : FrtcReminderType.MEETING_RECORDING_STOP_OPERATOR);
                        }
                    }
                    if (IsStreaming != streaming)
                    {
                        IsStreaming = streaming;
                        if (IsStreaming)
                        {
                            StreamingUrl = streamingUrl;
                            StreamingPassword = streamPwd;
                            StringBuilder sb = new StringBuilder();
                            sb.Append(Properties.Resources.FRTC_MEETING_SHARE_STREAMING_INFO_TEXT);
                            sb.Append(Environment.NewLine);
                            sb.Append(Environment.NewLine);
                            sb.Append(Properties.Resources.FRTC_MEETING_SHARE_STREAMING_INFO_TEXT2);
                            ShareStreamingText = string.Format(sb.ToString(), DisplayName, MeetingName);
                            if (string.IsNullOrEmpty(StreamingPassword))
                            {
                                StreamingPasswordInfo = string.Empty;
                            }
                            else
                                StreamingPasswordInfo = Properties.Resources.FRTC_MEETING_SHARE_STREAMING_INFO_PWD + StreamingPassword;

                            if (streamingStatusWidget == null)
                            {
                                streamingStatusWidget = new FRTCView.StatusWidgetWindow();
                                streamingStatusWidget.Tag = "Streaming";
                                streamingStatusWidget.Owner = _meetingVideoWnd;
                                Win32API.SetParent(new WindowInteropHelper(streamingStatusWidget).Handle, FRTCUIUtils.MeetingWindowHandle);
                                if (_meetingVideoWnd.WindowState == WindowState.Minimized)
                                {
                                    _meetingVideoWnd.WindowState = WindowState.Normal;
                                }
                                if (!(IsSendingContent && _contentPeopleWndCollapsed))
                                {
                                    streamingStatusWidget.Show();
                                    streamingStatusWidget.Activate();
                                    _meetingVideoWnd?.adjustStreamingStatusWidget();
                                }

                                ShowTips((IsGuestMeeting || (!IsOperatorRole && !IsMeetingOwner)) ?
                                    FrtcReminderType.MEETING_STREAMING_START : FrtcReminderType.MEETING_STREAMING_START_OPERATOR);
                            }
                        }
                        else
                        {
                            streamingStatusWidget?.Close();
                            streamingStatusWidget = null;
                            ShowTips((IsGuestMeeting || (!IsOperatorRole && !IsMeetingOwner)) ?
                                    FrtcReminderType.MEETING_STREAMING_STOP : FrtcReminderType.MEETING_STREAMING_STOP_OPERATOR);
                        }
                    }
                }));
            }
        }
        private void AddHistoryRecord(string meetingName, string meetingNumber, string displayName, string meetingPWD, string ownerName)
        {
            FRTCUserManager signInMgr = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCUserManager>();
            CommonServiceLocator.ServiceLocator.Current.GetInstance<MeetingHistoryManager>().AddHisotryRecord(new Model.DataObj.MeetingHistoryData()
            {
                uuid = Guid.NewGuid().ToString(),
                user_id = signInMgr.UserData.email,
                meeting_name = meetingName,
                meeting_number = meetingNumber,
                display_name = displayName,
                meeting_pwd = meetingPWD,
                begin_time = DateTime.Now.ToString("G"),
                owner_id = _selfOwnedMeeting ? signInMgr.UserData.user_id : string.Empty,
                owner_name = ownerName
            });
        }

        private void HandleCallReconnect(int state)
        {
            if (m_meetingWndThread != null)
            {
                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                {
                    LogHelper.Info("Handle reconnect state {0}", state);
                    switch (state)
                    {
                        case 0: // 	RECONNECT_IDLE,
                            break;
                        case 1: //  RECONNECT_SUCCESS,
                            LogHelper.Debug("RECONNECT_SUCCESS");
                            if (FRTCView.FRTCPopupViewManager.CurrentPopup != null
                                && FRTCView.FRTCPopupViewManager.CurrentPopup is FRTCView.FRTCReconnectingWindow)
                            {
                                LogHelper.Debug("close FRTCReconnectingWindow");
                                FRTCView.FRTCPopupViewManager.CurrentPopup.Close();
                            }
                            else
                            {
                                LogHelper.Debug("CurrentPopup is {0}", FRTCView.FRTCPopupViewManager.CurrentPopup == null ? "null" : FRTCView.FRTCPopupViewManager.CurrentPopup.GetType().FullName);
                            }
                            break;
                        case 2:  // RECONNECT_TRYING,
                            LogHelper.Debug("RECONNECT_TRYING");
                            if (FRTCView.FRTCPopupViewManager.CurrentPopup != null)
                            {
                                if (FRTCView.FRTCPopupViewManager.CurrentPopup is FRTCView.FRTCReconnectingWindow)
                                {
                                    LogHelper.Debug("existing FRTCReconnectingWindow");
                                    break;
                                }
                                else
                                {
                                    FRTCView.FRTCPopupViewManager.CurrentPopup.Close();
                                }
                            }
                            rosterListWindow?.Close();
                            statisticsWnd?.Close();
                            inviteInfoWindow?.Close();
                            frtcContentSourceWindow?.Close();
                            if (_isSendingContent)
                            {
                                StopShareContent();
                                OnContentSendingState(false);
                            }
                            object[] p = new object[1];
                            p[0] = _meetingVideoWnd;
                            LogHelper.Debug("show FRTCReconnectingWindow");
                            FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCReconnecting, p);
                            break;
                        case 3:  // RECONNECT_FAILED                        
                            if (FRTCView.FRTCPopupViewManager.CurrentPopup != null &&
                                FRTCView.FRTCPopupViewManager.CurrentPopup is FRTCView.FRTCReconnectingWindow)
                            {
                                (FRTCView.FRTCPopupViewManager.CurrentPopup as FRTCView.FRTCReconnectingWindow).ShowDlg();
                            }
                            break;
                        default:
                            break;
                    }
                }), null);
            }
        }
        public void OnEnableContentAudio(bool bEnable)
        {
            if (m_meetingWndThread != null)
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    if (bEnable)
                    {
                        ContentAudioIndex = 1;
                    }
                    else
                    {
                        if (IsShareAudio)
                        {
                            ContentAudioIndex = 2;
                        }
                    }
                });
            }
        }

        public void HandleServerAskUnmute()
        {
            if (m_meetingWndThread != null)
            {
                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke((Action)delegate ()
                {
                    if (FRTCView.FRTCMessageBox.ShowAskUnmuteMessage(_meetingVideoWnd))
                    {
                        LogHelper.Debug("Server ask unmute, user agreed do unmute");
                        MicMuted = false;
                        MuteLocalAudio(false);
                    }
                });
            }
        }

        public void CreateList(List<RosterItem> array)
        {
            if (m_meetingWndThread != null)
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    LogHelper.Debug("Enter lock m_rosterListLockObj");
                    lock (m_rosterListLockObj)
                    {
                        if (RosterList != null)
                        {
                            RosterList.Clear();
                        }

                        RosterList = CreateRosterList(array);
                        RosterList?.Sort(1, (RosterList.Count - 1), new LecturerComparer());
                        RaisePropertyChanged("RosterList");
                    }
                });
            }
        }

        public void UpdateList(List<RosterItem> array)
        {
            if (m_meetingWndThread != null)
            {
                Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                {
                    LogHelper.Debug("Enter lock m_rosterListLockObj");
                    lock (m_rosterListLockObj)
                    {
                        RosterList = UpdateRosterList(array);
                        RosterList?.Sort(1, (RosterList.Count - 1), new LecturerComparer());
                        RaisePropertyChanged("RosterList");
                    }
                });
            }
        }

        private List<RosterItem> CreateRosterList(IEnumerable<RosterItem> nameList)
        {
            if (mRosterList != null)
                mRosterList.Clear();
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            List<RosterItem> ret = new List<RosterItem>(nameList.Count());

            bool hasSelf = false;
            foreach (RosterItem n in nameList)
            {
                RosterItem it = new RosterItem() { Name = n.Name, UUID = n.UUID, MuteVideo = n.MuteVideo, MuteAudio = n.MuteAudio, IsLecturer = n.IsLecturer };

                it.IsPinned = it.UUID == PinnedUUID;

                if (SelectedParticipantCopy != null && SelectedParticipantCopy.UUID == it.UUID)
                {
                    SelectedParticipantCopy.MuteAudio = it.MuteAudio;
                    SelectedParticipantCopy.MuteVideo = it.MuteVideo;
                    SelectedParticipantCopy.Name = it.Name;
                    SelectedParticipantCopy.IsLecturer = it.IsLecturer;
                }
                if (it.UUID == FRTCUIUtils.GetFRTCDeviceUUID())
                {
                    it.Remark = it.IsLecturer ? Properties.Resources.FRTC_MEETING_SDKAPP_LECTURE_LABEL_SELF : Properties.Resources.FRTC_MEETING_ROSTER_ME;
                    it.IsSelf = true;
                    it.MuteAudio = self.MicMuted ? "true" : "false";
                    it.MuteVideo = self.CameraMuted ? "true" : "false";
                    if ((_tmpDisplayName == string.Empty || it.Name == _tmpDisplayName) && it.Name != DisplayName)//name had been changed
                    {
                        DisplayName = it.Name;
                        ShowTips(FrtcReminderType.DISPLAY_NAME_RENAMED);
                    }
                    ret.Insert(0, it);
                    hasSelf = true;
                }
                else
                {
                    it.Remark = it.IsLecturer ? Properties.Resources.FRTC_MEETING_SDKAPP_LECTURE_LABEL : string.Empty;
                    ret.Add(it);
                }
            }

            if (!hasSelf)
            {
                RosterItem selfItem = new RosterItem();
                selfItem.UUID = _selfUUID;
                if (string.IsNullOrEmpty(_renameUserUUID))
                {
                    selfItem.Name = DisplayName;
                }
                else
                {
                    if (_renameUserUUID == _selfUUID && !string.IsNullOrEmpty(_tmpDisplayName))
                    {
                        selfItem.Name = _tmpDisplayName;
                    }
                    else
                    {
                        selfItem.Name = DisplayName;
                    }
                }
                selfItem.IsLecturer = HasLecture && _currentLecturerList.Contains(_selfUUID);
                selfItem.Remark = Properties.Resources.FRTC_MEETING_ROSTER_ME;
                selfItem.IsPinned = PinnedUUID == _selfUUID;
                selfItem.MuteAudio = MicMuted.ToString().ToLower(); ;
                selfItem.MuteVideo = CameraMuted.ToString().ToLower();
                selfItem.IsSelf = true;
                ret.Insert(0, selfItem);
            }
            mRosterList = ret;
            return mRosterList;
        }

        private List<RosterItem> UpdateRosterList(IEnumerable<RosterItem> nameList)
        {
            if (mRosterList != null)
            {
                foreach (RosterItem n in nameList)
                {
                    RosterItem item = mRosterList.Find(x => x.UUID == n.UUID);
                    if (item != null)
                    {
                        item.MuteAudio = n.MuteAudio;
                        item.MuteVideo = n.MuteVideo;
                        item.Name = n.Name;
                        item.IsLecturer = n.IsLecturer;
                        item.IsPinned = item.UUID == PinnedUUID;
                        item.Remark = n.Remark;
                        item.IsSelf = n.IsSelf;
                    }
                }
            }
            else
            {
                mRosterList = new List<RosterItem>();
            }

            List<RosterItem> ret = new List<RosterItem>(mRosterList);

            return ret;
        }

        public void OnParticipant(int num)
        {
            string capsion = SQMeeting.Properties.Resources.FRTC_MEETING_SDKAPP_PARTICIPANTS;
            PaticipantHeader = capsion + "(" + num.ToString() + ")";
            if (num > 999)
            {
                RosterNum = "999+";
            }
            else
            {
                RosterNum = num.ToString();
            }
        }

        private static void OnWndMouseEventCB(SDKVIDEOEVENT type)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            switch (type)
            {
                case SDKVIDEOEVENT.LBUTTONDBLCLKEVENT:
                    {
                        self.DBCLKVideo();
                        break;
                    }
                case SDKVIDEOEVENT.LBUTTONUPEVENT:
                    {
                        self.LBtnCLKVideo();
                        break;
                    }
            }
        }

        private void OnReciveMeetingMsg(MeetingMessageInfo info, string strType)
        {
            MeetingMsgInfo.MsgColor = info.MsgColor;
            MeetingMsgInfo.MsgContent = info.MsgContent;
            MeetingMsgInfo.MsgDisplaySpeed = info.MsgDisplaySpeed;
            MeetingMsgInfo.MsgFont = info.MsgFont;
            MeetingMsgInfo.MsgBackgroundTransparency = info.MsgBackgroundTransparency;
            MeetingMsgInfo.MsgDisplayRepetition = info.MsgDisplayRepetition;
            MeetingMsgInfo.MsgFontSize = info.MsgFontSize;
            MeetingMsgInfo.MsgVerticalPosition = info.MsgVerticalPosition;
            MeetingMsgInfo.EnabledMeetingMessage = info.EnabledMeetingMessage;
            ShowMeetingMsgWnd = MeetingMsgInfo.EnabledMeetingMessage;

            if (_meetingVideoWnd != null && (info.EnabledMeetingMessage))
            {
                ShowMeetingMsg();
                if (meetingMsgWnd != null)
                {
                    meetingMsgWnd.StartShowMSg();
                }
                AdjustMeetingMsgPos();
            }
            else if (_meetingVideoWnd != null && (!info.EnabledMeetingMessage))
            {
                if (meetingMsgWnd != null)
                {
                    meetingMsgWnd.StopShowMSg();
                }
                HideMeetingMSg();
            }
        }

        public MeetingMessageInfo MeetingMsgInfo;

        public void RestartMessage()
        {
            if (meetingMsgWnd != null && meetingMsgWnd.Visibility == Visibility.Visible && MeetingMsgInfo != null && MeetingMsgInfo.EnabledMeetingMessage)
            {
                meetingMsgWnd.StartShowMSg();
            }
        }

        public bool IsEncrypted = true;
        private static void OnEncryptedStateNotifyCB(bool isEncrypted)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.IsEncrypted = isEncrypted;
        }

        private static void OnReminderNotify(FrtcReminderType type)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.ShowTips(type);
            self.UpdateTipsPostion();
        }

        public void UpdateTipsPostion()
        {
            if (m_meetingWndThread == null)
                return;

            Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
            {
                _meetingVideoWnd.adjustToolTips();
            });
        }

        private static void OnDisplayChanged(string monitorListData)
        {

        }

        public bool GetMonitorChanged()
        {
            IntPtr monitorStr = FRTCSDK.frtc_monitors_collect();
            if (monitorStr != IntPtr.Zero)
            {
                string jStr = FRTCUIUtils.StringFromNativeUtf8(monitorStr);
                JObject jObj = JObject.Parse(jStr);
                JArray array = (JArray)jObj["monitors"];
                UpdateMonitorList = CreateMonitorList(array);
            }

            if (MonitorList.Count != 0 && MonitorList.Count != UpdateMonitorList.Count)
            {
                return true;
            }
            else
            {
                if (CurMonInfo != null && MonitorList.Count != 0)
                {
                    foreach (FRTCMonitor item in MonitorList)
                    {
                        if (item.handle != CurMonInfo.handle)
                            continue;

                        foreach (FRTCMonitor updateItem in UpdateMonitorList)
                        {
                            if (updateItem.handle != CurMonInfo.handle)
                                continue;

                            {
                                if (item.right - item.left == updateItem.right - updateItem.left && item.bottom - item.top == updateItem.bottom - updateItem.top)
                                {
                                    continue;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
                }
                else
                {
                    return false;
                }
            }
        }

        public void ShowTips(FrtcReminderType type)
        {
            if (m_meetingWndThread == null || Dispatcher.FromThread(m_meetingWndThread) == null)
            {
                return;
            }
            try
            {
                Dispatcher.FromThread(m_meetingWndThread).BeginInvoke(new Action(() =>
                {
                    if (type == FrtcReminderType.SHARECONTENT_CPULIMITED)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_SHARECONTENT_CPULIMITED;

                    }
                    else if (type == FrtcReminderType.SHARECONTENT_NOPERMISSION)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_SHARECONTENT_NOPERMISSION;
                    }
                    else if (type == FrtcReminderType.SHARECONTENT_UPLINKBITRATELOW)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_SHARECONTENT_UPLINKBITRATELOW;
                    }
                    else if (type == FrtcReminderType.MICROPHONEMUTED)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_MICROPHONEMUTED;
                    }
                    else if (type == FrtcReminderType.CAMERAERROR)
                    {
                        CameraMuted = true;
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_CAMERAERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_INFO_COPIED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_COPY_CLIPBOARD_DONE;
                    }
                    else if (type == FrtcReminderType.MICROPHONE_MUTED_BY_ADMIN)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_HOST_MUTE_MEETING;
                    }
                    else if (type == FrtcReminderType.DISPLAY_NAME_RENAMED)
                    {
                        IsShowTips = true;
                        TipsContent = string.Format(Properties.Resources.FRTC_MEETING_SDKAPP_RENAME_NOTIFY, DisplayName);
                    }
                    else if (type == FrtcReminderType.YOU_ARE_LECTURER)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_YOU_ARE_LECTURER;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_GENERAL_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_OPERATION_FAILED;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_PARAM_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_PARAM_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_AUTH_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_AUTH_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_PERMISSION_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_PERMISSION_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_NOMEETING_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_NOMEETING_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_DATA_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_DATA_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_FORBIDDEN_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_FORBIDDEN_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_STATUS_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_STATUS_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_CONTROL_OPERATION_RECORDING_ERROR)
                    {
                        IsShowTips = true;
                        IsShowTipsIcon = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_REST_RECORDING_ERROR;
                    }
                    else if (type == FrtcReminderType.MEETING_RECORDING_START)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_START_MSG;
                    }
                    else if (type == FrtcReminderType.MEETING_RECORDING_START_OPERATOR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_START_MSG_AUTH;
                    }
                    else if (type == FrtcReminderType.MEETING_RECORDING_STOP)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_STOP_MSG;
                    }
                    else if (type == FrtcReminderType.MEETING_RECORDING_STOP_OPERATOR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_STOP_MSG_AUTH;
                    }
                    else if (type == FrtcReminderType.MEETING_STREAMING_START)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_START_MSG;
                    }
                    else if (type == FrtcReminderType.MEETING_STREAMING_START_OPERATOR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_START_MSG_AUTH;
                    }
                    else if (type == FrtcReminderType.MEETING_STREAMING_STOP)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_STOP_MSG;
                    }
                    else if (type == FrtcReminderType.MEETING_STREAMING_STOP_OPERATOR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_STOP_MSG_AUTH;
                    }
                    else if (type == FrtcReminderType.STREAMING_INFO_COPIED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_COPY_STREAMING_INFO_MSG;
                    }
                    else if (type == FrtcReminderType.TEXT_OVERLAY_STARTED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_TEXT_OVERLAY_SET_STARTED;
                    }
                    else if (type == FrtcReminderType.TEXT_OVERLAY_STOPPED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_TEXT_OVERLAY_SET_STOPED;
                    }
                    else if (type == FrtcReminderType.VIOCE_ONLY_MEETING_NOTIFICATION)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_VIOCE_ONLY_MEETING_TIP;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_START_ERROR;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_PARAMS_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_PARAMS_ERROR;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_NO_LICENSE)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_NO_LICENSE;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_LICENSE_LIMITATION_REACHED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_LICENSE_LIMITATION_ERROR;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_INSUFFICIENT_RESOURCE)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_INSUFFICIENT_RESOURCE;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_NO_SERVICE)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_NO_SERVICE;
                    }
                    else if (type == FrtcReminderType.START_RECORDING_MULTIPLY)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_MULTIPLE_START;
                    }
                    else if (type == FrtcReminderType.STOP_RECORDING_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_STOP_ERROR;
                    }
                    else if (type == FrtcReminderType.STOP_RECORDING_PARAMS_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_RECORDING_STOP_PARAMS_ERROR;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_START_ERROR;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_PARAMS_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_PARAMS_ERROR;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_NO_LICENSE)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_NO_LICENSE;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_LICENSE_LIMITATION_REACHED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_LICENSE_LIMITATION_ERROR;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_INSUFFICIENT_RESOURCE)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_INSUFFICIENT_RESOURCE;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_NO_SERVICE)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_NO_SERVICE;
                    }
                    else if (type == FrtcReminderType.START_STREAMING_MULTIPLY)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_MULTIPLE_START;
                    }
                    else if (type == FrtcReminderType.STOP_STREAMING_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_STOP_ERROR;
                    }
                    else if (type == FrtcReminderType.STOP_STREAMING_PARAMS_ERROR)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_STREAMING_STOP_PARAMS_ERROR;
                    }
                    else if (type == FrtcReminderType.PIN_VIDEO_SUCCESS)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_PIN_VIDEO_TIP;
                    }
                    else if (type == FrtcReminderType.UNPIN_VIDEO_SUCCESS)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_UNPIN_VIDEO_TIP;
                    }
                    else if (type == FrtcReminderType.VIDEO_PINNED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_VIDEO_PINNED_TIP;
                    }
                    else if (type == FrtcReminderType.VIDEO_UNPINNED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_VIDEO_UNPINNED_TIP;
                    }
                    else if (type == FrtcReminderType.UNMUTE_APPLIED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_UNMUTE_APPLIED_MSG;
                    }
                    else if (type == FrtcReminderType.ENABLE_MUTE_ALL)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_MUTEALL_ENABLED;
                    }
                    else if (type == FrtcReminderType.DISABLE_MUTE_ALL)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_MUTEALL_DISABLED;
                    }
                    else if (type == FrtcReminderType.MICROPHONE_UNMUTED)
                    {
                        IsShowTips = true;
                        TipsContent = Properties.Resources.FRTC_MEETING_SDKAPP_MIC_UNMUTED;
                    }
                    else
                    {
                        TipsContent = string.Empty;
                        IsShowTips = false;
                        IsShowTipsIcon = false;
                    }

                    if (ShowTipsTimer != null)
                    {
                        ShowTipsTimer.Stop();
                        ShowTipsTimer = null;
                    }

                    if (IsShowTips)
                    {
                        if (ShowTipsTimer == null)
                        {
                            try
                            {
                                ShowTipsTimer = new DispatcherTimer(
                                 TimeSpan.FromSeconds(3), DispatcherPriority.Normal,
                                 new EventHandler((o, ev) =>
                                 {
                                     IsShowTips = false;
                                     TipsContent = string.Empty;
                                     IsShowTipsIcon = false;
                                 }), Dispatcher.FromThread(m_meetingWndThread));
                            }
                            catch
                            {
                                if (ShowTipsTimer != null)
                                {
                                    ShowTipsTimer.Stop();
                                }
                                ShowTipsTimer = null;

                                IsShowTips = false;
                                TipsContent = string.Empty;
                            }
                        }

                        if (ShowTipsTimer != null)
                            ShowTipsTimer.Start();
                    }
                }
                ));
            }
            catch
            {
                if (ShowTipsTimer != null)
                {
                    ShowTipsTimer.Stop();
                }
                ShowTipsTimer = null;
                IsShowTips = false;
                TipsContent = string.Empty;
            }
        }

        private IntPtr JoinMeeting(IntPtr hostHwnd)
        {
            string strLayout = ConfigurationManager.AppSettings["VideoLayout"];
            _currentLayout = (FrtcLayout)Enum.Parse(typeof(FrtcLayout), strLayout, true);

            IsGalleryView = CurrentLayout == FrtcLayout.LAYOUT_GALLERY;

            FRTCUIUtils.MeetingWindowHandle = hostHwnd;

            ResolutionTools.UpdateMeetingWindowResolution(FRTCMeetingWndScreen);
            SimpleIoc.Default.GetInstance<FRTCCallManager>().m_meetingVideoWndHWND = FRTCSDK.frtc_parent_hwnd_set(hostHwnd);
            return SimpleIoc.Default.GetInstance<FRTCCallManager>().m_meetingVideoWndHWND;
        }

        public IntPtr GetWrapperMainWndHandle()
        {
            return SimpleIoc.Default.GetInstance<FRTCCallManager>().m_meetingVideoWndHWND;
        }

        private void ShowRosterListWindow()
        {
            GetParticipantList();

            if (rosterListWindow != null)
            {
                rosterListWindow.Close();
                rosterListWindow = null;
            }

            if (rosterListWindow == null)
            {
                rosterListWindow = new View.RosterListWindow();
            }
            rosterListWindow.Owner = _meetingVideoWnd;
            if (!ShareContentEnable)
                rosterListWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            else
                rosterListWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            rosterListWindow.Closed += (s, e) => { this.IsRosterWndShow = false; this.SearchUserPattern = string.Empty; };
            rosterListWindow.Show();
        }

        public void ParseStatisticsReport(string report)
        {
            if (IsSendingContent)
            {
                LogTool.LogHelper.Debug("ParseStatisticsReport: {0}" + report);
            }
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(report)))
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(FRTCStatistics));
                this.StatisticsReport = (FRTCStatistics)jsonSerializer.ReadObject(ms);
                ms.Close();
            }
            if (this.StatisticsReport.signalStatistics.callRate > 100000)
            {
                this.CallInfo = (this.StatisticsReport.signalStatistics.callRate / 100000).ToString() + "/" + (this.StatisticsReport.signalStatistics.callRate % 100000).ToString();
                SelfStatistics.TotalUploadRate = this.StatisticsReport.signalStatistics.callRate / 100000;
                SelfStatistics.TotalDownloadRate = this.StatisticsReport.signalStatistics.callRate % 100000;
            }
            else
            {
                this.CallInfo = this.StatisticsReport.signalStatistics.callRate.ToString();
            }
            List<MediaStatisticsForUI> tmp = new List<MediaStatisticsForUI>();
            int totalAPRRate = 0;
            long totalAPRPackage = 0;
            long totalAPRLostPackage = 0;
            foreach (APR apr in this.StatisticsReport.mediaStatistics.apr)
            {
                MediaStatisticsForUI ui = new MediaStatisticsForUI();
                ui.Channel = "Audio";
                ui.Format = "-";
                ui.FrameRate = apr.frameRate;
                ui.Jitter = apr.jitter.ToString();
                ui.PackageLost = string.Format("{0}%({1})/{2}%({3})",
                                                apr.packageLossRate,
                                                apr.packageLoss,
                                                apr.logicPacketLossRate,
                                                apr.logicPacketLoss);
                ui.Participant = apr.participantName;
                ui.Rate = "N/A";
                ui.UsingRate = apr.rtpActualBitRate.ToString();
                tmp.Add(ui);
                totalAPRRate += apr.rtpActualBitRate;
                totalAPRPackage += apr.packageTotal;
                totalAPRLostPackage += apr.packageLoss;
            }
            if (totalAPRPackage > 0)
                SelfStatistics.AudioDownloadRate = string.Format("{0} ({1}%)", totalAPRRate, (int)(((double)totalAPRLostPackage / (double)totalAPRPackage) * 100));
            else
                SelfStatistics.AudioDownloadRate = "-";

            int totalAPSRate = 0;
            long totalAPSPackage = 0;
            long totalAPSLostPackage = 0;
            long totalAPSRTT = 0;
            long averageAPSRTT = 0;
            int apsPkgLostRate = 0;
            foreach (APS aps in this.StatisticsReport.mediaStatistics.aps)
            {
                MediaStatisticsForUI ui = new MediaStatisticsForUI();
                ui.Channel = "Audio ↑";
                ui.Format = "-";
                ui.FrameRate = aps.frameRate;
                ui.Jitter = aps.jitter.ToString();
                ui.PackageLost = string.Format("{0}%({1})", aps.packageLossRate, aps.packageLoss);
                ui.Participant = "local";
                ui.Rate = aps.rtpLogicBitRate.ToString();
                ui.UsingRate = aps.rtpActualBitRate.ToString() + "(" + aps.roundTripTime.ToString() + ")";
                tmp.Add(ui);
                totalAPSRate = aps.rtpActualBitRate;
                totalAPSPackage = aps.packageTotal;
                totalAPSLostPackage = aps.packageLoss;
                totalAPSRTT = aps.roundTripTime;
                apsPkgLostRate = aps.packageLossRate;
            }
            if (totalAPSPackage > 0)
                SelfStatistics.AudioUploadRate = string.Format("{0} ({1}%)", totalAPSRate, apsPkgLostRate);
            else
                SelfStatistics.AudioUploadRate = "-";

            if (StatisticsReport.mediaStatistics.aps.Count() > 0)
                averageAPSRTT = totalAPSRTT / StatisticsReport.mediaStatistics.aps.Count();

            int totalVPRRate = 0;
            long totalVPRPackage = 0;
            long totalVPRLostPackage = 0;
            int totalVPRPkgLostRate = 0;
            foreach (VPR vpr in this.StatisticsReport.mediaStatistics.vpr)
            {
                MediaStatisticsForUI ui = new MediaStatisticsForUI();
                ui.Channel = "Video";
                ui.Format = vpr.resolution;
                ui.FrameRate = vpr.frameRate;
                ui.Jitter = vpr.jitter.ToString();
                ui.PackageLost = string.Format("{0}%({1})", vpr.packageLossRate, vpr.packageLoss);
                ui.Participant = vpr.participantName;
                ui.Rate = "N/A";
                ui.UsingRate = vpr.rtpActualBitRate.ToString();
                tmp.Add(ui);
                totalVPRRate += vpr.rtpActualBitRate;
                totalVPRPackage += vpr.packageTotal;
                totalVPRLostPackage += vpr.packageLoss;
                totalVPRPkgLostRate += vpr.packageLossRate;
            }
            if (this.StatisticsReport.mediaStatistics.vpr.Count() > 0)
                SelfStatistics.VideoDownloadRate = string.Format("{0} ({1}%)", totalVPRRate, totalVPRPkgLostRate / this.StatisticsReport.mediaStatistics.vpr.Count());
            else
                SelfStatistics.VideoDownloadRate = "-";

            int totalVPSRate = 0;
            long totalVPSPackage = 0;
            long totalVPSLostPackage = 0;
            long totalVPSRTT = 0;
            long averrageVPSRTT = 0;
            int totalPkgLostRate = 0;
            foreach (VPS vps in this.StatisticsReport.mediaStatistics.vps)
            {
                MediaStatisticsForUI ui = new MediaStatisticsForUI();
                ui.Channel = "Video ↑";
                ui.Format = vps.resolution;
                ui.FrameRate = vps.frameRate;
                ui.Jitter = vps.jitter.ToString();
                ui.PackageLost = string.Format("{0}%({1})", vps.packageLossRate, vps.packageLoss);
                ui.Participant = "local";
                ui.Rate = vps.rtpLogicBitRate.ToString();
                ui.UsingRate = vps.rtpActualBitRate.ToString() + "(" + vps.roundTripTime.ToString() + ")";
                tmp.Add(ui);
                totalVPSRate += vps.rtpActualBitRate;
                totalVPSPackage += vps.packageTotal;
                totalVPSLostPackage += vps.packageLoss;
                totalVPSRTT += vps.roundTripTime;
                totalPkgLostRate += vps.packageLossRate;
            }
            if (totalAPSPackage > 0 && this.StatisticsReport.mediaStatistics.vps.Count() > 0)
                SelfStatistics.VideoUploadRate = string.Format("{0} ({1}%)", totalVPSRate, (int)(totalPkgLostRate / this.StatisticsReport.mediaStatistics.vps.Count()));
            else
                SelfStatistics.VideoUploadRate = "-";

            if (this.StatisticsReport.mediaStatistics.vps.Count() > 0)
                averrageVPSRTT = totalVPSRTT / this.StatisticsReport.mediaStatistics.vps.Count();

            int totalVCRRate = 0;
            long totalVCRPackage = 0;
            long totalVCRLostPackage = 0;
            int totalVCRPkgLostRate = 0;
            foreach (VCR vcr in this.StatisticsReport.mediaStatistics.vcr)
            {
                MediaStatisticsForUI ui = new MediaStatisticsForUI();
                ui.Channel = "Content";
                ui.Format = vcr.resolution;
                ui.FrameRate = vcr.frameRate;
                ui.Jitter = vcr.jitter.ToString();
                ui.PackageLost = string.Format("{0}%({1})", vcr.packageLossRate, vcr.packageLoss);
                ui.Participant = vcr.participantName;
                ui.Rate = "N/A";
                ui.UsingRate = vcr.rtpActualBitRate.ToString();
                tmp.Add(ui);
                totalVCRRate = vcr.rtpActualBitRate;
                totalVCRPackage = vcr.packageTotal;
                totalVCRLostPackage = vcr.packageLoss;
                totalVCRPkgLostRate += vcr.packageLossRate;
            }
            if (this.StatisticsReport.mediaStatistics.vcr.Count() > 0)
                SelfStatistics.ContentDownloadRate = string.Format("{0} ({1}%)", totalVCRRate, totalVCRPkgLostRate / this.StatisticsReport.mediaStatistics.vcr.Count());
            else
                SelfStatistics.ContentDownloadRate = "-";

            int totalVCSRate = 0;
            long totalVCSPackage = 0;
            long totalVCSLostPackage = 0;
            long totalVCSRTT = 0;
            long averageVCSRTT = 0;
            int totalVCSPkgLost = 0;
            foreach (VCS vcs in this.StatisticsReport.mediaStatistics.vcs)
            {
                MediaStatisticsForUI ui = new MediaStatisticsForUI();
                ui.Channel = "Content ↑";
                ui.Format = vcs.resolution;
                ui.FrameRate = vcs.frameRate;
                ui.Jitter = vcs.jitter.ToString();
                ui.PackageLost = string.Format("{0}%({1})", vcs.packageLossRate, vcs.packageLoss);
                ui.Participant = "local";
                ui.Rate = vcs.rtpLogicBitRate.ToString();
                ui.UsingRate = vcs.rtpActualBitRate.ToString() + "(" + vcs.roundTripTime.ToString() + ")";
                tmp.Add(ui);
                totalVCSRate += vcs.rtpActualBitRate;
                totalVCSPackage += vcs.packageTotal;
                totalVCSLostPackage += vcs.packageLoss;
                totalVCSRTT += vcs.roundTripTime;
                totalVCSPkgLost += vcs.packageLossRate;
            }
            if (this.StatisticsReport.mediaStatistics.vcs.Count() > 0)
                SelfStatistics.ContentUploadRate = string.Format("{0} ({1}%)", totalVCSRate, totalVCSPkgLost / this.StatisticsReport.mediaStatistics.vcs.Count());
            else
                SelfStatistics.ContentUploadRate = "-";
            if (this.StatisticsReport.mediaStatistics.vcs.Count() > 0)
                averageVCSRTT = totalVCSRTT / this.StatisticsReport.mediaStatistics.vcs.Count();

            int rtt_cnt = 0;
            if (averageAPSRTT > 0) rtt_cnt++;
            if (averrageVPSRTT > 0) rtt_cnt++;
            if (averageVCSRTT > 0) rtt_cnt++;
            if (rtt_cnt > 0)
                SelfStatistics.Delay = string.Format("{0} ms", (averageAPSRTT + averrageVPSRTT + averageVCSRTT) / rtt_cnt);
            else
                SelfStatistics.Delay = "-";

            tmp.Sort((left, right) =>
            {
                int leftIndex = mediaStatisticsChannelOrder[left.Channel];
                int rightIndex = mediaStatisticsChannelOrder[right.Channel];
                if (leftIndex < rightIndex)
                    return -1;
                else if (leftIndex > rightIndex)
                    return 1;
                else
                    return 0;
            });
            this.MediaStatistics = tmp;
        }

        private FRTCStatistics _statisticsReport;
        public FRTCStatistics StatisticsReport
        {
            get
            {
                return _statisticsReport;
            }
            set
            {
                if (_statisticsReport != value)
                {
                    _statisticsReport = value;
                    RaisePropertyChanged();
                }
            }
        }

        private Dictionary<string, int> mediaStatisticsChannelOrder = new Dictionary<string, int>()
        {
            { "Audio", 0 },
            { "Audio ↑", 1 },
            { "Video", 2 },
            { "Video ↑", 3 },
            { "Content", 4 },
            { "Content ↑", 5 },
        };

        private List<MediaStatisticsForUI> _mediaStatistics;
        public List<MediaStatisticsForUI> MediaStatistics
        {
            get
            {
                return _mediaStatistics;
            }
            set
            {
                if (_mediaStatistics != value)
                {
                    _mediaStatistics = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SelfStatisticsInfo _selfStatistics = new SelfStatisticsInfo();
        public SelfStatisticsInfo SelfStatistics
        {
            get { return _selfStatistics; }
            set
            {
                _selfStatistics = value;
                RaisePropertyChanged("SelfStatistics");
            }
        }

        private bool _isSelfStatisticsPopupShow = false;
        public bool IsSelfStatisticsPopupShow
        {
            get => _isSelfStatisticsPopupShow;
            set
            {
                _isSelfStatisticsPopupShow = value;
                if (_isSelfStatisticsPopupShow)
                {
                    updateStatisticsTimer.Stop();
                    updateStatisticsTimer.Start();
                }
                else
                    updateStatisticsTimer.Stop();
            }
        }

        public FRTCMonitor CurMonInfo { get; set; }

        public IntPtr CurSharingWndHwnd { get; set; }


        private string _callInfo = string.Empty;
        public string CallInfo
        {
            get
            {
                return _callInfo;
            }
            set
            {
                if (_callInfo != value)
                {
                    _callInfo = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _TipsContent = string.Empty;
        public string TipsContent
        {
            get
            {
                return _TipsContent;
            }
            set
            {
                if (_TipsContent != value)
                {
                    _TipsContent = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsShowTips = false;
        public bool IsShowTips
        {
            get
            {
                return _IsShowTips;
            }
            set
            {
                if (_IsShowTips != value)
                {
                    _IsShowTips = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsShowTipsIcon = false;
        public bool IsShowTipsIcon
        {
            get
            {
                return _IsShowTipsIcon;
            }
            set
            {
                if (_IsShowTipsIcon != value)
                {
                    _IsShowTipsIcon = value;
                    RaisePropertyChanged();
                }
            }
        }


        private bool _IsShowCamralBtn = false;
        public bool IsShowCamralBtn
        {
            get
            {
                return _IsShowCamralBtn;
            }
            set
            {
                if (_IsShowCamralBtn != value)
                {
                    _IsShowCamralBtn = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsMonitorList = false;
        public bool IsMonitorList
        {
            get
            {
                return _IsMonitorList;
            }
            set
            {
                if (_IsMonitorList != value)
                {
                    _IsMonitorList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _meetingID = string.Empty;
        public string MeetingID
        {
            get
            {
                return _meetingID;
            }
            set
            {
                if (_meetingID != value)
                {
                    _meetingID = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _MainTitle = string.Empty;
        public string MainTitle
        {
            get
            {
                return _MainTitle;
            }
            set
            {
                if (_MainTitle != value)
                {
                    _MainTitle = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _IntensityImageIndex = 0;

        public int IntensityImageIndex
        {
            get
            {
                return _IntensityImageIndex;
            }
            set
            {
                if (_IntensityImageIndex != value)
                {
                    _IntensityImageIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _ContentAudioIndex = 0;

        public int ContentAudioIndex
        {
            get
            {
                return _ContentAudioIndex;
            }
            set
            {
                if (_ContentAudioIndex != value)
                {
                    _ContentAudioIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _meetingName = string.Empty;
        public string MeetingName
        {
            get
            {
                return _meetingName;
            }
            set
            {
                if (_meetingName != value)
                {
                    _meetingName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _meetingPassCode = string.Empty;
        public string MeetingPassCode
        {
            get => _meetingPassCode;
            set
            {
                if (_meetingPassCode != value)
                {
                    _meetingPassCode = value;
                    RaisePropertyChanged("MeetingPassCode");
                }
            }
        }

        private string _meetingOwnerId = string.Empty;
        public string MeetingOwnerId
        {
            get => _meetingOwnerId;
            set
            {
                if (_meetingOwnerId != value)
                {
                    _meetingOwnerId = value;
                    RaisePropertyChanged("MeetingOwnerId");
                }
            }
        }

        private string _meetingOwnerName = string.Empty;
        public string MeetingOwnerName
        {
            get => _meetingOwnerName;
            set
            {
                if (_meetingOwnerName != value)
                {
                    _meetingOwnerName = value;
                    RaisePropertyChanged("MeetingOwnerName");
                }
            }
        }

        private bool _selfOwnedMeeting = false;
        public bool SelfOwnedMeeting
        {
            get => _selfOwnedMeeting;
            set
            {
                if (_selfOwnedMeeting != value)
                {
                    _selfOwnedMeeting = value;
                    RaisePropertyChanged("SelfOwnedMeeting");
                }
            }
        }

        private string _meetingInviteString = string.Empty;
        public string MeetingInviteString
        {
            get => _meetingInviteString;
            set
            {
                _meetingInviteString = value;
                RaisePropertyChanged("MeetingInviteString");
            }
        }

        private RelayCommand _copyMeetingInfoCommand;
        public RelayCommand CopyMeetingInfoCommand
        {
            get => _copyMeetingInfoCommand;
            set => _copyMeetingInfoCommand = value;
        }


        private FRTCMonitor findMonitor(JArray array, int index)
        {
            bool bfind = false;
            FRTCMonitor it = new FRTCMonitor();
            List<FRTCMonitor> ret = new List<FRTCMonitor>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                foreach (JProperty p in array[i].ToObject<JObject>().Properties())
                {
                    if (p.Name == "monitorName")
                    {
                        //it.monitorName = p.Value.ToString();

                    }
                    if (p.Name == "index")
                    {
                        it.index = int.Parse(p.Value.ToString());

                        it.monitorName = Properties.Resources.FRTC_MEETING_SDKAPP_DESKTOPNAME + " " + it.index;

                        if (it.index == index)
                        {
                            bfind = true;
                        }
                    }

                    if (p.Name == "left")
                    {
                        it.left = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "top")
                    {
                        it.top = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "right")
                    {
                        it.right = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "bottom")
                    {
                        it.bottom = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "handle")
                    {
                        it.handle = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "isPrimary")
                    {
                        it.isPrimary = bool.Parse(p.Value.ToString());
                    }
                }

                if (bfind)
                {
                    break;
                }
            }

            return it;
        }

        private ObservableCollection<FRTCMonitor> CreateMonitorList(JArray array)
        {
            List<FRTCMonitor> ret = new List<FRTCMonitor>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                FRTCMonitor it = new FRTCMonitor();
                foreach (JProperty p in array[i].ToObject<JObject>().Properties())
                {
                    if (p.Name == "monitorName")
                    {
                        //it.monitorName = p.Value.ToString();

                    }
                    if (p.Name == "index")
                    {
                        it.index = int.Parse(p.Value.ToString());

                        it.monitorName = Properties.Resources.FRTC_MEETING_SDKAPP_DESKTOPNAME + " " + it.index;
                    }

                    if (p.Name == "left")
                    {
                        it.left = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "top")
                    {
                        it.top = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "right")
                    {
                        it.right = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "bottom")
                    {
                        it.bottom = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "handle")
                    {
                        it.handle = int.Parse(p.Value.ToString());
                    }

                    if (p.Name == "isPrimary")
                    {
                        it.isPrimary = bool.Parse(p.Value.ToString());
                    }
                }
                ret.Add(it);
            }

            ObservableCollection<FRTCMonitor> list = new ObservableCollection<FRTCMonitor>(ret);
            return list;
        }

        private ObservableCollection<ContentSourceItem> CreateWindowList(JArray array)
        {
            List<ContentSourceItem> ret = new List<ContentSourceItem>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                int hwnd = 0;
                string title = string.Empty;
                foreach (JProperty p in array[i].ToObject<JObject>().Properties())
                {
                    if (p.Name == "windowTitle")
                    {
                        title = p.Value.ToString();

                    }
                    if (p.Name == "hwnd")
                    {
                        hwnd = int.Parse(p.Value.ToString());
                    }
                }
                if (hwnd != 0
                    && hwnd != _videoWndHost.GetHostHandle().ToInt32()
                    && hwnd != FRTCUIUtils.MeetingWindowHandle.ToInt32())
                {
                    ret.Add(new ContentSourceItem(hwnd, title));
                }
            }
            ObservableCollection<ContentSourceItem> list = new ObservableCollection<ContentSourceItem>(ret);
            return list;
        }

        private bool _isMeetingOwner = false;
        public bool IsMeetingOwner
        {
            get { return _isMeetingOwner; }
            set { _isMeetingOwner = value; RaisePropertyChanged("IsMeetingOwner"); }
        }

        private bool _isOperatorRole = false;
        public bool IsOperatorRole
        {
            get { return _isOperatorRole; }
            set { _isOperatorRole = value; RaisePropertyChanged("IsOperatorRole"); }
        }

        private bool _isGuestMeeting = false;
        public bool IsGuestMeeting
        {
            get { return _isGuestMeeting; }
            set { _isGuestMeeting = value; RaisePropertyChanged("IsGuestMeeting"); }
        }

        public bool ShareContentEnable
        {
            get
            {
                return _shareContentEnable;
            }
            set
            {
                if (_shareContentEnable != value)
                {
                    _shareContentEnable = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _isSendingContent = false;
        public bool IsSendingContent
        {
            get { return _isSendingContent; }
            set { _isSendingContent = value; RaisePropertyChanged("IsSendingContent"); }
        }

        private bool _isReceivingContent = false;
        public bool IsReceivingContent
        {
            get { return _isReceivingContent; }
            set { _isReceivingContent = value; RaisePropertyChanged("IsReceivingContent"); }
        }


        private bool _cameraMuted = false;
        public bool CameraMuted
        {
            get
            {
                return _cameraMuted;
            }
            set
            {
                if (_cameraMuted != value)
                {
                    _cameraMuted = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _showSamll = false;
        public bool ShowSmall
        {
            get
            {
                return _showSamll;
            }
            set
            {
                _showSamll = value;
                MeetingMsgInfo.ShowMeetingMessage = value;
            }
        }

        private bool _showMeetingMsgWnd = false;
        public bool ShowMeetingMsgWnd
        {
            get
            {
                if (IsSmallWnd)
                {
                    return false;
                }
                return _showMeetingMsgWnd;
            }
            set
            {
                _showMeetingMsgWnd = value;
                if (IsSmallWnd)
                {
                    MeetingMsgInfo.ShowMeetingMessage = false;
                }
                else
                {
                    MeetingMsgInfo.ShowMeetingMessage = value;
                }
            }
        }

        private int _micMeterLevel = 0;
        public int MicMeterLevel
        {
            get { return _micMeterLevel; }
            set { _micMeterLevel = value; RaisePropertyChanged(); }
        }

        private bool _micMuted = false;
        public bool MicMuted
        {
            get
            {
                return _micMuted;
            }
            set
            {
                if (_micMuted != value)
                {
                    _micMuted = value;
                    if (mRosterList?.Count > 0 && m_meetingWndThread != null)
                    {
                        List<RosterItem> RosterItem = new List<RosterItem>();
                        RosterItem item = new RosterItem();
                        item.UUID = mRosterList[0].UUID;
                        item.MuteVideo = CameraMuted ? "true" : "false";
                        item.Name = mRosterList[0].Name;
                        item.IsLecturer = mRosterList[0].IsLecturer;
                        item.IsPinned = mRosterList[0].IsPinned;
                        item.Remark = mRosterList[0].Remark;
                        item.IsSelf = mRosterList[0].IsSelf;
                        if (_micMuted)
                        {
                            item.MuteAudio = "true";
                        }
                        else
                        {
                            item.MuteAudio = "false";
                        }
                        RosterItem.Add(item);
                        Dispatcher.FromThread(m_meetingWndThread).Invoke(() =>
                        {
                            RosterList = UpdateRosterList(RosterItem);
                        });
                    }

                    RaisePropertyChanged();
                }
            }
        }

        private bool _allowUnmuted = true;
        public bool AllowUnmute
        {
            get
            {
                return _allowUnmuted;
            }
            set
            {
                if (_allowUnmuted != value)
                {
                    _allowUnmuted = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _localVideoEnabled = true;
        public bool LocalVideoEnabled
        {
            get { return _localVideoEnabled; }
            set { _localVideoEnabled = value; RaisePropertyChanged("LocalVideoEnabled"); }
        }

        private string _rosterNum = "1";
        public string RosterNum
        {
            get
            {
                return _rosterNum;
            }
            set
            {
                if (_rosterNum != value)
                {
                    _rosterNum = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _paticipantHeader = SQMeeting.Properties.Resources.FRTC_MEETING_SDKAPP_PARTICIPANTS;
        public string PaticipantHeader
        {
            get
            {
                return _paticipantHeader;
            }
            set
            {
                if (_paticipantHeader != value)
                {
                    _paticipantHeader = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _searchUserPattern = string.Empty;
        public string SearchUserPattern
        {
            get { return _searchUserPattern; }
            set
            {
                if (_searchUserPattern != value)
                {
                    _searchUserPattern = value;
                    Searching = !string.IsNullOrEmpty(_searchUserPattern);
                    if (Searching)
                    {
                        SearchUserResult = RosterList.FindAll((r) => { return r.Name.ToLower().Contains(_searchUserPattern.ToLower()); });
                        NoResult = !(SearchUserResult != null && SearchUserResult.Count() > 0);
                    }
                    else
                    {
                        NoResult = false;
                        CurrentSelectedParticipant = null;
                        SelectedParticipantCopy = null;
                        SearchUserResult.Clear();
                    }
                    RaisePropertyChanged("SearchUserPattern");

                }
            }
        }

        private bool _searching = false;
        public bool Searching
        {
            get { return _searching; }
            set
            {
                _searching = value;
                RaisePropertyChanged("Searching");
                if (!_searching)
                    RosterList = new List<RosterItem>(_rosterList);
            }
        }

        private bool _noResult = false;
        public bool NoResult { get { return _noResult; } set { _noResult = value; RaisePropertyChanged("NoResult"); } }

        private List<RosterItem> _searchUserResult = new List<RosterItem>();
        public List<RosterItem> SearchUserResult
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

        public List<RosterItem> mRosterList = null;
        private List<RosterItem> _rosterList = null;
        public List<RosterItem> RosterList
        {
            get
            {
                return _rosterList;
            }
            set
            {
                _rosterList = value;
                RaisePropertyChanged("RosterList");
            }
        }

        private string _pinnedUUID = string.Empty;
        public string PinnedUUID
        {
            get
            {
                return _pinnedUUID;
            }
            set
            {
                _pinnedUUID = value;
                RaisePropertyChanged("PinnedUUID");
            }
        }

        private List<string> _currentLecturerList = new List<string>();
        public List<string> CurrentLecturerList
        {
            get => _currentLecturerList;
            set { _currentLecturerList = value; RaisePropertyChanged("CurrentLecturerList"); }
        }

        private bool _hasLecture = false;
        public bool HasLecture
        {
            get { return _hasLecture; }
            set { _hasLecture = value; RaisePropertyChanged(nameof(HasLecture)); }
        }


        private RosterItem _selectedParticipantCopy;
        public RosterItem SelectedParticipantCopy
        {
            get { return _selectedParticipantCopy; }
            set { _selectedParticipantCopy = value; RaisePropertyChanged("SelectedParticipantCopy"); }
        }


        private RosterItem _currentSelectedParticipant;
        public RosterItem CurrentSelectedParticipant
        {
            get { return _currentSelectedParticipant; }
            set { _currentSelectedParticipant = value; RaisePropertyChanged("CurrentSelectedParticipant"); }
        }

        private ObservableCollection<RosterItem> _unmuteApplicationsList;
        public ObservableCollection<RosterItem> UnmuteApplicationsList
        {
            get => _unmuteApplicationsList;
            set { _unmuteApplicationsList = value; RaisePropertyChanged(nameof(UnmuteApplicationsList)); }
        }

        private bool _newUnmuteApplications = false;
        public bool NewUnmuteApplications
        {
            get { return _newUnmuteApplications; }
            set { _newUnmuteApplications = value; RaisePropertyChanged(nameof(NewUnmuteApplications)); }
        }

        private string _newUnmuteApplicationsNotify = string.Empty;
        public string NewUnmuteApplicationsNotify
        {
            get { return _newUnmuteApplicationsNotify; }
            set { _newUnmuteApplicationsNotify = value; RaisePropertyChanged(nameof(NewUnmuteApplicationsNotify)); }
        }

        private RelayCommand _popupUnmuteApplicationList;
        public RelayCommand PopupUnmuteApplicationList
        {
            get => _popupUnmuteApplicationList;
            set => _popupUnmuteApplicationList = value;
        }

        private RelayCommand<string> _approveUmuteApplication;
        public RelayCommand<string> ApproveUmuteApplication
        {
            get => _approveUmuteApplication;
            set => _approveUmuteApplication = value;
        }

        private RelayCommand _approveAllUmuteApplications;
        public RelayCommand ApproveAllUmuteApplications
        {
            get => _approveAllUmuteApplications;
            set => _approveAllUmuteApplications = value;
        }

        private RelayCommand<string> _ignoreUmuteApplication;
        public RelayCommand<string> IgnoreUmuteApplication
        {
            get => _ignoreUmuteApplication;
            set => _ignoreUmuteApplication = value;
        }

        public bool IsSelfDialog
        {
            get { return SelectedParticipantCopy != null && SelectedParticipantCopy.UUID == FRTCUIUtils.GetFRTCDeviceUUID(); }
        }

        private RelayCommand<RosterItem> _popupMuteDialogCommand;
        public RelayCommand<RosterItem> PopupMuteDialogCommand
        {
            get => _popupMuteDialogCommand;
            set => _popupMuteDialogCommand = value;
        }

        private RelayCommand<RosterItem> _muteOneParticipant;
        public RelayCommand<RosterItem> MuteOneParticipantCommand
        {
            get => _muteOneParticipant;
            set => _muteOneParticipant = value;
        }

        private RelayCommand<RosterItem> _popupRenameDlgCommand;
        public RelayCommand<RosterItem> PopupRenameDlgCommand
        {
            get => _popupRenameDlgCommand;
            set => _popupRenameDlgCommand = value;
        }

        private RelayCommand<Window> _renameParticipantCommand;
        public RelayCommand<Window> RenameParticipantCommand
        {
            get => _renameParticipantCommand;
            set => _renameParticipantCommand = value;
        }

        private RelayCommand<RosterItem> _setAsLecturerCommand;
        public RelayCommand<RosterItem> SetAsLecturerCommand
        {
            get => _setAsLecturerCommand;
            set => _setAsLecturerCommand = value;
        }

        private RelayCommand<RosterItem> _pinVideoCommand;
        public RelayCommand<RosterItem> PinVideoCommand
        {
            get => _pinVideoCommand;
            set => _pinVideoCommand = value;
        }

        private RelayCommand<RosterItem> _removeFromMeetingCommand;
        public RelayCommand<RosterItem> RemoveFromMeetingCommand
        {
            get => _removeFromMeetingCommand;
            set => _removeFromMeetingCommand = value;
        }

        private ObservableCollection<FRTCMonitor> monitorList = null;
        public ObservableCollection<FRTCMonitor> MonitorList
        {
            get { return monitorList; }
            set
            {
                if (monitorList != value)
                {
                    monitorList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<FRTCMonitor> updateMonitorList = null;
        public ObservableCollection<FRTCMonitor> UpdateMonitorList
        {
            get { return updateMonitorList; }
            set
            {
                if (updateMonitorList != value)
                {
                    updateMonitorList = value;
                }
            }
        }

        private ObservableCollection<ContentSourceItem> windowList = null;
        public ObservableCollection<ContentSourceItem> WindowList
        {
            get { return windowList; }
            set
            {
                if (windowList != value)
                {
                    windowList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<ContentSourceItem> contentSourceList = null;
        public ObservableCollection<ContentSourceItem> ContentSourceList
        {
            get { return contentSourceList; }
            set { contentSourceList = value; RaisePropertyChanged(); }
        }

        private RelayCommand<ContentSourceItem> _shareContentCommand;
        public RelayCommand<ContentSourceItem> ShareContentCommand
        {
            get => _shareContentCommand;
        }

        private RelayCommand _fullScreenCommand;
        public RelayCommand FullScreenCommand
        {
            get => _fullScreenCommand;
            set => _fullScreenCommand = value;
        }

        private RelayCommand<string> _switchLayoutCommand;
        public RelayCommand<string> SwitchLayoutCommand
        {
            get => _switchLayoutCommand;
            set => _switchLayoutCommand = value;
        }

        private bool _isSettingViewShow = false;
        public bool IsSettingViewShow
        {
            get
            {
                return _isSettingViewShow;
            }
            set
            {
                if (_isSettingViewShow != value)
                {
                    _isSettingViewShow = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RelayCommand _showSettingViewCommand;
        public RelayCommand ShowSettingViewCommand
        {
            get { return _showSettingViewCommand; }
            set { _showSettingViewCommand = value; }
        }

        private RelayCommand<bool> _muteLocalVideo;
        public RelayCommand<bool> MuteLocalVideo
        {
            get
            {
                return _muteLocalVideo;
            }
        }

        private RelayCommand<bool> _muteMic;
        public RelayCommand<bool> MuteMic
        {
            get
            {
                return _muteMic;
            }
        }

        private DateTime _unmuteApplicationTime = DateTime.MinValue;

        private RelayCommand _startShareDesk;
        public RelayCommand StartShareDesk
        {
            get
            {
                return _startShareDesk;
            }
        }

        private bool _sendMeetingMsgEnabled = false;
        public bool SendMeetingMsgEnabled
        {
            get => _sendMeetingMsgEnabled;
            set { _sendMeetingMsgEnabled = value; RaisePropertyChanged("SendMeetingMsgEnabled"); }
        }

        private string _sendMeetingMsgText = string.Empty;
        public string SendMeetingMsgText
        {
            get => _sendMeetingMsgText;
            set
            {
                _sendMeetingMsgText = value;
                RaisePropertyChanged("SendMeetingMsgText");
            }
        }

        private bool _meetingMsgEnableScroll = false;
        public bool MeetingMsgEnableScroll
        {
            get => _meetingMsgEnableScroll;
            set { _meetingMsgEnableScroll = value; RaisePropertyChanged("MeetingMsgEnableScroll"); }
        }

        private string _meetingMsgRepeatTimes = "3";
        public string MeetingMessageRepeatTimes
        {
            get => _meetingMsgRepeatTimes;
            set
            {
                if (string.IsNullOrEmpty(value) || value == "0")
                    _meetingMsgRepeatTimes = "1";
                else
                    _meetingMsgRepeatTimes = value;
                RaisePropertyChanged("MeetingMessageRepeatTimes");
            }
        }

        private RelayCommand<string> _meetingMsgAddRepeatTimesCommand;
        public RelayCommand<string> MeetingMsgAddRepeatTimesCommand
        {
            get => _meetingMsgAddRepeatTimesCommand;
            set => _meetingMsgAddRepeatTimesCommand = value;
        }

        private int meetingMsgVerticalPos = 0;
        private RelayCommand<string> _meetingMsgSetPosCommand;
        public RelayCommand<string> SetMeetingMsgPosCommand
        {
            get => _meetingMsgSetPosCommand;
            set => _meetingMsgSetPosCommand = value;
        }

        private RelayCommand<string> _sendMeetingMsgCommand;
        public RelayCommand<string> SendMeetingMsgCommand
        {
            get => _sendMeetingMsgCommand;
            set => _sendMeetingMsgCommand = value;
        }

        private bool _enableRecordingAndStreaming = false;
        public bool EnableRecordingAndStreaming
        {
            get { return _enableRecordingAndStreaming; }
            set { _enableRecordingAndStreaming = value; RaisePropertyChanged("EnableRecordingAndStreaming"); }
        }

        private bool _isRecording = false;
        public bool IsRecording
        {
            get { return _isRecording; }
            set { _isRecording = value; RaisePropertyChanged("IsRecording"); }
        }

        private bool _isStreaming = false;
        public bool IsStreaming
        {
            get { return _isStreaming; }
            set { _isStreaming = value; RaisePropertyChanged("IsStreaming"); }
        }

        private string _shareStreamingText = string.Empty;
        public string ShareStreamingText
        {
            get { return _shareStreamingText; }
            set { _shareStreamingText = value; RaisePropertyChanged("ShareStreamingText"); }
        }

        private string _streamingUrl = string.Empty;
        public string StreamingUrl
        {
            get { return _streamingUrl; }
            set { _streamingUrl = value; RaisePropertyChanged("StreamingUrl"); }
        }

        private string _streamingPassword = string.Empty;
        public string StreamingPassword
        {
            get { return _streamingPassword; }
            set { _streamingPassword = value; RaisePropertyChanged("StreamingPassword"); }
        }

        private string _streamingPasswordInfo = string.Empty;
        public string StreamingPasswordInfo
        {
            get { return _streamingPasswordInfo; }
            set { _streamingPasswordInfo = value; RaisePropertyChanged("StreamingPasswordInfo"); }
        }

        private RelayCommand<string> _stopStreamingOrRecording;
        public RelayCommand<string> StopStreamingOrRecording
        {
            get => _stopStreamingOrRecording;
            set => _stopStreamingOrRecording = value;
        }

        private RelayCommand<bool> _enableRecordingCommand;
        public RelayCommand<bool> EnableRecordingCommand
        {
            get => _enableRecordingCommand;
            set => _enableRecordingCommand = value;
        }

        private RelayCommand<bool> _enableStreamingCommand;
        public RelayCommand<bool> EnableStreamingCommand
        {
            get => _enableStreamingCommand;
            set => _enableStreamingCommand = value;
        }

        private RelayCommand _popupStreamingInfoWindow;
        public RelayCommand PopupStreamingInfoWindow
        {
            get => _popupStreamingInfoWindow;
            set => _popupStreamingInfoWindow = value;
        }

        private RelayCommand _hideMyVideoCommand;
        public RelayCommand HideMyVideoCommand
        {
            get => _hideMyVideoCommand;
            set => _hideMyVideoCommand = value;
        }

        private bool _isRosterWndShow = false;
        public bool IsRosterWndShow
        {
            get { return _isRosterWndShow; }
            set { _isRosterWndShow = value; RaisePropertyChanged("IsRosterWndShow"); }
        }

        private RelayCommand _startStatiticsTimerCommand;
        public RelayCommand StartStatiticsTimerCommand
        {
            get
            {
                return _startStatiticsTimerCommand;
            }
        }

        private RelayCommand _stopStatiticsTimerCommand;
        public RelayCommand StoptStatiticsTimerCommand
        {
            get
            {
                return _stopStatiticsTimerCommand;
            }
        }

        private RelayCommand _popupStatisticsWnd;
        public RelayCommand PopupStatisticsWnd
        {
            get
            {
                return _popupStatisticsWnd;
            }
        }

        private RelayCommand _popupRosterWnd;
        public RelayCommand PopupRosterWnd
        {
            get
            {
                return _popupRosterWnd;
            }
        }

        private RelayCommand _copyMeetingInviteInfoCommand;
        public RelayCommand CopyMeetingInviteInfoCommand
        {
            get
            {
                return _copyMeetingInviteInfoCommand;
            }
        }


        private RelayCommand _popupInviteWnd;
        public RelayCommand PopupInviteWnd
        {
            get
            {
                return _popupInviteWnd;
            }
        }

        private bool _muteAllState = false;
        public bool MuteAllState
        {
            get { return _muteAllState; }
            set { _muteAllState = value; RaisePropertyChanged("MuteAllState"); }
        }

        private RelayCommand<string> _popupMuteAllDialogCommand;
        public RelayCommand<string> PopupMuteAllDialogCommand
        {
            get { return _popupMuteAllDialogCommand; }
            set { _popupMuteAllDialogCommand = value; }
        }

        private RelayCommand<bool> _muteAllCommand;
        public RelayCommand<bool> MuteAllCommand
        {
            get { return _muteAllCommand; }
            set { _muteAllCommand = value; }
        }

        private RelayCommand _CloseWnd;
        public RelayCommand CloseWnd
        {
            get
            {
                return _CloseWnd;
            }
        }

        private RelayCommand _popHangUpDialogCommand;
        public RelayCommand PopHangUpDialog
        {
            get { return _popHangUpDialogCommand; }
            set { _popHangUpDialogCommand = value; }
        }

        private RelayCommand<UIElement> _dropCallCommand;
        public RelayCommand<UIElement> DropCallCommand
        {
            get
            {
                return _dropCallCommand;
            }
        }

        private RelayCommand _finishCallCommand;
        public RelayCommand FinishCallCommand
        {
            get { return _finishCallCommand; }
            set { _finishCallCommand = value; }
        }

        private RelayCommand _reconnectCallCommand;
        public RelayCommand ReconnectCallCommand
        {
            get { return _reconnectCallCommand; }
            set { _reconnectCallCommand = value; }
        }

        public int LastReconnectState { get; set; }

        private RelayCommand _shareContent;
        public RelayCommand ShareContent
        {
            get
            {
                return _shareContent;
            }
        }

        private RelayCommand _stopContentCommand;
        public RelayCommand StopContentCommand
        {
            get
            {
                return _stopContentCommand;
            }
        }

        private RelayCommand _showContentPeopleCommand;
        public RelayCommand ShowContentPeopleCommand
        {
            get => _showContentPeopleCommand;
            set => _showContentPeopleCommand = value;
        }

        private RelayCommand _foldContentPeopleCommand;
        public RelayCommand FoldContentPeopleCommand
        {
            get => _foldContentPeopleCommand;
            set => _foldContentPeopleCommand = value;
        }

        private RelayCommand _hideSharingBarCommand;
        public RelayCommand HideSharingBarCommand
        {
            get => _hideSharingBarCommand;
        }

        private bool _IsShowNetSignal = true;
        public bool IsShowNetSignal
        {
            get
            {
                if (IsSmallWnd)
                    return false;
                return _IsShowNetSignal;
            }
            set
            {
                if (_IsShowNetSignal != value)
                {
                    _IsShowNetSignal = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _isShowToolBar = true;
        public bool IsShowToolBar
        {
            get
            {
                return _isShowToolBar;
            }
            set
            {
                if (_isShowToolBar != value)
                {
                    _isShowToolBar = value;
                    IsShowNetSignal = value;

                    if (IsSmallWnd)
                    {
                        IsShowMoreBtn = false;
                        IsShowCamralBtn = _isShowToolBar;
                    }
                    else
                    {
                        IsShowMoreBtn = _isShowToolBar;
                        if (_callRate > 0 && _callRate <= 64)
                        {
                            IsShowCamralBtn = false;
                        }
                        else
                        {
                            IsShowCamralBtn = _isShowToolBar;
                        }
                    }
                    RaisePropertyChanged("IsShowToolBar");
                }
            }
        }

        public bool IsSmallWnd = false;
        private bool _isShowMoreBtn = true;
        public bool IsShowMoreBtn
        {
            get
            {
                return _isShowMoreBtn;
            }
            set
            {
                if (_isShowMoreBtn != value)
                {
                    _isShowMoreBtn = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
