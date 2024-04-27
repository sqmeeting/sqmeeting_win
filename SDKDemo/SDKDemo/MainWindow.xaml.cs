using SDKDemo.Model;
using SDKDemo.MvvMMessages;
using SDKDemo.Utilities;
using Microsoft.VisualBasic;
using System;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Threading;
using System.Xml.Linq;

namespace SDKDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        FRTCUserManager m_signInMgr = new FRTCUserManager();
        FRTCCallManager m_callMgr = new FRTCCallManager();

        string token_ = string.Empty;

        bool _joinAfterSignOut = false;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += (s, e) => { FRTCSDK.frtc_call_leave(); App.Current.Shutdown(); };
        }

        static OnMeetingControlMsgCallback pfnOnMeetingControlMsgCallback;
        static OnContentSendingState pfnOnContentSendingState;
        static OnWndMouseEvent pfnOnWndMouseEvent;
        static OnReminderNotify pfnOnReminderNotify;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbServerAddr.TextChanged += TbServerAddr_TextChanged;

            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<FRTCCallStateChangeMessage>(this, OnCallStateChangeMessage);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<FRTCAPIResultMessage>(this, OnSignInResultMessage);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<FRTCMeetingPasswordMessage>(this, OnMeetingPasswordRequest);

            pfnOnMeetingControlMsgCallback = new OnMeetingControlMsgCallback(OnMeetingControlMessage);
            pfnOnContentSendingState = new OnContentSendingState(OnContentSendingState);
            pfnOnWndMouseEvent = new OnWndMouseEvent(OnWndMouseEventCB);
            pfnOnReminderNotify = new OnReminderNotify(OnReminderNotify);

            FrtcCallInitParam frtcCallParam = new FrtcCallInitParam();

            string uuid = FRTCUIUtils.GetFRTCDeviceUUID();
            frtcCallParam.uuid = Marshal.StringToHGlobalAnsi(uuid);

            string addr = ConfigurationManager.AppSettings["FRTCServerAddress"];
            if (string.IsNullOrEmpty(addr))
            {
                addr = "";
            }
            SaveConfig("FRTCServerAddress", addr);
            tbServerAddr.Text = addr;

            frtcCallParam.serverAddress = Marshal.StringToHGlobalAnsi(addr);

            string prdName = "frtcsdk_sample";
            frtcCallParam.callWndTitle = Marshal.StringToHGlobalAnsi(prdName);

            frtcCallParam.funcCallStateChangedCB = FRTCCallManager.callStateChangecallback;
            frtcCallParam.funcCallPasswordProcessCB = FRTCCallManager.passCodeCallback;
            frtcCallParam.pfnOnMeetingCtrlMsg = pfnOnMeetingControlMsgCallback;
            frtcCallParam.funcContentShareStateCB = pfnOnContentSendingState;
            frtcCallParam.funcReminderNotifyCB = pfnOnReminderNotify;
            frtcCallParam.funcWndMouseEvent = pfnOnWndMouseEvent;

            FRTCSDK.frtc_sdk_init(frtcCallParam);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbServerAddr.Text.Trim()))
            {
                emptyServerAddrError.Visibility = Visibility.Visible;
                return;
            }
            SaveConfig("FRTCServerAddress", tbServerAddr.Text.Trim());
            RESTEngine.SetBaseUrl(tbServerAddr.Text.Trim());
            if (m_signInMgr.IsUserSignIn)
            {
                m_signInMgr.SignOut();
                _joinAfterSignOut = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(tbUserName.Text.Trim()))
                {
                    if (!string.IsNullOrEmpty(pbSignIn.Password.Trim()))
                    {
                        DoSignIn(tbUserName.Text.Trim(), pbSignIn.Password.Trim());
                    }
                    else
                    {
                        JoinMeeting();
                    }
                }
            }
        }

        private void TbServerAddr_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbServerAddr.Text.Trim()))
            {
                emptyServerAddrError.Visibility = Visibility.Visible;
            }
            else
            {
                emptyServerAddrError.Visibility = Visibility.Collapsed;
            }
        }

        private void DoSignIn(string username, string password)
        {
            m_signInMgr.SignIn(username, password);
        }

        private void JoinMeeting()
        {
            SaveConfig("FRTCServerAddress", tbServerAddr.Text.Trim());
            m_callMgr.JoinMeeting(tbMeetingNumber.Text.Trim(), tbUserName.Text.Trim(), 0, false, false, token_, pbMeeting.Password.Trim());
        }

        Thread videoWindowThread = null;
        MeetingWindowHost videoViewHost = null;
        SampleMeetingWindow sampleMeetingWindow = null;
        void ShowMeetingWindow()
        {
            videoWindowThread = new Thread(new ThreadStart(() =>
            {
                videoViewHost = new MeetingWindowHost(OnJoinMeeting);
                sampleMeetingWindow = new SampleMeetingWindow(videoViewHost);
                sampleMeetingWindow.Closing += (s, e) =>
                {
                    if (m_callMgr.CurrentCallState == FrtcCallState.DISCONNECTED)
                    {
                        e.Cancel = false;
                    }
                    else
                    {
                        e.Cancel = true;
                        m_callMgr.DropCall();
                    }
                };
                sampleMeetingWindow.Closed += (s, e) => { Dispatcher.CurrentDispatcher?.InvokeShutdown(); };

                sampleMeetingWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                //sampleMeetingWindow.Init();

                sampleMeetingWindow.Show();

                sampleMeetingWindow.Topmost = true;
                sampleMeetingWindow.Topmost = false;

                sampleMeetingWindow.Activate();

                System.Windows.Threading.Dispatcher.Run();

                if (sampleMeetingWindow != null)
                {
                    sampleMeetingWindow.Close();
                }
                return;
            }));
            videoWindowThread.SetApartmentState(ApartmentState.STA);
            videoWindowThread.Start();
        }

        private IntPtr OnJoinMeeting(IntPtr hostHwnd)
        {
            IntPtr hwnd = FRTCSDK.frtc_parent_hwnd_set(hostHwnd);
            return hwnd;
        }

        private void OnCallStateChangeMessage(FRTCCallStateChangeMessage msg)
        {
            if (msg.callState == FrtcCallState.CONNECTED)
            {
                ShowMeetingWindow();
            }
            else if (msg.callState == FrtcCallState.DISCONNECTED)
            {
                if (videoWindowThread != null)
                {
                    Dispatcher.FromThread(videoWindowThread).Invoke(() =>
                    {
                        if (sampleMeetingWindow != null)
                        {
                            sampleMeetingWindow.Close();
                        }
                    });
                }
                if (msg.reason == FrtcCallReason.CALL_AUTH_FAILED)
                {
                    MessageBox.Show("验证登录状态失败");
                }
                else if (msg.reason == FrtcCallReason.CALL_GUEST_NOT_ALLOW)
                {
                    MessageBox.Show("需要登录");
                }
                else if (msg.reason == FrtcCallReason.CALL_PASSWORD_FAILED)
                {
                    MessageBox.Show("需要会议密码");
                }
                else
                {
                    MessageBox.Show(Enum.GetName<FrtcCallReason>(msg.reason));
                }
            }
        }

        private void OnSignInResultMessage(FRTCAPIResultMessage msg)
        {
            if (msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS)
            {
                token_ = m_signInMgr.UserData.user_token;
                this.Dispatcher.BeginInvoke(() =>
                {
                    JoinMeeting();
                });

            }
            else if (msg.Result == FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD)
            {
                MessageBox.Show("用户登录失败,密码错误");
            }
            else if (msg.Result == FRTC_API_RESULT.SIGNIN_FAILED_PWD_LOCKED)
            {
                MessageBox.Show("用户登录失败,已被锁定");
            }
            else if(msg.Result == FRTC_API_RESULT.SIGNOUT_SUCCESS)
            {
                if(_joinAfterSignOut)
                {
                    _joinAfterSignOut = false;
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        if (!string.IsNullOrEmpty(tbUserName.Text.Trim()))
                        {
                            if (!string.IsNullOrEmpty(pbSignIn.Password.Trim()))
                            {
                                DoSignIn(tbUserName.Text.Trim(), pbSignIn.Password.Trim());
                            }
                            else
                            {
                                JoinMeeting();
                            }
                        }
                    });
                }
            }
        }

        bool _newPwdTry = true;
        private void OnMeetingPasswordRequest(FRTCMeetingPasswordMessage msg)
        {
            if (msg.reason == "reject")
            {
                MessageBox.Show("会议密码错误次数过多");
                return;
            }
            else if (msg.reason == "timeout")
            {
                MessageBox.Show("输入会议密码超时");
                return;
            }
            else
            {
                if (_newPwdTry)
                {
                    _newPwdTry = false;
                    if (!m_signInMgr.IsUserSignIn && !string.IsNullOrEmpty(this.pbMeeting.Password.Trim()))
                    {
                        FRTCSDK.frtc_call_password_send(this.pbMeeting.Password.Trim());
                        return;
                    }
                    else
                    {
                        MessageBox.Show("会议密码错误");
                    }
                }
                else
                {
                    MessageBox.Show("会议密码错误");
                    _newPwdTry = true;
                    return;
                }
            }
            Task.Run(() => FRTCSDK.frtc_call_leave());
            _newPwdTry = true;
        }

        StringBuilder txt = new StringBuilder();
        long lastUpdateTime = DateAndTime.Now.Ticks;
        private void OnMeetingControlMessage(IntPtr message)
        {
            string msgStr = FRTCUIUtils.StringFromNativeUtf8(message);
            if (videoWindowThread != null && sampleMeetingWindow != null)
            {
                Dispatcher.FromThread(videoWindowThread).BeginInvoke(() =>
                {
                    try
                    {
                        if (txt.Length > 100000 && msgStr.Length < txt.Length - 1)
                        {
                            txt.Remove(0, msgStr.Length);
                        }
                        txt.AppendLine(msgStr);
                        if (DateAndTime.Now.Ticks - lastUpdateTime > 10000000)
                        {
                            try
                            {
                                if (sampleMeetingWindow != null)
                                {
                                    sampleMeetingWindow.tbMsg.Text = txt.ToString();
                                    lastUpdateTime = DateAndTime.Now.Ticks;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                });
            }
        }

        private void OnContentSendingState(bool isSending)
        {
            if (videoWindowThread != null && sampleMeetingWindow != null)
            {
                Dispatcher.FromThread(videoWindowThread).Invoke(() =>
            {
                sampleMeetingWindow.tbMsg.Text += (string.Format("Content sending status changed to {0}", isSending ? "sending" : "stopped") + Environment.NewLine + Environment.NewLine);

            });
            }
        }

        private void OnWndMouseEventCB(SDKVIDEOEVENT type)
        {
            if (videoWindowThread != null && sampleMeetingWindow != null)
            {
                Dispatcher.FromThread(videoWindowThread).Invoke(() =>
            {
                sampleMeetingWindow.tbMsg.Text += (string.Format("OnWndMouseEventCB event type is {0}", type.ToString()) + Environment.NewLine + Environment.NewLine);

            });
            }
        }

        private void OnReminderNotify(FrtcReminderType type)
        {
            if (videoWindowThread != null && sampleMeetingWindow != null)
            {
                Dispatcher.FromThread(videoWindowThread).Invoke(() =>
            {
                sampleMeetingWindow.tbMsg.Text += (string.Format("OnReminderNotify event type is {0}", type.ToString()) + Environment.NewLine + Environment.NewLine);

            });
            }
        }

        private void SaveConfig(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                {
                    config.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    config.AppSettings.Settings.Add(key, value);
                }
                config.Save(ConfigurationSaveMode.Modified, true);
                ConfigurationManager.RefreshSection("appSettings");
                config = null;
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine("[Exception error: {0}]",
                    e.ToString());
            }
        }
    }
}
