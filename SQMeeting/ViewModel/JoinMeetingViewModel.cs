using SQMeeting.FRTCView;
using SQMeeting.LogTool;
using SQMeeting.Model;
using SQMeeting.MvvMMessages;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using WPFMediaKit.DirectShow.Controls;

namespace SQMeeting.ViewModel
{
    public class JoinMeetingViewModel : FRTCViewModelBase
    {
        private bool m_initialized = false;

        private FRTCCallManager m_callManager;
        private FRTCUserManager m_signInManager;
        private DeviceManager m_deviceMgr;

        private bool _meetingPWDFirstTime = true;


        public JoinMeetingViewModel()
        {
            this.ShutdownAfterClose = false;

            m_deviceMgr = SimpleIoc.Default.GetInstance<DeviceManager>();
            m_callManager = SimpleIoc.Default.GetInstance<FRTCCallManager>();
            m_signInManager = SimpleIoc.Default.GetInstance<FRTCUserManager>();

            MessengerInstance.Register<FRTCWindowStateChangedMessage>(this, OnWindowStateChanged);
            MessengerInstance.Register<OnFRTCViewShownMessage>(this, OnShow);
            MessengerInstance.Register<FRTCAPIResultMessage>(this, OnAPIResult);
            MessengerInstance.Register<FRTCMeetingScheduledMessage>(this, new Action<FRTCMeetingScheduledMessage>((msg) =>
            {
                if (msg.MeetingData.meeting_type == "instant")
                {
                    this.MeetingID = msg.MeetingData.meeting_number;
                    this.FRTCMeetingPWD = msg.MeetingData.meeting_password;
                    this._selfOwnedMeeting = true;
                    this.JoinMeetingAsOwner();
                }
            }));

            MessengerInstance.Register<JoinMeetingFromHistoryOrScheduleList>(this, new Action<JoinMeetingFromHistoryOrScheduleList>((msg) =>
            {
                this.MeetingID = msg.MeetingData.meeting_number;
                this.FRTCMeetingPWD = msg.MeetingData.meeting_password;
                this._selfOwnedMeeting = msg.MeetingData.owner_id == m_signInManager.UserData.user_id;
                this.CurrentMeetingBeginTime = msg.MeetingData.schedule_start_time;
                this.CurrentMeetingEndTime = msg.MeetingData.schedule_end_time;
                this.IsVoiceOnly = false;
                this.MutedJoinMeetingAsSignedUser();
            }));


            MessengerInstance.Register<FRTCCallStateChangeMessage>(this, OnFRTCCallStateChanged);
            MessengerInstance.Register<FRTCMeetingPasswordMessage>(this, OnMeetingPassword);

            this._joinMeetingCommand = new RelayCommand(() =>
            {
                this.LocalCaptureSource = string.Empty;
                string addr = ConfigurationManager.AppSettings["FRTCServerAddress"];
                if (string.IsNullOrWhiteSpace(this._meetingID) || string.IsNullOrWhiteSpace(addr) || string.IsNullOrWhiteSpace(this._guestName))
                {
                    string tip = string.Empty;
                    if (string.IsNullOrWhiteSpace(this._meetingID))
                    {
                        tip = Properties.Resources.FRTC_MEETING_SDKAPP_TIP_NO_MEETING_ID;
                    }
                    else if (string.IsNullOrWhiteSpace(this._guestName))
                    {
                        tip = Properties.Resources.FRTC_MEETING_SDKAPP_TIP_NO_GUEST_NAME;
                    }
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = tip });
                    return;
                }
                else
                {
                    MessengerInstance.Send<FRTCCallStateChangeMessage>(new FRTCCallStateChangeMessage() { callState = FrtcCallState.CONNECTING, meetingId = this._meetingID });
                    if (m_signInManager.IsUserSignIn)
                        JoinMeetingAsSignedUser();
                    else
                        JoinMeeting();
                    Ringing = true;
                }
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
            });

            this._sendMeetingPWD = new RelayCommand<Window>((w) =>
            {
                this.Ringing = true;
                if (w != null)
                {
                    w.Close();
                }
                m_callManager.SendMeetingPWD(this.FRTCMeetingPWD.Trim());
            });

            this._dropCallCommand = new RelayCommand<Window>((w) =>
            {
                LogHelper.DebugMethodEnter();
                LogHelper.Debug("Drop call out of meeting, sender is {0}", w.Name);
                //if (Ringing)
                {
                    if (m_callManager.CurrentCallState != FrtcCallState.DISCONNECTED)
                    {
                        m_callManager.DropCall();
                    }
                    Ringing = false;
                    _meetingPWDFirstTime = true;
                    PasswordError = false;
                    if (Ringing)
                        MessengerInstance.Send<FRTCCallStateChangeMessage>(new FRTCCallStateChangeMessage() { callState = FrtcCallState.DISCONNECTED, reason = FrtcCallReason.CALL_ABORT });
                }
                if (w != null)
                {
                    w.Close();
                }
            });

            this._setGuestName = new RelayCommand<string>((name) =>
            {
                this.GuestName = name;
            });

            this._showVoiceOnlyTip = new RelayCommand(() =>
            {
                if (IsVoiceOnly)
                {
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_VIOCE_ONLY_MEETING_TIP });
                }
            });

            bool saveGuestName = false;
            if (bool.TryParse(ConfigurationManager.AppSettings["SaveGuestName"], out saveGuestName))
            {
                SaveGuestName = saveGuestName;
                if (SaveGuestName.Value)
                {
                    GuestName = ConfigurationManager.AppSettings["GuestName"];
                    if (GuestName.Length > 20)
                    {
                        GuestName = GuestName.Substring(0, 20);
                    }
                }
            }

            MeetingID = string.Empty;

            m_initialized = true;
        }

        private void OnWindowStateChanged(FRTCWindowStateChangedMessage msg)
        {
            if (msg.State == System.Windows.WindowState.Minimized)
            {
                LocalCaptureSource = string.Empty;
            }
            else if (msg.State == System.Windows.WindowState.Normal && JoinWithCamera.Value == true && !IsVoiceOnly)
            {
                GetCurrentCameraDevice();
            }
        }

        private void OnShow(OnFRTCViewShownMessage msg)
        {
            if (msg.View == FrtcMeetingViews.FRTCJoinMeetingView)
            {
                this.IsUserSignIn = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<FRTCUserManager>().IsUserSignIn;
                if (this.IsUserSignIn)
                {
                    this.GuestName = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<FRTCUserViewModel>().DisplayName;
                }

                string currentMic = ConfigurationManager.AppSettings["PreferredMicID"];
                if (!string.IsNullOrEmpty(currentMic))
                {
                    LogTool.LogHelper.Debug("Set micphone when join meeting view show, current mic id is {0}", currentMic);
                    m_deviceMgr.SetMicrophoneDevice(currentMic);
                }

                string currentSpeaker = ConfigurationManager.AppSettings["PreferredSpeakerID"];
                if (!string.IsNullOrEmpty(currentSpeaker))
                {
                    m_deviceMgr.SetSpeakerDevice(currentSpeaker);
                }

                string currentCamera = GetCurrentCameraDevice();
                SetSelectedCameraDeviceToSDK(currentCamera);
            }
            else
            {
                LocalCaptureSource = string.Empty;
            }
        }

        private void OnAPIResult(FRTCAPIResultMessage msg)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS || msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN)
                {
                    this.IsUserSignIn = true;
                    string resourceCultureName = UIHelper.GetResourceCultureName();
                    if (string.IsNullOrEmpty(m_signInManager.UserData.real_name))
                    {

                        if ("zh-CHS" == resourceCultureName || "zh-CHT" == resourceCultureName)
                            this.GuestName = m_signInManager.UserData.lastname + m_signInManager.UserData.firstname;
                        else
                            this.GuestName = m_signInManager.UserData.firstname + " " + m_signInManager.UserData.lastname;
                    }
                    else
                    {
                        this.GuestName = m_signInManager.UserData.real_name;
                    }
                }
                if (msg.Result == FRTC_API_RESULT.SIGNOUT_SUCCESS)
                {
                    GuestName = ConfigurationManager.AppSettings["GuestName"];
                    this.IsUserSignIn = false;
                }
            }));
        }

        private void OnFRTCCallStateChanged(FRTCCallStateChangeMessage msg)
        {
            switch (msg.callState)
            {
                case FrtcCallState.CONNECTED:
                    HideMainWindowTillMeetingEnd();
                    this.MeetingName = msg.meetingName;
                    Ringing = false;
                    break;
                case FrtcCallState.DISCONNECTED:
                    Ringing = false;
                    FRTCMeetingPWD = string.Empty;
                    _meetingPWDFirstTime = true;
                    PasswordError = false;
                    if (this.JoinWithCamera.Value == true && !IsVoiceOnly)
                    {
                        GetCurrentCameraDevice();
                    }
                    JoinWithCamera = false;

                    if (msg.reason == FrtcCallReason.CALL_SUCCESS)
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    RestoreMainWindow();

                    if (msg.reason != FrtcCallReason.CALL_ABORT
                        && msg.reason != FrtcCallReason.CALL_SUCCESS)
                    {
                        LogHelper.Debug("JoinMeetingViewModel got call disconnected msg, reason is {0}", msg.reason);
                        FRTCTipsMessage tipMsg = new FRTCTipsMessage();
                        switch (msg.reason)
                        {
                            case FrtcCallReason.CALL_NON_EXISTENT_MEETING:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_NOT_EXIST;
                                break;
                            case FrtcCallReason.CALL_REJECTED:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_REJECT;
                                break;
                            case FrtcCallReason.CALL_NO_ANSWER:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_NOANSWER;
                                break;
                            case FrtcCallReason.CALL_UNREACHABLE:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_UNREACHABLE;
                                break;
                            case FrtcCallReason.CALL_HANGUP:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_HANGUP;
                                break;
                            //case FrtcCallReason.CALL_ABORTED:
                            //    tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_ABORTED;
                            //    break;
                            case FrtcCallReason.CALL_CONNECTION_LOST:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_LOST_CONNECTION;
                                break;
                            case FrtcCallReason.CALL_SERVER_ERROR:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_SERVER_ERR;
                                break;
                            case FrtcCallReason.CALL_NO_PERMISSION:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_NO_PERMISSION;
                                break;
                            case FrtcCallReason.CALL_AUTH_FAILED:
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_SIGNIN_AUTH_FAILED, Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_AUTH_FAILED);
                                    m_signInManager.SignOut();
                                });
                                break;
                            case FrtcCallReason.CALL_UNABLE_PROCESS:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_UNABLE_PROCESS;
                                break;
                            case FrtcCallReason.CALL_MEETING_STOP:
                            case FrtcCallReason.CALL_REMOVE_FROM_MEETING:
                            case FrtcCallReason.CALL_MEETING_EXPIRED:
                            case FrtcCallReason.CALL_MEETING_NOT_START:
                            case FrtcCallReason.CALL_GUEST_NOT_ALLOW:
                            case FrtcCallReason.CALL_LOCKED:
                            case FrtcCallReason.CALL_MEETING_FULL:
                            case FrtcCallReason.CALL_MEETING_NO_LICENSE:
                            case FrtcCallReason.CALL_MEETING_LICENSE_MAX_LIMIT_REACHED:
                                ShowMeetingErrorMessageBox(msg.reason);
                                break;
                            case FrtcCallReason.CALL_PASSWORD_FAILED_RETRY_MAX:
                                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                                Messenger.Default.Send<FRTCMeetingPasswordMessage>(new FRTCMeetingPasswordMessage() { reason = "reject" });
                                break;
                            case FrtcCallReason.CALL_PASSWORD_FAILED_TIMEOUT:
                                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                                Messenger.Default.Send<FRTCMeetingPasswordMessage>(new FRTCMeetingPasswordMessage() { reason = "timeout" });
                                break;
                            case FrtcCallReason.CALL_PASSWORD_FAILED:
                                Messenger.Default.Send<FRTCMeetingPasswordMessage>(new FRTCMeetingPasswordMessage() { reason = "" });
                                return;
                            case FrtcCallReason.CALL_FAILED:
                            default:
                                tipMsg.TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_FAILED;
                                break;
                        }
                        if (!string.IsNullOrEmpty(tipMsg.TipMessage))
                            MessengerInstance.Send<FRTCTipsMessage>(tipMsg);
                    }
                    else
                    {
                        if (IsUserSignIn)
                        {
                            CommonServiceLocator.ServiceLocator.Current.GetInstance<MeetingHistoryManager>().UpdateLastMeetingEndTime();
                            MessengerInstance.Send<NotificationMessage>(new NotificationMessage("HistoryUpdated"));
                        }
                    }
                    _joinWithCamera = false;
                    _joinWithMic = false;
                    break;
                default:
                    break;
            }
            if (msg.callState == FrtcCallState.DISCONNECTED)
            {
                _selfOwnedMeeting = false;
            }
        }

        private void OnMeetingPassword(FRTCMeetingPasswordMessage msg)
        {
            this.Ringing = false;
            this.FRTCMeetingPWD = string.Empty;
            if (msg.reason == "reject")
            {
                _meetingPWDFirstTime = true;
                PasswordError = false;
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_RETRY_MAX });
                return;
            }
            else if (msg.reason == "timeout")
            {
                _meetingPWDFirstTime = true;
                PasswordError = false;
                FRTCView.FRTCPopupViewManager.CurrentPopup?.Close();
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_TIMEOUT });
                return;
            }
            else
            {
                if (_meetingPWDFirstTime)
                {
                    _meetingPWDFirstTime = false;
                }
                else
                {
                    PasswordError = true;
                }

                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    App.Current.MainWindow.Show();
                    App.Current.MainWindow.Activate();
                    App.Current.MainWindow.Focus();
                    FRTCView.FRTCPopupViewManager.ShowPopupView(FRTCView.FRTCPopupViews.FRTCMeetingPassword, null);
                }));
            }
        }

        private void ShowMeetingErrorMessageBox(FrtcCallReason reason)
        {
            string title = Properties.Resources.FRTC_MEETING_SDKAPP_JOIN_MEETING_FAILED;
            string message = "";
            switch (reason)
            {
                case FrtcCallReason.CALL_REMOVE_FROM_MEETING:
                    title = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_ENDED;
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_REMOVED_BY_HOST;
                    break;
                case FrtcCallReason.CALL_MEETING_STOP:
                    title = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_ENDED;
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_HOST_END_MEETING;
                    break;
                case FrtcCallReason.CALL_MEETING_EXPIRED:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_EXPIRED;
                    break;
                case FrtcCallReason.CALL_MEETING_NOT_START:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_EARLY;
                    break;
                case FrtcCallReason.CALL_GUEST_NOT_ALLOW:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_REJECT_GUEST;
                    break;
                case FrtcCallReason.CALL_LOCKED:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_LOCKED;
                    break;
                case FrtcCallReason.CALL_MEETING_FULL:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_FULL;
                    break;
                case FrtcCallReason.CALL_MEETING_NO_LICENSE:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_LICENSE_FAILED;
                    break;
                case FrtcCallReason.CALL_MEETING_LICENSE_MAX_LIMIT_REACHED:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_USER_MAXIMUM;
                    break;
                default:
                    message = Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_NOT_EXIST;
                    break;
            }
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                FRTCView.FRTCMessageBox.ShowNotificationMessage(title, message);
            });
        }
        private void SaveConfig(string propertyName, string value)
        {
            if (m_initialized)
            {
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings[propertyName] != null)
                    {
                        config.AppSettings.Settings[propertyName].Value = value;
                        config.Save(ConfigurationSaveMode.Modified, true);
                        ConfigurationManager.RefreshSection("appSettings");
                    }
                    config = null;
                }
                catch (ConfigurationErrorsException e)
                {
                    LogHelper.Exception(e);
                }
            }
        }

        private void HideMainWindowTillMeetingEnd()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Current.MainWindow.ShowInTaskbar = false;
                App.Current.MainWindow.Hide();
            });
        }

        private void RestoreMainWindow()
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                _meetingPWDFirstTime = true;
                PasswordError = false;
                App.Current.MainWindow.ShowInTaskbar = true;
                App.Current.MainWindow.Show();
                App.Current.MainWindow.Activate();
                m_deviceMgr.StartWatchDeviceChange();
            }));
        }

        private void JoinMeetingAsSignedUser()
        {
            int rate = IsVoiceOnly ? 64 : int.Parse(ConfigurationManager.AppSettings["MeetingCallRate"]);
            JoinFRTCMeetingMsg msg = new JoinFRTCMeetingMsg()
            {
                callRate = rate,
                confAlias = _meetingID,
                displayName = GuestName,
                preMuteCamera = !_joinWithCamera.Value,
                preMuteMic = !JoinWithMic.Value,
                userToken = m_signInManager.UserData.user_token,
                passCode = this.FRTCMeetingPWD,
                isSelfOwnedMeeting = _selfOwnedMeeting,
                isVoiceOnlyMeeting = IsVoiceOnly
            };
            MessengerInstance.Send<JoinFRTCMeetingMsg>(msg);
        }

        private void MutedJoinMeetingAsSignedUser()
        {
            int rate = IsVoiceOnly ? 64 : int.Parse(ConfigurationManager.AppSettings["MeetingCallRate"]);
            JoinFRTCMeetingMsg msg = new JoinFRTCMeetingMsg()
            {
                callRate = rate,
                confAlias = _meetingID,
                displayName = GuestName,
                preMuteCamera = !_joinWithCamera.Value,
                preMuteMic = !JoinWithMic.Value,
                userToken = m_signInManager.UserData.user_token,
                passCode = this.FRTCMeetingPWD,
                isSelfOwnedMeeting = _selfOwnedMeeting,
                isVoiceOnlyMeeting = IsVoiceOnly
            };
            MessengerInstance.Send<JoinFRTCMeetingMsg>(msg);
        }

        private void JoinMeetingAsOwner()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Ringing = true;
                int rate = int.Parse(ConfigurationManager.AppSettings["MeetingCallRate"]);
                //string displayName = CommonServiceLocator.ServiceLocator.Current.GetInstance<SignInViewModel>().DisplayName;
                JoinFRTCMeetingMsg msg = new JoinFRTCMeetingMsg()
                {
                    callRate = rate,
                    confAlias = _meetingID,
                    displayName = GuestName,
                    preMuteCamera = JoinWithCamera.HasValue ? !JoinWithCamera.Value : true,
                    preMuteMic = true,
                    userToken = m_signInManager.UserData.user_token,
                    passCode = this.FRTCMeetingPWD,
                    isSelfOwnedMeeting = _selfOwnedMeeting,
                    isVoiceOnlyMeeting = false
                };
                MessengerInstance.Send<JoinFRTCMeetingMsg>(msg);
            });
        }

        private void JoinMeeting()
        {
            int rate = IsVoiceOnly ? 64 : int.Parse(ConfigurationManager.AppSettings["MeetingCallRate"]);
            string name = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(_guestName));
            JoinFRTCMeetingMsg msg = new JoinFRTCMeetingMsg()
            { callRate = rate, confAlias = _meetingID, displayName = name, preMuteCamera = !_joinWithCamera.Value, preMuteMic = !JoinWithMic.Value, isSelfOwnedMeeting = _selfOwnedMeeting, isVoiceOnlyMeeting = IsVoiceOnly };
            MessengerInstance.Send<JoinFRTCMeetingMsg>(msg);
        }

        private string GetCurrentCameraDevice()
        {
            string settedDeviceID = ConfigurationManager.AppSettings["PreferredCameraID"];
            Console.Out.WriteLine("GetLocalPreviewSource::Current camera id is {0}", settedDeviceID);
            if (!string.IsNullOrEmpty(settedDeviceID))
            {
                if (MultimediaUtil.VideoInputDevices.Count() > 0)
                {
                    int index = -1;
                    for (int i = 0; i < MultimediaUtil.VideoInputsDevicePaths.Count(); i++)
                    {
                        if (MultimediaUtil.VideoInputDevices[i].DevicePath == settedDeviceID)
                        {
                            Console.Out.WriteLine("GetLocalPreviewSource::Get selected camera, name is {0}", MultimediaUtil.VideoInputDevices[i].Name);
                            index = i;
                            break;
                        }
                    }
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (index != -1)
                        {
                            //LocalCaptureSource = string.Empty;
                            Console.Out.WriteLine("GetLocalPreviewSource::Set camera, name is {0}", MultimediaUtil.VideoInputDevices[index].Name);
                            //LocalCaptureSource = MultimediaUtil.VideoInputDevices[index].Name;
                            //m_deviceMgr.SetCameraDevice(settedDeviceID);
                        }
                        else
                        {
                            //LocalCaptureSource = string.Empty;
                            //LocalCaptureSource = MultimediaUtil.VideoInputNames[0];
                            //m_deviceMgr.SetCameraDevice(MultimediaUtil.VideoInputsDevicePaths[0]);
                            settedDeviceID = MultimediaUtil.VideoInputNames[0];
                        }
                    }));
                }
            }
            else
            {
                if (MultimediaUtil.VideoInputDevices.Count() > 0)
                {
                    settedDeviceID = MultimediaUtil.VideoInputNames[0];
                }
            }
            return settedDeviceID;
        }

        private void SetSelectedCameraDeviceToSDK(string deviceID)
        {
            if (MultimediaUtil.VideoInputNames != null && MultimediaUtil.VideoInputNames.Length > 0)
            {
                string device = string.IsNullOrEmpty(deviceID) ? MultimediaUtil.VideoInputNames[0] : deviceID;
                m_deviceMgr.SetCameraDevice(device);
            }
        }

        private bool _canMinimize = false;
        public new bool CanMinimize
        {
            get { return _canMinimize; }
            set
            {
                _canMinimize = value;
                RaisePropertyChanged("CanMinimize");
            }
        }

        private bool _isUserSignIn = false;
        public bool IsUserSignIn
        {
            get
            {
                return _isUserSignIn;
            }
            set
            {
                if (_isUserSignIn != value)
                {
                    _isUserSignIn = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _isVoiceOnly = false;
        public bool IsVoiceOnly
        {
            get
            {
                return _isVoiceOnly;
            }
            set
            {
                if (_isVoiceOnly != value)
                {
                    _isVoiceOnly = value;
                    RaisePropertyChanged();
                    if (_isVoiceOnly)
                        JoinWithCamera = false;
                }
            }
        }

        private RelayCommand _joinMeetingCommand;
        public RelayCommand JoinMeetingCommand
        {
            get
            {
                return _joinMeetingCommand;
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

        private string _guestName = string.Empty;
        public string GuestName
        {
            get
            {
                return _guestName;
            }
            set
            {
                if (_guestName != value)
                {
                    _guestName = value;
                    RaisePropertyChanged();
                    if (_saveGuestName.Value == true && !IsUserSignIn)
                    {
                        SaveConfig("GuestName", _guestName.Trim());
                    }
                }
            }
        }

        private bool? _saveGuestName = false;
        public bool? SaveGuestName
        {
            get
            {
                return _saveGuestName;
            }
            set
            {
                if (_saveGuestName != value)
                {
                    _saveGuestName = value;
                    RaisePropertyChanged();
                    SaveConfig("SaveGuestName", _saveGuestName.ToString());
                    if (_saveGuestName.Value)
                    {
                        SaveConfig("GuestName", _guestName);
                    }
                }
            }
        }

        private bool? _joinWithCamera = false;
        public bool? JoinWithCamera
        {
            get
            {
                return _joinWithCamera;
            }
            set
            {
                if (_joinWithCamera != value)
                {
                    _joinWithCamera = value;
                    RaisePropertyChanged();
                    SaveConfig("StartCameraJoinMeeting", _joinWithCamera.Value.ToString());
                    if (_joinWithCamera.Value == true)
                    {
                        GetCurrentCameraDevice();
                    }
                    else
                    {
                        LocalCaptureSource = string.Empty;
                    }
                    if (_joinWithCamera.Value)
                        IsVoiceOnly = false;
                }
            }
        }

        private bool? _joinWithMic = false;
        public bool? JoinWithMic
        {
            get
            {
                return _joinWithMic;
            }
            set
            {
                if (_joinWithMic != value)
                {
                    _joinWithMic = value;
                    RaisePropertyChanged();
                    SaveConfig("StartMicJoinMeeting", _joinWithMic.Value.ToString());
                }
            }
        }

        private bool _pwdError = false;
        public bool PasswordError
        {
            get { return _pwdError; }
            set { _pwdError = value; RaisePropertyChanged(); }
        }


        private List<string> _userMeetingRoomList = new List<string>();
        public List<string> UserMeetingRoomList
        {
            get { return _userMeetingRoomList; }
            set { _userMeetingRoomList = value; RaisePropertyChanged("UserMeetingRoomList"); }
        }

        private bool _selfOwnedMeeting = false;
        public bool SelfOwnedMeeting
        {
            get => _selfOwnedMeeting;
            set { _selfOwnedMeeting = value; RaisePropertyChanged("SelfOwnedMeeting"); }
        }

        private string _currentMeetingBeginTime = string.Empty;
        public string CurrentMeetingBeginTime
        {
            get => _currentMeetingBeginTime;
            set { _currentMeetingBeginTime = value; RaisePropertyChanged("CurrentMeetingBeginTime"); }
        }

        private string _currentMeetingEndTime = string.Empty;
        public string CurrentMeetingEndTime
        {
            get => _currentMeetingEndTime;
            set { _currentMeetingEndTime = value; RaisePropertyChanged("CurrentMeetingEndTime"); }
        }

        private RelayCommand<Window> _dropCallCommand;
        public RelayCommand<Window> DropCallCommand
        {
            get
            {
                return _dropCallCommand;
            }
        }


        private bool _ringing = false;
        public bool Ringing
        {
            get
            {
                return _ringing;
            }
            set
            {
                if (_ringing != value)
                {
                    _ringing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _localCaptureSource = string.Empty;
        public string LocalCaptureSource
        {
            get
            {
                return _localCaptureSource;
            }
            set
            {
                if (_localCaptureSource != value)
                {
                    _localCaptureSource = value;

                }
                RaisePropertyChanged();
            }
        }

        private string _frtcMeetingPWD = string.Empty;
        public string FRTCMeetingPWD
        {
            get
            {
                return _frtcMeetingPWD;
            }
            set
            {
                if (_frtcMeetingPWD != value)
                {
                    _frtcMeetingPWD = value;
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

        private RelayCommand<Window> _sendMeetingPWD;
        public RelayCommand<Window> SendMeetingPWD
        {
            get
            {
                return _sendMeetingPWD;
            }
        }

        private RelayCommand<string> _setGuestName;
        public RelayCommand<string> SetGuestName
        {
            get
            {
                return _setGuestName;
            }
        }

        private RelayCommand _showVoiceOnlyTip;
        public RelayCommand ShowVoiceOnlyTip
        {
            get
            {
                return _showVoiceOnlyTip;
            }
        }
    }
}
