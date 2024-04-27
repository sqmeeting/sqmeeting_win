using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using SQMeeting.Model;
using SQMeeting.MvvMMessages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using SQMeeting.Model.DataObj;
using System.IO;
using System.Text;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using CommonServiceLocator;
using SQMeeting.Properties;
using GalaSoft.MvvmLight.Ioc;
using Newtonsoft.Json.Linq;
using SQMeeting.LogTool;
using SQMeeting.FRTCView;
using System.Security.Policy;
using System.Linq;
using SQMeeting.Utilities;

namespace SQMeeting.ViewModel
{
    public enum FrtcMeetingViews
    {
        FRTCLaunchingWindow,
        FRTCGuestWindow,
        FRTCMainWindow,
        FRTCSettingWindow,
        SignInView,
        LaunchView,
        ServerAddrSettingView,
        JoinMeetingView,
        InputPassCodeView,
        SettingView,
        AccountView,
        FRTCGuestHomeView,
        FRTCSettingView,
        FRTCSignInView,
        FRTCJoinMeetingView
    }

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    /// 
    public class MainViewModel : FRTCViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            //DateTime t0 = DateTime.Now;
            m_signInMgr = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<FRTCUserManager>();

            this.MessengerInstance.Register<FRTCViewNavigatorMessage>(this, NavigateToFRTCView);
            this.MessengerInstance.Register<FRTCTipsMessage>(this, ShowTipMsg);
            this.MessengerInstance.Register<FRTCCallStateChangeMessage>(this, OnCallStateChange);
            this.MessengerInstance.Register<FRTCAPIResultMessage>(this, OnSignInResult);

            this._frtcPopupGuestJoinMeetingDialog = new RelayCommand(() =>
            {
                string servAddr = ConfigurationManager.AppSettings["FRTCServerAddress"];
                NoServerAddress = string.IsNullOrEmpty(servAddr);
                if (!NoServerAddress)
                {
                    ShowMask = true;
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCJoinMeeting, null);
                    ShowMask = false;
                }
            });

            this._backwardCommand = new RelayCommand(() =>
            {
                this.NavigateBackward();
            });

            this._guestJoinMeetingViewCommand = new RelayCommand<string>((p) =>
            {
                this.TipMsgText = string.Empty;
                if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["FRTCServerAddress"]))
                {
                    this.NavigateToFRTCView(new FRTCViewNavigatorMessage()
                    { SourceView = FrtcMeetingViews.LaunchView, TargetView = FrtcMeetingViews.ServerAddrSettingView, Param = p });
                    this.TipMsgText = Properties.Resources.FRTC_MEETING_SDKAPP_TIP_SETCALLSERVER;
                }
                else
                {
                    this.NavigateToFRTCView(new FRTCViewNavigatorMessage()
                    { SourceView = FrtcMeetingViews.LaunchView, TargetView = FrtcMeetingViews.JoinMeetingView, Param = p });
                }
            });

            this._signInViewCommand = new RelayCommand(() =>
            {
                string servAddr = ConfigurationManager.AppSettings["FRTCServerAddress"];
                NoServerAddress = string.IsNullOrEmpty(servAddr);
                if (!NoServerAddress)
                {
                    this.NavigateToFRTCView(new FRTCViewNavigatorMessage()
                    { SourceView = FrtcMeetingViews.FRTCGuestHomeView, TargetView = FrtcMeetingViews.FRTCSignInView });
                }
            });

            this._settingViewCommand = new RelayCommand(() =>
            {
                this.TipMsgText = string.Empty;
                this.NavigateToFRTCView(new FRTCViewNavigatorMessage()
                { SourceView = this._currentView, TargetView = FrtcMeetingViews.FRTCSettingWindow });
            });

            this._accountViewCommand = new RelayCommand(() =>
            {
                this.TipMsgText = string.Empty;
                this.NavigateToFRTCView(new FRTCViewNavigatorMessage()
                { SourceView = this._currentView, TargetView = FrtcMeetingViews.AccountView });
                if (!m_signInMgr.IsUserSignIn)
                {
                    m_signInMgr.SignInViaToken();
                }
            });

            this.m_navigatorStack.Push(this.CurrentView);

            FrtcCallInitParam frtcCallParam = new FrtcCallInitParam();

            string uuid = FRTCUIUtils.GetFRTCDeviceUUID();

            frtcCallParam.uuid = Marshal.StringToHGlobalAnsi(uuid);

            string addr = ConfigurationManager.AppSettings["FRTCServerAddress"];
            frtcCallParam.serverAddress = Marshal.StringToHGlobalAnsi(addr);

            string prdName = Resources.FRTC_MEETING_SDKAPP_MAINCAPTION;
            frtcCallParam.callWndTitle = Marshal.StringToHGlobalAnsi(prdName);

            frtcCallParam.funcCallStateChangedCB = FRTCCallManager.callStateChangecallback;
            frtcCallParam.funcCallPasswordProcessCB = FRTCCallManager.passCodeCallback;
            frtcCallParam.pfnOnMeetingCtrlMsg = FRTCMeetingVideoViewModel.pfnMeetingControlMsgCallback;
            frtcCallParam.funcContentShareStateCB = FRTCMeetingVideoViewModel.pfnContentSendingState;
            frtcCallParam.funcReminderNotifyCB = FRTCMeetingVideoViewModel.pfnOnReminderNotifyCallback;
            frtcCallParam.funcWndMouseEvent = FRTCMeetingVideoViewModel.pfnVideoEventCallBack;

            FRTCSDK.frtc_sdk_init(frtcCallParam);

            string enableNoiseBlocker = ConfigurationManager.AppSettings["EnableNoiseBlocker"];
            if (enableNoiseBlocker == null)
            {
                enableNoiseBlocker = "true";
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings.Add("EnableNoiseBlocker", enableNoiseBlocker);
                    config.Save(ConfigurationSaveMode.Modified, true);
                    ConfigurationManager.RefreshSection("appSettings");
                    config = null;
                }
                catch (ConfigurationErrorsException e)
                {
                    LogHelper.Exception(e);
                }
            }
            FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_ENABLE_NOISE_BLOCKER, enableNoiseBlocker);

            this.TitleVisibility = Visibility.Hidden;
            this.Title = "SQMeeting_Windows_" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private Stack<FrtcMeetingViews> m_navigatorStack = new Stack<FrtcMeetingViews>(5);

        private FrtcCallState currentCallState = FrtcCallState.DISCONNECTED;

        private void OnCallStateChange(FRTCCallStateChangeMessage msg)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                currentCallState = msg.callState;
                if (msg.callState == FrtcCallState.CONNECTING)
                {
                    IsProcessing = true;
                    this.MeetingID = Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGID_COLON + " " + msg.meetingId;
                }
                else if (msg.callState == FrtcCallState.CONNECTED)
                {
                    this.MeetingID = Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGID_COLON + " " + msg.meetingId;
                    this.MeetingName = msg.meetingName;
                    IsProcessing = false;
                    PreventSystemSleep(true);

                }
                else if (msg.callState == FrtcCallState.DISCONNECTING || msg.callState == FrtcCallState.DISCONNECTED)
                {
                    LogHelper.Debug("Got call disconnected msg");
                    this.MeetingID = string.Empty;
                    IsProcessing = false;
                    PreventSystemSleep(false);
                    App.Current.MainWindow.Activate();
                    if (msg.callState == FrtcCallState.DISCONNECTED && msg.reason == FrtcCallReason.CALL_SUCCESS)
                    {
                        MessengerInstance.Send<OnFRTCViewShownMessage>(new OnFRTCViewShownMessage() { View = IsSignIn ? FrtcMeetingViews.FRTCMainWindow : FrtcMeetingViews.FRTCGuestHomeView });
                    }
                }
                else
                {
                    IsProcessing = false;
                }
            });
        }

        private void OnSignInResult(FRTCAPIResultMessage msg)
        {
            if (msg.Result == FRTC_API_RESULT.SIGNIN_SESSION_RESET)
            {
                _signInReset = true;
            }
            else
            {
                _signInReset = false;
            }
            if (msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS || msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN)
                IsSignIn = true;
            else if (msg.Result == FRTC_API_RESULT.SIGNOUT_SUCCESS)
                IsSignIn = false;
            IsProcessing = false;
            RaisePropertyChanged("CanViewAccount");
        }

        private void NavigateToFRTCView(FRTCViewNavigatorMessage msg)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (msg.TargetView == FrtcMeetingViews.FRTCLaunchingWindow
                    || msg.TargetView == FrtcMeetingViews.FRTCGuestWindow
                    || msg.TargetView == FrtcMeetingViews.FRTCMainWindow
                    || msg.TargetView == FrtcMeetingViews.FRTCSettingWindow)
                {
                    FRTCNavigateToWindow(msg);
                    return;
                }
                else
                {
                    if (this.m_navigatorStack.Peek() != msg.SourceView
                    && msg.SourceView != FrtcMeetingViews.ServerAddrSettingView
                    && msg.SourceView != FrtcMeetingViews.InputPassCodeView)
                    {
                        if (msg.SourceView == FrtcMeetingViews.SignInView && msg.TargetView != FrtcMeetingViews.SettingView)
                        {
                            //Don't push
                        }
                        else
                        {
                            m_navigatorStack.Push(msg.SourceView);
                        }
                    }
                    this.CurrentView = msg.TargetView;
                    MessengerInstance.Send<OnFRTCViewShownMessage>(new OnFRTCViewShownMessage() { View = CurrentView, Param = msg.Param });
                    RaisePropertyChanged("CanDoBackward");
                    RaisePropertyChanged("CanDoSetting");
                    RaisePropertyChanged("CanViewAccount");
                }
            });
        }

        private void FRTCNavigateToWindow(FRTCViewNavigatorMessage msg)
        {
            Window current = App.Current.MainWindow;
            switch (msg.TargetView)
            {
                case FrtcMeetingViews.FRTCLaunchingWindow:
                    break;
                case FrtcMeetingViews.FRTCGuestWindow:
                    if (current != null && current is FRTCView.GuestWindow)
                        return;
                    current.Close();
                    App.Current.MainWindow = new FRTCView.GuestWindow();
                    App.Current.MainWindow.Show();
                    this.CurrentView = FrtcMeetingViews.FRTCGuestHomeView;
                    string callUri = msg.Param as string;
                    if (!string.IsNullOrEmpty(callUri))
                    {
                        this.IsProcessing = true;
                        this.CheckSchemaMsg(callUri);
                    }
                    break;
                case FrtcMeetingViews.FRTCMainWindow:
                    current.Close();
                    App.Current.MainWindow = new FRTCView.SignedMainWindow();
                    App.Current.MainWindow.Show();
                    this.CurrentView = FrtcMeetingViews.FRTCMainWindow;
                    string callUriSignin = msg.Param as string;
                    if (!string.IsNullOrEmpty(callUriSignin))
                        this.CheckSchemaMsg(callUriSignin);
                    break;
                case FrtcMeetingViews.FRTCSettingWindow:
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCSetting, new object[] { Application.Current.MainWindow });
                    break;
                default:
                    break;
            }
        }

        void AddMeetingToMyList(string meetingUrl)
        {
            if (string.IsNullOrEmpty(meetingUrl))
            {
                return;
            }
            Uri uri = new Uri(meetingUrl);
            string meetingServer = uri.Host + ":" + uri.Port;

            Window parent = null;
            if (currentCallState == FrtcCallState.CONNECTED)
            {
                parent = Utilities.FRTCUIUtils.MeetingWindow;
            }
            else
            {
                parent = Application.Current.MainWindow;
            }

            if (!m_signInMgr.IsUserSignIn)
            {
                if (parent != null)
                {
                    parent.Dispatcher.Invoke(new Action(() =>
                    {
                        FRTCMessageBox.ShowNotificationMessage(Resources.FRTC_SDKAPP_ADD_MEEETING_TO_LIST_FAILED, Resources.FRTC_SDKAPP_ADD_MEETING_NEED_LOGIN, string.Empty, parent);
                    }));
                }
                return;
            }
            if (meetingServer != ConfigurationManager.AppSettings["FRTCServerAddress"])
            {
                if (parent != null)
                {
                    parent.Dispatcher.Invoke(new Action(() =>
                    {
                        FRTCMessageBox.ShowNotificationMessage(Resources.FRTC_SDKAPP_ADD_MEEETING_TO_LIST_FAILED, Resources.FRTC_SDKAPP_ADD_MEETING_SERVER_WRONG, string.Empty, parent);
                    }));
                }
                return;
            }
            string identifier = uri.Segments.Last();
            LogHelper.Info(identifier);
            ServiceLocator.Current.GetInstance<MeetingScheduleManager>().AddMeetingToList(identifier);
        }

        public void CheckSchemaMsg(string msg)
        {
            LogTool.LogHelper.DebugMethodEnter();
            LogTool.LogHelper.Debug("uri schema {0}", msg);
            if (!string.IsNullOrEmpty(msg))
            {
                string name = string.Empty;
                string storedName = ConfigurationManager.AppSettings["GuestName"];
                if (!string.IsNullOrEmpty(storedName))
                {
                    name = storedName;
                }
                else
                {
                    name = Environment.UserName;
                    name = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(name));
                }
                string userToken = string.Empty;
                string addr = ConfigurationManager.AppSettings["FRTCServerAddress"];
                JObject meetingInfo = null;
                string meeting_num = string.Empty;
                string meeting_pwd = string.Empty;
                string serverInUri = string.Empty;

                if (TryGetSchemaMsgParamPlainText(msg, out meetingInfo) && meetingInfo != null)
                {
                    try
                    {
                        if (meetingInfo.ContainsKey("operation"))
                        {
                            HandleAddToMeetingListMsg(meetingInfo);
                            this.IsProcessing = false;
                            return;
                        }
                        else
                        {
                            name = meetingInfo["username"].ToString();
                            serverInUri = meetingInfo["FRTCServerAddress"].ToString();
                            meeting_num = meetingInfo["meeting_number"].ToString();
                            meeting_pwd = meetingInfo["meeting_passwd"].ToString();
                        }
                    }
                    catch (Exception eeex) { LogHelper.Exception(eeex); return; }

                    if (this.currentCallState == FrtcCallState.DISCONNECTED)
                    {
                        if (m_signInMgr.IsUserSignIn)
                        {
                            if (serverInUri == addr)
                            {
                                name = m_signInMgr.UserData.real_name;
                                userToken = m_signInMgr.UserData.user_token;
                            }
                        }
                        else
                        {
                            if (serverInUri != addr)//compare server address in url with current server address
                            {
                                string tipmsg = string.Format(Properties.Resources.FRTC_MEETING_SAVE_ADDRESS_IN_URL, serverInUri);
                                App.Current.MainWindow.Activate();
                                bool? ret = FRTCView.FRTCMessageBox.ShowLongConfirmMessage(tipmsg, Properties.Resources.FRTC_MEETING_CONTACT_ADMIN_IF_CONFUSED);
                                if (ret.HasValue)
                                {
                                    if (ret.Value)
                                    {
                                        try
                                        {
                                            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                                            config.AppSettings.Settings["FRTCServerAddress"].Value = serverInUri;
                                            config.Save(ConfigurationSaveMode.Modified, true);
                                            ConfigurationManager.RefreshSection("appSettings");
                                            config = null;
                                            CommonServiceLocator.ServiceLocator.Current.GetInstance<SettingViewModel>().ServerAddress = serverInUri;
                                            MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_SETTING_SAVE_SUCCEED });
                                        }
                                        catch (ConfigurationErrorsException e)
                                        {
                                            LogHelper.Exception(e);
                                        }
                                    }
                                }
                                else if (ret == null)
                                {
                                    return;
                                }
                            }
                        }
                        JoinFRTCMeetingMsg msgObj = new JoinFRTCMeetingMsg()
                        {
                            isPlainTextURLJoin = true,
                            serverAddress = serverInUri,
                            callRate = 0,
                            confAlias = meeting_num,
                            displayName = name,
                            preMuteCamera = true,
                            preMuteMic = true,
                            userToken = addr == serverInUri ? userToken : string.Empty,
                            passCode = meeting_pwd,
                            isSelfOwnedMeeting = false,
                            isVoiceOnlyMeeting = false
                        };
                        MessengerInstance.Send<JoinFRTCMeetingMsg>(msgObj);
                    }
                    else
                    {
                        if (this.currentCallState == FrtcCallState.CONNECTED)
                        {
                            MessengerInstance.Send(new NotificationMessage("join_other_meeting_notify"));
                        }
                        this.IsProcessing = false;
                    }
                }
            }
            ((App)Application.Current).StartupArgSchemaString = string.Empty;
        }

        public bool TryGetSchemaMsgParamPlainText(string schemaMsg, out JObject schemaMsgParam)
        {
            string plaintextUri = string.Empty;
            JObject meetingInfo = null;
            try
            {
                plaintextUri = Encoding.UTF8.GetString(Convert.FromBase64String(schemaMsg));
                meetingInfo = JObject.Parse(plaintextUri);
            }
            catch
            { plaintextUri = string.Empty; meetingInfo = null; }


            schemaMsgParam = meetingInfo;

            return meetingInfo != null;
        }

        public void HandleAddToMeetingListMsg(JObject meetingInfo)
        {
            string op = meetingInfo["operation"].ToString();
            if (op == "meeting_save")
            {
                string url = meetingInfo["meeting_url"].ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    AddMeetingToMyList(url);
                }
                return;
            }
        }

        private void ShowTipMsg(FRTCTipsMessage msg)
        {
            this.TipMsgText = msg.TipMessage;
        }

        private void NavigateBackward()
        {
            this.TipMsgText = string.Empty;
            if (CurrentView == FrtcMeetingViews.SignInView)
            {
                MessengerInstance.Send<NotificationMessage>(new NotificationMessage("SignInViewBackward"));
            }
            else if (m_navigatorStack.Count > 1)
            {
                this.CurrentView = m_navigatorStack.Pop();
                MessengerInstance.Send<OnFRTCViewShownMessage>(new OnFRTCViewShownMessage() { View = CurrentView });
                RaisePropertyChanged("CanDoBackward");
                RaisePropertyChanged("CanDoSetting");
                RaisePropertyChanged("CanViewAccount");
            }
        }

        public string Version
        {
            get
            {
                return Properties.Resources.FRTC_MEETING_SDKAPP_VERSION + ":" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        private FrtcMeetingViews _currentView = FrtcMeetingViews.LaunchView;
        public FrtcMeetingViews CurrentView
        {
            get
            {
                return _currentView;
            }
            private set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    RaisePropertyChanged();
                    switch (_currentView)
                    {
                        case FrtcMeetingViews.LaunchView:
                            Caption = "";
                            break;
                        case FrtcMeetingViews.SignInView:
                            Caption = Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN;
                            break;
                        case FrtcMeetingViews.ServerAddrSettingView:
                            Caption = Properties.Resources.FRTC_MEETING_SDKAPP_CAPTION_SERVERADDR;

                            break;
                        case FrtcMeetingViews.JoinMeetingView:
                            Caption = Properties.Resources.FRTC_MEETING_SDKAPP_CAPTION_JOIN;
                            break;
                        case FrtcMeetingViews.SettingView:
                            Caption = Properties.Resources.FRTC_MEETING_SDKAPP_CAPTION_SETTING;
                            break;
                        case FrtcMeetingViews.AccountView:
                            Caption = Properties.Resources.FRTC_MEETING_SDKAPP_CAPTION_ACCOUNT;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private string _caption = "";
        public string Caption
        {
            get
            {
                return _caption;
            }
            private set
            {
                if (_caption != value)
                {
                    _caption = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _tipMsgText = string.Empty;
        public string TipMsgText
        {
            get
            {
                return _tipMsgText;
            }
            private set
            {
                if (_tipMsgText != value)
                {
                    _tipMsgText = value;
                    RaisePropertyChanged();
                    //if(!string.IsNullOrEmpty(_tipMsgText))
                    //{
                    //    Task.Run(new Action(() =>
                    //    {
                    //        System.Threading.Thread.Sleep(3000);
                    //        App.Current.Dispatcher.Invoke(() =>
                    //        {
                    //            TipMsgText = string.Empty;
                    //        });
                    //    }));
                    //}
                }
            }
        }

        public bool CanDoBackward
        {
            get
            {
                return _currentView == FrtcMeetingViews.SignInView
                    || _currentView == FrtcMeetingViews.ServerAddrSettingView
                    || (_currentView == FrtcMeetingViews.JoinMeetingView)
                    || _currentView == FrtcMeetingViews.SettingView
                    || _currentView == FrtcMeetingViews.AccountView;
            }
        }

        public bool CanDoSetting
        {
            get
            {
                return _currentView == FrtcMeetingViews.LaunchView
                    || _currentView == FrtcMeetingViews.SignInView
                    || _currentView == FrtcMeetingViews.JoinMeetingView;
            }
        }

        public bool CanViewAccount
        {
            get
            {
                return _currentView == FrtcMeetingViews.LaunchView && (m_signInMgr.IsUserSignIn);
            }
        }

        private bool _noServerAddress = false;
        public bool NoServerAddress
        {
            get { return _noServerAddress; }
            set
            {
                _noServerAddress = value;
                RaisePropertyChanged("NoServerAddress");
            }
        }

        private FRTCUserManager m_signInMgr;

        private bool _signInReset = false;

        private bool _isSignIn = false;
        public bool IsSignIn
        {
            get
            {
                return _isSignIn;
            }
            set
            {
                if (_isSignIn != value)
                {
                    _isSignIn = value;
                    RaisePropertyChanged();
                }
            }
        }


        private bool _isProcessing = false;
        public bool IsProcessing
        {
            get
            {
                return _isProcessing;
            }
            private set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RelayCommand _backwardCommand;
        public RelayCommand BackwardCommand
        {
            get
            {
                return _backwardCommand;
            }
        }

        private RelayCommand<string> _guestJoinMeetingViewCommand;
        public RelayCommand<string> GuestJoinMeetingViewCommand
        {
            get
            {
                return _guestJoinMeetingViewCommand;
            }
        }

        private RelayCommand _frtcPopupGuestJoinMeetingDialog;
        public RelayCommand FRTCPopupGuestJoinMeetingDialog
        {
            get
            {
                return _frtcPopupGuestJoinMeetingDialog;
            }
        }


        private RelayCommand _signInViewCommand;
        public RelayCommand SignInViewCommand
        {
            get
            {
                return _signInViewCommand;
            }
        }

        private RelayCommand _settingViewCommand;
        public RelayCommand SettingViewCommand
        {
            get
            {
                return _settingViewCommand;
            }
        }

        private RelayCommand _accountViewCommand;
        public RelayCommand AccountViewCommand
        {
            get
            {
                return _accountViewCommand;
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

        const uint ES_AWAYMODE_REQUIRED = 0x00000040;
        const uint ES_CONTINUOUS = 0x80000000;
        const uint ES_SYSTEM_REQUIRED = 0x00000001;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        private void PreventSystemSleep(bool prevent)
        {
            uint flag = prevent ? ES_DISPLAY_REQUIRED | ES_CONTINUOUS | ES_SYSTEM_REQUIRED : ES_CONTINUOUS;
            App.Current.Dispatcher.Invoke(() =>
            {
                Win32API.SetThreadExecutionState(flag);
            });
        }
    }
}