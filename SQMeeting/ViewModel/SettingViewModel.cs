using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQMeeting.MvvMMessages;
using System.Windows.Threading;

using SQMeeting.Model;
using GalaSoft.MvvmLight.Ioc;
using System.Runtime.InteropServices;
using SQMeeting.Properties;
using SQMeeting.Commands;
using GalaSoft.MvvmLight.Messaging;
using System.Globalization;
using System.Windows;
using System.Diagnostics;
using System.Security.AccessControl;
using NAudio;
using NAudio.Wave;
using System.Threading;
using NAudio.CoreAudioApi;
using DirectShowLib;
using System.IO;
using System.Text.RegularExpressions;
using DirectShowLib.DMO;
using System.Windows.Navigation;
using SQMeeting.Utilities;
using System.Drawing;
using SQMeeting.LogTool;
using Newtonsoft.Json.Linq;
using SQMeeting.FRTCView;

namespace SQMeeting.ViewModel
{
    public enum SettingTab
    {
        General,
        Network,
        Device,
        Logs,
        About,
        FRTC_NormalSettings,
        FRTC_VideoSettings,
        FRTC_AudioSettings,
        FRTC_LabFeatures,
        FRTC_AccountSettings,
        FRTC_RecordingSettings,
        FRTC_DiagnosticSettings,
        FRTC_About
    }

    public enum AUDIODEVICETYPE
    {
        eUNKNOWN,
        eINTEGRATED,
        eUSB,
        eBLUETOOTH,
        eHDMI,
    }

    public class CameraDevice : ObservableObject
    {
        public CameraDevice()
        {

        }

        public CameraDevice(VIDEO_DEVICE device)
        {
            this.DeviceID = device.id;
            this.DeviceName = device.name;
            this.InputIndex = device.inputIndex;
        }

        private string _deviceID = string.Empty;
        public string DeviceID
        {
            get
            {
                return _deviceID;
            }
            set
            {
                if (_deviceID != value)
                {
                    _deviceID = value;
                }
            }
        }

        private string _deviceName = string.Empty;
        public string DeviceName
        {
            get
            {
                return _deviceName;
            }
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                }
            }
        }

        private int _inputIndex = 0;
        public int InputIndex
        {
            get
            {
                return _inputIndex;
            }
            set
            {
                if (_inputIndex != value)
                {
                    _inputIndex = value;
                }
            }
        }
    }

    public class SoundDevice : ObservableObject
    {
        public SoundDevice()
        {

        }

        public SoundDevice(SOUND_DEVICE device)
        {
            this.DeviceID = device.id;
            this.DeviceName = device.name;
            this.DeviceType = device.type;
        }

        private string _deviceID = string.Empty;
        public string DeviceID
        {
            get
            {
                return _deviceID;
            }
            set
            {
                if (_deviceID != value)
                {
                    _deviceID = value;
                }
            }
        }

        private string _deviceName = string.Empty;
        public string DeviceName
        {
            get
            {
                return _deviceName;
            }
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                }
            }
        }


        private int _deviceType = 0;
        public int DeviceType
        {
            get
            {
                return _deviceType;
            }
            set
            {
                if (_deviceType != value)
                {
                    _deviceType = value;
                }
            }
        }
    }

    public class VideoLayout
    {
        public string Name { get; set; }
        public FrtcLayout Layout { get; set; }
    }

    public class LanguageItem
    {
        public string Name { get; set; }
        public int index { get; set; }
    }

    public class CameraPreference
    {
        public string Name { get; set; }
        public string Preference { get; set; }
    }

    public class SettingViewModel : FRTCViewModelBase
    {
        private DispatcherTimer saveRetDisappearTimer;
        private DeviceManager m_deviceManager;

        private object _deviceChangeHandlerLocker = new object();
        private bool m_initialized = false;

        const string complexPWDRule = "8-48位间，须含大、小写字母，数字，特殊字符。";
        const string simplePWDRule = "6-48位间，由大、小写字母，数字，特殊字符组成。";

        const string device_os_default = "os_default";
        public SettingViewModel()
        {
            //DateTime t0 = DateTime.Now;
            this.ShutdownAfterClose = false;
            this.CanMinimize = false;
            this.PWDRule = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_RULE_SIMPLE;

            this.SupportedLanguages = new List<LanguageItem>()
            {
                new LanguageItem(){ Name = "简体中文", index = 0 },
                new LanguageItem(){ Name = "繁體中文", index = 1 },
                new LanguageItem(){ Name = "English", index = 2 }
                };

            try
            {
                if (Resources.Culture != null)
                {
                    string cultureNmae = Resources.Culture.Name;

                    string resourceName = UIHelper.GetResourceCultureName(cultureNmae);
                    switch (resourceName)
                    {
                        case "zh-CHS":
                            this.CurrentLanguage = SupportedLanguages[0];
                            break;
                        case "zh-CHT":
                            this.CurrentLanguage = SupportedLanguages[1];
                            break;
                        default:
                            this.CurrentLanguage = SupportedLanguages[2];
                            break;
                    }
                }
            }
            catch (Exception ex)
            { }
            this._setLanguage = new RelayCommand<int>(SetCurrentLanguage);

            this._enableMeetingReminderCommand = new RelayCommand<bool>((enable) =>
            {
                this.EnableMeetingReminder = enable;
                SaveConfig("EnableMeetingReminder", enable.ToString());
            });

            this.MessengerInstance.Register<FRTCCallStateChangeMessage>(this, (msg) =>
            {
                if (msg.callState == FrtcCallState.CONNECTED)
                {
                    try
                    {
                        UpdateAVDeviceList();
                        StopPreview();
                    }
                    catch (Exception ex) { LogHelper.Exception(ex); }
                }
            });
            this.MessengerInstance.Register<OnFRTCViewShownMessage>(this, OnShow);
            this.MessengerInstance.Register<FRTCWindowStateChangedMessage>(this, OnWindowStateChanged);

            this.MessengerInstance.Register<FRTCAPIResultMessage>(this, OnSignInResult);

            this.MessengerInstance.Register<NotificationMessage>(this, (p) =>
            {
                switch (p.Notification)
                {
                    case "CleanupChangePassword":
                        this.OldPwdPlainText = string.Empty;
                        this.NewPwdPlainText = string.Empty;
                        this.ConfirmPwdPlainText = string.Empty;
                        break;
                    case "viewmodel_locator_initialized":
                        SetMediaDevicesToSDK();
                        Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith((t) =>
                        {
                            FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_MICROPHONE_SHAREMODE, SimpleIoc.Default.GetInstance<SettingViewModel>().MicShareModeEnabled.ToString());
                        });
                        break;
                    default:
                        break;
                }
            });

            this.MessengerInstance.Register<MediaDeviceResetMessage>(this, (p) =>
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lock (_deviceChangeHandlerLocker)
                    {
                        LogTool.LogHelper.Debug("Media device reset by sdk, type {0}, id {1}", p.DeviceType, p.DeviceId);
                        if (p.DeviceType == "mic")
                        {
                            StopMicTest();
                            this.UpdateMicrophoneDeviceList();
                            SoundDevice mic = this.MicrophoneDeviceList.Find((d) => { return d.DeviceID == p.DeviceId; });
                            if (mic != null && (CurrentMicDevice == null || CurrentMicDevice.DeviceID != mic.DeviceID))
                            {
                                this.CurrentMicDevice = mic;
                            }
                        }
                        else if (p.DeviceType == "speaker")
                        {
                            StopSpeakerDeviceTesting();
                            this.UpdateSpeakerDeviceList();
                            SoundDevice speaker = this.SpeakerDeviceList.Find((d) => { return d.DeviceID == p.DeviceId; });
                            if (speaker != null && (CurrentSpeakerDevice == null || CurrentSpeakerDevice.DeviceID != speaker.DeviceID))
                            {
                                this.CurrentSpeakerDevice = speaker;
                            }
                        }
                        else if (p.DeviceType == "camera")
                        {
                            this.UpdateCameraDeviceList();
                            if (!string.IsNullOrEmpty(p.DeviceId))
                            {
                                CameraDevice camera = this.CameraDeviceList.Find((d) => { return d.DeviceID == p.DeviceId; });
                                if (camera != null && (CurrentCameraDevice == null || CurrentCameraDevice.DeviceID != camera.DeviceID))
                                {
                                    this.CurrentCameraDevice = camera;
                                    this.CurrentCameraName = CurrentCameraDevice.DeviceName;
                                }
                            }
                        }
                    }
                }), null);
            });

            this.MessengerInstance.Register<NotificationMessage<string>>(this, (p) =>
            {
                if (p == null)
                    return;
                if (p.Notification == "on_mic_test")
                {
                    HandleMicTestPeakValue(p.Content);
                }
                else if (p.Notification == "set_video_layout_incall")
                {
                    SaveConfig("VideoLayout", p.Content);
                }
            });

            this._switchSettingTab = new RelayCommand<SettingTab>(new Action<SettingTab>((p) =>
            {
                var lastTab = CurrentSettingTab;
                CurrentSettingTab = p;
                if (CurrentSettingTab == SettingTab.FRTC_VideoSettings)
                {
                    Task.Run(new Action(() =>
                    {
                        //lock (_deviceChangeHandlerLocker)
                        {
                            //PreviewCurrentCamera();
                            UpdateCameraDeviceList();
                            if (!InCall)
                                PreviewCurrentCamera();
                        }
                    }));
                    //m_deviceManager.StartWatchDeviceChange();
                }
                else if (CurrentSettingTab == SettingTab.FRTC_AudioSettings)
                {
                    Task.Run(new Action(() =>
                    {
                        StopPreview();
                        UpdateMicrophoneDeviceList();
                        UpdateSpeakerDeviceList();
                    }));
                }

                if (lastTab == SettingTab.FRTC_VideoSettings)
                {
                    StopPreview();
                    //m_deviceManager.StopWatchDeviceChange();
                }
                else if (lastTab == SettingTab.FRTC_AudioSettings)
                {
                    StopMicTest();
                    StopSpeakerDeviceTesting();
                }
            }));

            this._saveNetworkCommand = new RelayCommand(() =>
            {
                SaveNetwork();
            });

            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["PreferredMicID"] != null)
                {
                    config.AppSettings.Settings["PreferredMicID"].Value = device_os_default;
                }
                else
                {
                    config.AppSettings.Settings.Add("PreferredMicID", device_os_default);
                }
                if (config.AppSettings.Settings["PreferredSpeakerID"] != null)
                {
                    config.AppSettings.Settings["PreferredSpeakerID"].Value = device_os_default;
                }
                else
                {
                    config.AppSettings.Settings.Add("PreferredSpeakerID", device_os_default);
                }
                if (config.AppSettings.Settings["DisabelHardwareRender"] != null)
                {
                    string str = config.AppSettings.Settings["DisabelHardwareRender"].Value;
                    if (!string.IsNullOrEmpty(str))
                    {
                        HardwareRenderDisabled = bool.Parse(str);
                    }
                    else
                    {
                        HardwareRenderDisabled = FRTCUIUtils.IsVM();
                        config.AppSettings.Settings["DisabelHardwareRender"].Value = HardwareRenderDisabled.ToString();
                    }
                }
                else
                {
                    HardwareRenderDisabled = FRTCUIUtils.IsVM();
                    config.AppSettings.Settings.Add("DisabelHardwareRender", HardwareRenderDisabled.ToString());
                }

                if (config.AppSettings.Settings["EnableCameraMirroring"] != null)
                {
                    string str = config.AppSettings.Settings["EnableCameraMirroring"].Value;
                    if (!string.IsNullOrEmpty(str))
                    {
                        CameraMirroringEnabled = bool.Parse(str);
                    }
                    else
                    {
                        CameraMirroringEnabled = false;
                        config.AppSettings.Settings["EnableCameraMirroring"].Value = CameraMirroringEnabled.ToString();
                    }
                }
                else
                {
                    CameraMirroringEnabled = false;
                    config.AppSettings.Settings.Add("EnableCameraMirroring", CameraMirroringEnabled.ToString());
                }
                FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_MIRROR_LOCAL_VIDEO, CameraMirroringEnabled.ToString());


                if (config.AppSettings.Settings["EnableMeetingReminder"] != null)
                {
                    string str = config.AppSettings.Settings["EnableMeetingReminder"].Value;
                    if (!string.IsNullOrEmpty(str))
                    {
                        EnableMeetingReminder = bool.Parse(str);
                    }
                    else
                    {
                        EnableMeetingReminder = true;
                        config.AppSettings.Settings["EnableMeetingReminder"].Value = EnableMeetingReminder.ToString();
                    }
                }
                else
                {
                    EnableMeetingReminder = true;
                    config.AppSettings.Settings.Add("EnableMeetingReminder", EnableMeetingReminder.ToString());
                }

                EnableRecordingAndStreaming = true;

                _cameraPreferenceList = new List<CameraPreference>()
                { new CameraPreference(){ Name = Properties.Resources.FRTC_MEETING_APP_CAMERA_FRAMERATE_FIRST, Preference = "framerate"},
                new CameraPreference(){ Name = Properties.Resources.FRTC_MEETING_APP_CAMERA_RESOLUTION_FIRST, Preference = "resolution"}};

                string strCamera = ConfigurationManager.AppSettings["CameraPreference"];
                if (strCamera == null)
                {
                    strCamera = _cameraPreferenceList[0].Preference.ToString();
                    lock (_deviceChangeHandlerLocker)
                    {
                        try
                        {
                            config.AppSettings.Settings.Add("CameraPreference", strCamera);

                        }
                        catch (ConfigurationErrorsException e)
                        {
                            LogHelper.Exception(e);
                        }
                    }
                }


                FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_CAMERA_QUALITY_PREFERENCE, strCamera);

                _currentCameraPreference = _cameraPreferenceList.Find((p) =>
                {
                    return p.Preference == strCamera;
                });

                if (config.AppSettings.Settings["MicrophoneShareMode"] != null)
                {
                    string str = config.AppSettings.Settings["MicrophoneShareMode"].Value;
                    if (!string.IsNullOrEmpty(str))
                    {
                        MicShareModeEnabled = bool.Parse(str);
                    }
                    else
                    {
                        MicShareModeEnabled = false;
                        config.AppSettings.Settings["MicrophoneShareMode"].Value = MicShareModeEnabled.ToString();
                    }
                }
                else
                {
                    MicShareModeEnabled = false;
                    config.AppSettings.Settings.Add("MicrophoneShareMode", MicShareModeEnabled.ToString());
                }

                config.Save(ConfigurationSaveMode.Modified, true);
                ConfigurationManager.RefreshSection("appSettings");
                config = null;
            }
            catch (ConfigurationErrorsException e)
            {
                LogTool.LogHelper.Exception(e);
            }


            string addr = ConfigurationManager.AppSettings["FRTCServerAddress"];
            int rate = int.Parse(ConfigurationManager.AppSettings["MeetingCallRate"]);
            string noiseBlocker = ConfigurationManager.AppSettings["EnableNoiseBlocker"];

            this.SaveSucceed = null;
            this.ServerAddress = addr;
            CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCCallManager>().SetAPIBaseUrl(addr);
            this.CallRate = rate;
            this.NoiseBlockerEnabled = string.IsNullOrEmpty(noiseBlocker) ? false : bool.Parse(noiseBlocker);
            this.Version = Properties.Resources.FRTC_MEETING_SDKAPP_VERSION + ":" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            this.m_deviceManager = SimpleIoc.Default.GetInstance<DeviceManager>();
            this.m_deviceManager.OnSystemDeviceChanged += DeviceManager_OnSystemDeviceChanged;
            this._setCameraDevice = new RelayCommand<string>((deviceID) =>
            {
                if (!string.IsNullOrEmpty(deviceID))
                {
                    bool doPreview = CurrentCameraDevice == null ? true : !InCall && deviceID != CurrentCameraDevice.DeviceID;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        lock (_deviceChangeHandlerLocker)
                        {
                            this.m_deviceManager.SetCameraDevice(deviceID);
                            try
                            {
                                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                                config.AppSettings.Settings["PreferredCameraID"].Value = deviceID;
                                config.Save(ConfigurationSaveMode.Modified, true);
                                ConfigurationManager.RefreshSection("appSettings");
                                config = null;
                            }
                            catch (ConfigurationErrorsException e)
                            {
                                LogHelper.Exception(e);
                            }
                        }
                    });
                    CurrentCameraDevice = CameraDeviceList.Find(p => { return p.DeviceID == deviceID; });
                    if (doPreview)
                    {
                        CurrentCameraName = CurrentCameraDevice.DeviceName;
                        Task.Run(new Action(() =>
                        {
                            StopPreview();
                            Thread.Sleep(1000);
                            PreviewCurrentCamera();
                        }));
                    }
                }
            });

            this._setMicDevice = new RelayCommand<string>((deviceID) =>
            {
                if (!string.IsNullOrEmpty(deviceID))
                {
                    try
                    {
                        //if (IsTestingMic && !_testingMicDevice.ID.ToLower().Contains(deviceID == device_os_default ?
                        //    new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).ID.ToLower()
                        //    : deviceID.ToLower()))
                        ////do not stop testing when testing device still be the selected device
                        ///
                        if (IsTestingMic)//stop test any way
                        {
                            StopMicTest();
                        }
                    }
                    catch (Exception e) { LogHelper.Exception(e); }
                    SaveMicrophoneDevice(deviceID);
                }
            });

            this._setSpeakerDevice = new RelayCommand<string>((deviceID) =>
            {
                if (!string.IsNullOrEmpty(deviceID))
                {
                    StopSpeakerDeviceTesting();
                    SaveSpeakerDevice(deviceID);
                    LogTool.LogHelper.Debug("Set speaker in setting view model command, speaker id is {0}, name is {1}", deviceID, CurrentSpeakerDevice.DeviceName);
                }
            });

            this._checkSpeakerDeviceCommand = new RelayCommand(() =>
            {
                TestSpeakerDevice();
            });

            this._checkMicDeviceCommand = new RelayCommand(() => TestMicDevice());

            this._setSpeakerDeviceVolumeCommand = new RelayCommand<double>(v =>
            {
                SetSpeakerVolume(CurrentSpeakerDevice.DeviceID, (float)v);
            });

            _videoLayoutList = new List<VideoLayout>();
            for (int i = 0; i < Enum.GetValues(typeof(FrtcLayout)).Length; i++)
            {
                VideoLayout layout = new VideoLayout();
                FrtcLayout videoLayout = (FrtcLayout)Enum.GetValues(typeof(FrtcLayout)).GetValue(i);
                switch (videoLayout)
                {
                    case FrtcLayout.LAYOUT_AUTO:
                        layout.Name = Resources.FRTC_MEETING_SDKAPP_SETTING_PRESENTER;
                        break;
                    case FrtcLayout.LAYOUT_GALLERY:
                        layout.Name = Resources.FRTC_MEETING_SDKAPP_SETTING_Gallery;
                        break;
                    default:
                        break;
                }
                layout.Layout = videoLayout;
                _videoLayoutList.Add(layout);
            }

            string strLayout = ConfigurationManager.AppSettings["VideoLayout"];
            if (strLayout == null)
            {
                strLayout = _videoLayoutList[0].Layout.ToString();
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["VideoLayout"] != null)
                    {
                        config.AppSettings.Settings["VideoLayout"].Value = strLayout;
                    }
                    else
                    {
                        config.AppSettings.Settings.Add("VideoLayout", strLayout);
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
            _currentVideoLayout = _videoLayoutList.Find((p) =>
            {
                return p.Layout == (FrtcLayout)Enum.Parse(typeof(FrtcLayout), strLayout, true);
            });

            this._setVideoLayout = new RelayCommand<FrtcLayout>((layout) =>
            {
                Task.Run(new Action(() =>
                {
                    SaveConfig("VideoLayout", layout.ToString());
                    CurrentVideoLayout = _videoLayoutList.Find((p) =>
                    {
                        return p.Layout == layout;
                    });
                    FRTCSDK.frtc_layout_config(layout);
                    MessengerInstance.Send(new NotificationMessage<FrtcLayout>(layout, "set_video_layout"));
                }));
            });

            this._setCameraPreference = new RelayCommand<string>((p) =>
            {

                Task.Run(new Action(() =>
                {
                    lock (_deviceChangeHandlerLocker)
                    {
                        FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_CAMERA_QUALITY_PREFERENCE, p);
                        try
                        {
                            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                            config.AppSettings.Settings["CameraPreference"].Value = p;
                            config.Save(ConfigurationSaveMode.Modified, true);
                            ConfigurationManager.RefreshSection("appSettings");
                            config = null;
                        }
                        catch (ConfigurationErrorsException e)
                        {
                            LogHelper.Exception(e);
                        }

                    }
                }));
            });

            this._noiseBlockerCommand = new RelayCommand<bool>((enable) =>
            {
                FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_ENABLE_NOISE_BLOCKER, enable.ToString());
                SaveConfig("EnableNoiseBlocker", enable.ToString());
            });

            this._microphoneShareModeCommand = new RelayCommand<bool>((enable) =>
            {
                FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_MICROPHONE_SHAREMODE, enable.ToString());
                SaveConfig("MicrophoneShareMode", enable.ToString());
            });

            this._mirrorCameraCommand = new RelayCommand<bool>((doMirror) =>
            {
                FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_MIRROR_LOCAL_VIDEO, doMirror.ToString());
                SaveConfig("EnableCameraMirroring", doMirror.ToString());
            });

            this._disableHardwareRenderCommand = new RelayCommand<bool>((disableHardwareRender) =>
            {
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["DisabelHardwareRender"].Value = disableHardwareRender.ToString();
                    config.Save(ConfigurationSaveMode.Modified, true);
                    ConfigurationManager.RefreshSection("appSettings");
                    config = null;

                    FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_USE_SOFTWARE_RENDER, disableHardwareRender.ToString());
                }
                catch (ConfigurationErrorsException e)
                {
                    LogHelper.Exception(e);
                }
            });

            this._frtcChangePWDCommand = new FRTCChangePWDCommand();

            bool joinWithCamera = false;
            if (bool.TryParse(ConfigurationManager.AppSettings["StartCameraJoinMeeting"], out joinWithCamera))
            {
                JoinWithCamera = joinWithCamera;
            }

            bool joinWithMic = false;
            if (bool.TryParse(ConfigurationManager.AppSettings["StartMicJoinMeeting"], out joinWithMic))
            {
                JoinWithMic = joinWithMic;
            }

            bool joinVoiceOnly = false;
            if (bool.TryParse(ConfigurationManager.AppSettings["VoiceOnlyMeeting"], out joinVoiceOnly))
            {
                VoiceOnly = joinVoiceOnly;
            }

            _startUploadLogCommand = new RelayCommand<string>((dscp =>
            {
                if (LogUploadProgress < 0)
                {
                    LogUploadProgress = 0;
                    _uploadingWnd?.Close();
                }
                UploadLogs(dscp);
            }));

            _cancelUploadLogCommand = new RelayCommand<Window>((w) =>
            {
                _queryUploadStatusTimer?.Stop();
                if (LogUploadProgress < 100)
                {
                    IsCancelingUpload = true;
                    Task.Run(
                        () => FRTCSDK.frtc_logs_upload_cancel(_uploadTractionId))
                    .ContinueWith((t) =>
                    {
                        IsCancelingUpload = false;
                        LogUploadProgress = 0;
                        LogUploadSpeed = "0";
                        _uploadTractionId = 0;
                        _fileType = 0;
                        FRTCPopupViewManager.CurrentPopup.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _uploadingWnd?.Close();
                        }));
                    });
                    return;
                }
                else
                {
                    LogUploadDescription = string.Empty;
                }
                LogUploadProgress = 0;
                _uploadTractionId = 0;
                _fileType = 0;
                w?.Close();
            });

            saveRetDisappearTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(3), DispatcherPriority.Normal,
                new EventHandler((o, ev) =>
                {
                    this.SaveSucceed = null;
                    saveRetDisappearTimer.Stop();
                }), App.Current.Dispatcher);
            m_initialized = true;
            FRTCSDK.frtc_sdk_ext_config(FrtcCallExtConfigKey.CONFIG_USE_SOFTWARE_RENDER, HardwareRenderDisabled.ToString());
        }

        public void UpdateAVDeviceList()
        {
            UpdateCameraDeviceList();
            UpdateMicrophoneDeviceList();
            UpdateSpeakerDeviceList();
        }


        private void OnSignInResult(FRTCAPIResultMessage msg)
        {
            if (msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS || msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN)
            {
                this.IsGuestUser = false;
                this.SignInUserSecurityLevel = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCUserManager>().UserData.security_level;
                if (!string.IsNullOrEmpty(SignInUserSecurityLevel) && SignInUserSecurityLevel.ToLower() == "high")
                {
                    PWDRule = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_RULE_COMPLEXED;
                }
            }
            else if (msg.Result == FRTC_API_RESULT.SIGNOUT_SUCCESS
                || msg.Result == FRTC_API_RESULT.SIGNIN_SESSION_RESET
                || msg.Result == FRTC_API_RESULT.CONNECTION_FAILED
                || msg.Result == FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR)
            {
                this.IsGuestUser = true;
                this.OldPwdPlainText = string.Empty;
                this.NewPwdPlainText = string.Empty;
                this.ConfirmPwdPlainText = string.Empty;
                this.SignInUserSecurityLevel = "Normal";
                PWDRule = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_RULE_SIMPLE;
            }
        }

        private void OnWindowStateChanged(FRTCWindowStateChangedMessage msg)
        {
            if (msg.State == System.Windows.WindowState.Minimized)
            {
                this.CurrentCameraName = string.Empty;
                this.CurrentCameraDevice = null;
            }
            else if (msg.State == System.Windows.WindowState.Normal)
            {
                UpdateCameraDeviceList();
                //PreviewCurrentCamera();
            }
        }

        private void OnShow(OnFRTCViewShownMessage msg)
        {
            if (msg.View == FrtcMeetingViews.FRTCSettingWindow)
            {
                ServerAddress = ConfigurationManager.AppSettings["FRTCServerAddress"];
                string strLayout = ConfigurationManager.AppSettings["VideoLayout"];
                if (!string.IsNullOrEmpty(strLayout))
                {
                    FrtcLayout layout = (FrtcLayout)Enum.Parse(typeof(FrtcLayout), strLayout, true);
                    CurrentVideoLayout = _videoLayoutList.Find((p) =>
                    {
                        return p.Layout == layout;
                    });
                }
            }
            SwitchSettingTab.Execute(InCall ? SettingTab.FRTC_VideoSettings : SettingTab.FRTC_NormalSettings);
        }

        private void DeviceManager_OnSystemDeviceChanged(object sender, EventArgs e)
        {
            LogTool.LogHelper.DebugMethodEnter();
            Task.Delay(TimeSpan.FromMilliseconds(200)).ContinueWith(new Action<Task>((t) =>
            {
                {
                    UpdateCameraDeviceList();
                    UpdateMicrophoneDeviceList();
                    UpdateSpeakerDeviceList();
                }
            }));
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
                    }
                    else
                    {
                        config.AppSettings.Settings.Add(propertyName, value);
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

        private void SaveMicrophoneDevice(string deviceID)
        {
            if (!string.IsNullOrEmpty(deviceID))
            {
                Task.Run(new Action(() =>
                {
                    lock (_deviceChangeHandlerLocker)
                    {
                        this.m_deviceManager.SetMicrophoneDevice(deviceID);
                        SaveConfig("PreferredMicID", deviceID);
                        CurrentMicDevice = this.MicrophoneDeviceList.Find((d) => { return d.DeviceID == deviceID; });
                    }

                }));
            }
            else
            {
                LogTool.LogHelper.Debug("Save mic device id is empty");
            }
        }

        private void SaveSpeakerDevice(string deviceID)
        {
            if (!string.IsNullOrEmpty(deviceID))
            {

                Task.Run(new Action(() =>
                {
                    lock (_deviceChangeHandlerLocker)
                    {
                        this.m_deviceManager.SetSpeakerDevice(deviceID);
                        SaveConfig("PreferredSpeakerID", deviceID);
                        this.CurrentSpeakerDevice = this.SpeakerDeviceList.Find((p) => { return p.DeviceID == deviceID; });
                    }
                }));
            }
        }

        private void SetMediaDevicesToSDK()
        {
            UpdateCameraDeviceList();

            if (this.CurrentCameraDevice != null)
            {
                lock (_deviceChangeHandlerLocker)
                {
                    this.m_deviceManager.SetCameraDevice(this.CurrentCameraDevice.DeviceID);
                }
            }
            UpdateMicrophoneDeviceList();
            if (CurrentMicDevice != null)
            {
                LogTool.LogHelper.Debug("Set micphone when init setting view model, mic id is {0}", CurrentMicDevice.DeviceID);
                SaveMicrophoneDevice(CurrentMicDevice.DeviceID);
            }
            else
            {
                LogTool.LogHelper.Debug("current mic device is null after update");
            }
            UpdateSpeakerDeviceList();
            if (CurrentSpeakerDevice != null)
            {
                SaveSpeakerDevice(CurrentSpeakerDevice.DeviceID);
            }

        }

        private bool _inMeeting = false;
        public bool InMeeting
        {
            get
            {
                return _inMeeting;
            }
            set
            {
                if (_inMeeting != value)
                {
                    _inMeeting = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SettingTab _currentSettingTab = SettingTab.FRTC_NormalSettings;
        public SettingTab CurrentSettingTab
        {
            get
            {
                return _currentSettingTab;
            }
            set
            {
                if (_currentSettingTab != value)
                {
                    _currentSettingTab = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RelayCommand<SettingTab> _switchSettingTab;
        public RelayCommand<SettingTab> SwitchSettingTab
        {
            get
            {
                return _switchSettingTab;
            }
        }

        private bool? _saveSucceed;
        public bool? SaveSucceed
        {
            get
            {
                return _saveSucceed;
            }
            private set
            {
                if (_saveSucceed != value)
                {
                    _saveSucceed = value;
                    RaisePropertyChanged();
                    if (_saveSucceed.HasValue)
                    {
                        saveRetDisappearTimer.Start();
                    }
                }
            }
        }

        private void SaveNetwork()
        {
            string ipStr = this._serverAddress.Trim();
            if (string.IsNullOrEmpty(ipStr))
            {
                MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_TIP_ADDRNOTVALID });
                SaveSucceed = false;
                _serverAddress = ConfigurationManager.AppSettings["FRTCServerAddress"];
                return;
            }

            string port = string.Empty;
            if (ipStr.Contains(":"))
            {
                string[] splitIP = ipStr.Split(':');
                ipStr = splitIP[0];
                port = splitIP[1];
                int nPort = 0;
                if (!int.TryParse(port, out nPort))
                {
                    SaveSucceed = false;
                    MessengerInstance.Send<FRTCTipsMessage>(new FRTCTipsMessage()
                    { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_TIP_ADDRNOTVALID });
                    _serverAddress = ConfigurationManager.AppSettings["FRTCServerAddress"];
                    return;
                }
            }
            bool withPort = !string.IsNullOrEmpty(port);
            string addr = withPort ? ipStr + ":" + port : ipStr;
            if (addr == ConfigurationManager.AppSettings["FRTCServerAddress"])
            {
                return;
            }
            {
                Task.Run(new Action(() =>
                {
                    lock (_deviceChangeHandlerLocker)
                    {
                        SaveConfig("FRTCServerAddress", addr);
                    }
                })).Wait();

                SaveSucceed = true;
                CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCCallManager>().SetAPIBaseUrl(addr);
                if (!IsGuestUser)
                {
                    FRTCView.FRTCMessageBox.ShowNotificationMessage(Properties.Resources.FRTC_MEETING_SDKAPP_SERVER_CHANGED, Properties.Resources.FRTC_MEETING_SDKAPP_SERVER_CHANGED_TIP);
                    FRTCView.FRTCPopupViewManager.CurrentPopup.Close();
                    SimpleIoc.Default.GetInstance<FRTCUserManager>().SignOut();
                }
                else
                {
                    MessengerInstance.Send<MvvMMessages.FRTCTipsMessage>(new FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_SETTING_SAVE_SUCCEED });
                }

            }
        }

        private RelayCommand _saveNetworkCommand;
        public RelayCommand SaveNetworkCommand
        {
            get
            {
                return _saveNetworkCommand;
            }
        }

        private bool _isGuestUser = true;
        public bool IsGuestUser
        {
            get
            {
                return _isGuestUser;
            }
            set
            {
                if (_isGuestUser != value)
                {
                    _isGuestUser = value;
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

        private int _callRate = 4096;
        public int CallRate
        {
            get
            {
                return _callRate;
            }
            set
            {
                if (_callRate != value)
                {
                    _callRate = value;
                    RaisePropertyChanged();
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
                }
            }
        }

        private bool _voiceOnly = false;
        public bool VoiceOnly
        {
            get
            {
                return _voiceOnly;
            }
            set
            {
                this._voiceOnly = value;
                RaisePropertyChanged();
                SaveConfig("VoiceOnlyMeeting", _voiceOnly.ToString());
                SaveConfig("MeetingCallRate", value ? "64" : _callRate.ToString());
            }
        }


        class CameraDeviceComparer : IEqualityComparer<CameraDevice>
        {
            public bool Equals(CameraDevice x, CameraDevice y)
            {
                if (x.DeviceID == y.DeviceID)
                    return true;

                return false;
            }

            public int GetHashCode(CameraDevice obj)
            {
                return obj.GetHashCode();
            }
        }

        private List<CameraDevice> _cameraDeviceList = null;
        public List<CameraDevice> CameraDeviceList
        {
            get
            {
                return _cameraDeviceList;
            }
            private set
            {
                if (_cameraDeviceList != null)
                {
                    bool isEqual = _cameraDeviceList.SequenceEqual(value, new CameraDeviceComparer());
                    if (isEqual && _cameraDeviceList.Count == value.Count)
                    {
                    }
                    else
                    {
                        _cameraDeviceList = value;
                        RaisePropertyChanged();
                    }
                }
                else
                {
                    _cameraDeviceList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CameraDevice _currentCameraDevice = null;
        public CameraDevice CurrentCameraDevice
        {
            get
            {
                return _currentCameraDevice;
            }
            private set
            {
                _currentCameraDevice = value;
                RaisePropertyChanged();
            }
        }

        private string _currentCameraName = string.Empty;
        public string CurrentCameraName
        {
            get
            {
                return _currentCameraName;
            }
            private set
            {
                if (_currentCameraName != value)
                {
                    _currentCameraName = value;
                }
                RaisePropertyChanged();
            }
        }

        private RelayCommand<string> _setCameraDevice;
        public RelayCommand<string> SetCameraDevice
        {
            get
            {
                return _setCameraDevice;
            }
        }

        private void PreviewCurrentCamera()
        {
            CurrentCameraName = CurrentCameraDevice.DeviceName;
            MessengerInstance.Send<NotificationMessage<string>>(new NotificationMessage<string>(CurrentCameraDevice.DeviceID, "preview_camera"));
        }

        public void StopPreview()
        {
            CurrentCameraName = string.Empty;
            if (CurrentCameraDevice != null)
            {
                MessengerInstance.Send<NotificationMessage<string>>(new NotificationMessage<string>(CurrentCameraDevice.DeviceID, "stop_preview"));
            }
        }

        private void UpdateCameraDeviceList()
        {
            List<VIDEO_DEVICE> video_devices = null;
            lock (_deviceChangeHandlerLocker)
            {
                video_devices = m_deviceManager.GetCameraDevice();
            }

            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (video_devices != null && video_devices.Count > 0)
                {
                    List<CameraDevice> tmp = new List<CameraDevice>(video_devices.Count);
                    foreach (VIDEO_DEVICE video in video_devices)
                    {
                        tmp.Add(new CameraDevice(video));
                    }
                    this.CameraDeviceList = tmp;
                    //this.CameraDeviceList = tmp.OrderBy(x => x.InputIndex).ToList<CameraDevice>();
                    if (this.CameraDeviceList?.Count > 0)
                    {
                        string currentCameraID = ConfigurationManager.AppSettings["PreferredCameraID"];
                        if (string.IsNullOrEmpty(currentCameraID))
                        {
                            if (this.CameraDeviceList.Count > 0)
                            {
                                CurrentCameraDevice = this.CameraDeviceList[0];
                            }
                        }
                        else
                        {
                            CameraDevice found = _cameraDeviceList.Find(new Predicate<CameraDevice>((p) =>
                            {
                                return p.DeviceID == currentCameraID;
                            }));
                            if (found != null)
                            {
                                CurrentCameraDevice = found;
                            }
                            else
                            {
                                CurrentCameraDevice = this.CameraDeviceList[0];
                            }
                        }
                    }
                }
            }));
        }

        private List<SoundDevice> _microphoneDeviceList = null;
        public List<SoundDevice> MicrophoneDeviceList
        {
            get
            {
                return _microphoneDeviceList;
            }
            private set
            {
                if (_microphoneDeviceList != null)
                {
                    bool isEqual = _microphoneDeviceList.SequenceEqual(value, new SoundDeviceComparer());
                    if (isEqual && _microphoneDeviceList.Count == value.Count)
                    {
                    }
                    else
                    {
                        _microphoneDeviceList = value;
                        RaisePropertyChanged();
                    }
                }
                else
                {
                    _microphoneDeviceList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SoundDevice _currentMicDevice = null;
        public SoundDevice CurrentMicDevice
        {
            get
            {
                return _currentMicDevice;
            }
            private set
            {
                _currentMicDevice = value;
                RaisePropertyChanged();
            }
        }

        private int _currentMicDeviceMeterLevel = 0;
        public int CurrentMicDeviceMeterLevel
        {
            get
            {
                return _currentMicDeviceMeterLevel;
            }
            set
            {
                _currentMicDeviceMeterLevel = value;
                RaisePropertyChanged();
            }
        }

        private bool _isTestingMic = false;
        public bool IsTestingMic
        {
            get
            {
                return _isTestingMic;
            }
            set
            {
                _isTestingMic = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand<string> _setMicDevice;
        public RelayCommand<string> SetMicDevice
        {
            get
            {
                return _setMicDevice;
            }
        }

        public List<LanguageItem> SupportedLanguages { get; private set; }

        private LanguageItem _currentLanguage = null;
        public LanguageItem CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand<int> _setLanguage;
        public RelayCommand<int> SetLanguage
        {
            get { return _setLanguage; }
        }

        private void SetCurrentLanguage(int language)
        {
            if (FRTCView.FRTCMessageBox.ShowConfirmMessage(
                Properties.Resources.FRTC_MEETING_SDKAPP_SETTING_LANGUAGE_CHANGE,
                Properties.Resources.FRTC_MEETING_SDKAPP_SETTING_LANGUAGE_CHANGED_MSG,
                Properties.Resources.FRTC_MEETING_SDKAPP_RESTART_NOW))
            {
                string resourceCultureName = "en-US";
                switch (language)
                {
                    case 0:
                        resourceCultureName = "zh-CHS";
                        break;
                    case 1:
                        resourceCultureName = "zh-CHT";
                        break;
                    default:
                        break;
                }
                SaveConfig("AppLanguage", resourceCultureName);
                var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
                (App.Current as App).RemoveMutex();
                Application.Current.Shutdown();
                Process.Start(currentExecutablePath);
            }
        }

        private bool _enableMeetingReminder = true;
        public bool EnableMeetingReminder
        {
            get => _enableMeetingReminder;
            set => _enableMeetingReminder = value;
        }

        private RelayCommand<bool> _enableMeetingReminderCommand;
        public RelayCommand<bool> EnableMeetingReminderCommand
        {
            get { return _enableMeetingReminderCommand; }
        }

        private void UpdateMicrophoneDeviceList()
        {
            LogTool.LogHelper.DebugMethodEnter();
            LogTool.LogHelper.Debug(Environment.StackTrace);
            List<SOUND_DEVICE> sound_devices = null;
            lock (_deviceChangeHandlerLocker)
            {
                sound_devices = m_deviceManager.GetMicrophoneDevices();
            }
            if (sound_devices != null && sound_devices.Count > 0)
            {
                List<SoundDevice> tmp = new List<SoundDevice>(sound_devices.Count);
                foreach (SOUND_DEVICE mic in sound_devices)
                {
                    LogTool.LogHelper.Debug("Mic device from SDK {0}", mic.id);
                    tmp.Add(new SoundDevice(mic));
                }

                tmp.Insert(0, new SoundDevice() { DeviceID = device_os_default, DeviceName = Properties.Resources.FRTC_MEETING_SDKAPP_SETTING_SAMEAS_OS, DeviceType = 0 });
                string currentMicID = ConfigurationManager.AppSettings["PreferredMicID"];
                //bool bSet = false;

                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (this.MicrophoneDeviceList != null)
                    {
                        bool isEqual = this.MicrophoneDeviceList.SequenceEqual(tmp, new SoundDeviceComparer());
                        if (isEqual && this.MicrophoneDeviceList.Count == tmp.Count)
                        {
                            return;
                        }
                    }

                    this.MicrophoneDeviceList = tmp;


                    if (string.IsNullOrEmpty(currentMicID))
                    {
                        CurrentMicDevice = this.MicrophoneDeviceList[0];
                        //bSet = true;
                        lock (_deviceChangeHandlerLocker)
                        {
                            m_deviceManager.SetMicrophoneDevice(CurrentMicDevice.DeviceID);
                        }
                    }
                    else
                    {
                        LogTool.LogHelper.Debug("Current mic is {0}", currentMicID); ;
                        SoundDevice found = _microphoneDeviceList.Find(new Predicate<SoundDevice>((p) =>
                        {
                            return p.DeviceID == currentMicID;
                        }));

                        if (found != null)
                        {
                            CurrentMicDevice = found;
                        }
                        else
                        {
                            CurrentMicDevice = this.MicrophoneDeviceList[0];
                            //bSet = true;
                            lock (_deviceChangeHandlerLocker)
                            {
                                m_deviceManager.SetMicrophoneDevice(CurrentMicDevice.DeviceID);
                            }
                        }
                    }
                }));
            }
        }

        class SoundDeviceComparer : IEqualityComparer<SoundDevice>
        {
            public bool Equals(SoundDevice x, SoundDevice y)
            {
                if (x.DeviceID == y.DeviceID)
                    return true;

                return false;
            }

            public int GetHashCode(SoundDevice obj)
            {
                return obj.GetHashCode();
            }
        }

        private List<SoundDevice> _speakerDeviceList = null;
        public List<SoundDevice> SpeakerDeviceList
        {
            get
            {
                return _speakerDeviceList;
            }
            private set
            {

                if (_speakerDeviceList != null)
                {
                    bool isEqual = _speakerDeviceList.SequenceEqual(value, new SoundDeviceComparer());
                    if (isEqual && _speakerDeviceList.Count == value.Count)
                    {
                    }
                    else
                    {
                        _speakerDeviceList = value;
                        RaisePropertyChanged();
                    }
                }
                else
                {
                    _speakerDeviceList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private SoundDevice _currentSpeakerDevice = null;
        public SoundDevice CurrentSpeakerDevice
        {
            get
            {
                return _currentSpeakerDevice;
            }
            private set
            {
                _currentSpeakerDevice = value;
                RaisePropertyChanged();
            }
        }

        private int _currentSpeakerDeviceMeterLevel = 0;
        public int CurrentSpeakerDeviceMeterLevel
        {
            get
            {
                return _currentSpeakerDeviceMeterLevel;
            }
            set
            {
                _currentSpeakerDeviceMeterLevel = value;
                RaisePropertyChanged();
            }
        }

        private bool _isPlayingTestAudio = false;
        public bool IsPlayingTestAudio
        {
            get
            {
                return _isPlayingTestAudio;
            }
            set
            {
                _isPlayingTestAudio = value;
                RaisePropertyChanged();
            }
        }

        private uint _currentSpeakerDeviceVolumeStep = 1;
        public uint CurrentSpeakerDeviceVolumeStep
        {
            get
            {
                return _currentSpeakerDeviceVolumeStep;
            }
            private set
            {
                _currentSpeakerDeviceVolumeStep = value;
                RaisePropertyChanged();
            }
        }

        private float _currentSpeakerDeviceVolumeMax = 1.0f;
        public float CurrentSpeakerDeviceVolumeMax
        {
            get
            {
                return _currentSpeakerDeviceVolumeMax;
            }
            private set
            {
                _currentSpeakerDeviceVolumeMax = value;
                RaisePropertyChanged();
            }
        }

        private float _currentSpeakerDeviceVolume = 0.0f;
        public float CurrentSpeakerDeviceVolume
        {
            get
            {
                return _currentSpeakerDeviceVolume;
            }
            set
            {
                _currentSpeakerDeviceVolume = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand<string> _setSpeakerDevice;
        public RelayCommand<string> SetSpeakerDevice
        {
            get
            {
                return _setSpeakerDevice;
            }
        }

        private RelayCommand _checkSpeakerDeviceCommand;
        public RelayCommand CheckSpeakerDeviceCommand
        {
            get
            {
                return _checkSpeakerDeviceCommand;
            }
        }

        private RelayCommand _checkMicDeviceCommand;
        public RelayCommand CheckMicDeviceCommand
        {
            get
            {
                return _checkMicDeviceCommand;
            }
        }

        private RelayCommand<double> _setSpeakerDeviceVolumeCommand;
        public RelayCommand<double> SetSpeakerDeviceVolumeCommand
        {
            get
            {
                return _setSpeakerDeviceVolumeCommand;
            }
        }

        private void UpdateSpeakerDeviceList()
        {
            List<SOUND_DEVICE> sound_devices = null;
            lock (_deviceChangeHandlerLocker)
            {
                sound_devices = m_deviceManager.GetSpeakerDevices();
            }
            if (sound_devices != null && sound_devices.Count > 0)
            {
                List<SoundDevice> tmp = new List<SoundDevice>(sound_devices.Count);
                foreach (SOUND_DEVICE speaker in sound_devices)
                {
                    tmp.Add(new SoundDevice(speaker));
                    LogTool.LogHelper.Debug("enum speaker devices {0}, {1}", speaker.name, speaker.id);
                }

                tmp.Insert(0, new SoundDevice() { DeviceID = device_os_default, DeviceName = Properties.Resources.FRTC_MEETING_SDKAPP_SETTING_SAMEAS_OS, DeviceType = 0 });
                string currentSpeakerID = ConfigurationManager.AppSettings["PreferredSpeakerID"];

                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (this.SpeakerDeviceList != null)
                    {
                        bool isEqual = this.SpeakerDeviceList.SequenceEqual(tmp, new SoundDeviceComparer());
                        if (isEqual && this.SpeakerDeviceList.Count == tmp.Count)
                        {
                            LogTool.LogHelper.Debug("enum speaker devices no change");
                            return;
                        }
                    }

                    this.SpeakerDeviceList = tmp;


                    if (string.IsNullOrEmpty(currentSpeakerID))
                    {
                        CurrentSpeakerDevice = this.SpeakerDeviceList[0];

                        lock (_deviceChangeHandlerLocker)
                        {
                            m_deviceManager.SetSpeakerDevice(CurrentSpeakerDevice.DeviceID);
                        }
                    }
                    else
                    {
                        SoundDevice found = _speakerDeviceList.Find(new Predicate<SoundDevice>((p) =>
                        {
                            return p.DeviceID == currentSpeakerID;
                        }));
                        if (found != null)
                        {
                            CurrentSpeakerDevice = found;
                        }
                        else
                        {
                            CurrentSpeakerDevice = this.SpeakerDeviceList[0];
                            lock (_deviceChangeHandlerLocker)
                            {
                                m_deviceManager.SetSpeakerDevice(CurrentSpeakerDevice.DeviceID);
                            }
                        }
                    }
                }));
            }
        }

        private void InitSpeakerVolume()
        {
            string id = CurrentSpeakerDevice.DeviceID;
            using (var output = id == device_os_default ? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console)
                : new MMDeviceEnumerator().GetDevice(CurrentSpeakerDevice.DeviceID))
            {
                if (output != null)
                {
                    CurrentSpeakerDeviceVolumeStep = output.AudioEndpointVolume.StepInformation.Step;
                    CurrentSpeakerDeviceVolumeMax = Math.Abs(output.AudioEndpointVolume.VolumeRange.MinDecibels);
                    CurrentSpeakerDeviceVolume = (int)(output.AudioSessionManager.SimpleAudioVolume.Volume * output.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
                }
            };
        }

        WasapiOut _testingSpeakerWavOut = null;
        private void TestSpeakerDevice()
        {
            IsPlayingTestAudio = true;
            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                try
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    }
                    using (var output = CurrentSpeakerDevice.DeviceID == device_os_default ? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console)
                        : FindSpeakerDevice(CurrentSpeakerDevice.DeviceID))
                    {
                        while (output != null && IsPlayingTestAudio)
                        {
                            if (cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            }
                            CurrentSpeakerDeviceMeterLevel = (int)Math.Round(output.AudioMeterInformation.MasterPeakValue * 10);
                            Thread.Sleep(200);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    cancellationTokenSource.Dispose();
                }
            }, cancellationTokenSource.Token);

            Task.Run(() =>
            {
                using (var audioFile = new WaveFileReader(Resources.Classical))
                {
                    using (var output = CurrentSpeakerDevice.DeviceID == device_os_default ? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console)
                        : FindSpeakerDevice(CurrentSpeakerDevice.DeviceID))
                    {
                        using (_testingSpeakerWavOut = new WasapiOut(output, AudioClientShareMode.Shared, true, 200))
                        {
                            LogTool.LogHelper.Debug("Start testing speaker, current speaker is {0}, {1}", CurrentSpeakerDevice.DeviceName, CurrentSpeakerDevice.DeviceID);
                            _testingSpeakerWavOut.Init(audioFile);
                            _testingSpeakerWavOut.PlaybackStopped += new EventHandler<StoppedEventArgs>((s, e) =>
                            {
                                try
                                {
                                    if (cancellationTokenSource.Token.CanBeCanceled)
                                        cancellationTokenSource.Cancel();
                                }
                                catch (Exception ex)
                                {
                                    LogTool.LogHelper.Exception(ex);
                                }
                                finally
                                {
                                    IsPlayingTestAudio = false;
                                    CurrentSpeakerDeviceMeterLevel = 0;
                                }
                            });
                            _testingSpeakerWavOut.Play();
                            while (_testingSpeakerWavOut != null && _testingSpeakerWavOut.PlaybackState != PlaybackState.Stopped)
                            {
                                Thread.Sleep(1000);
                            }
                        }
                    }
                }
            });
        }

        public void StopSpeakerDeviceTesting()
        {
            if (_testingSpeakerWavOut != null)
            {
                _testingSpeakerWavOut.Stop();

            }
        }

        WaveInEvent _wavInEvent = null;
        WasapiCapture _capture = null;
        MMDevice _testingMicDevice = null;
        private void TestMicDevice()
        {
            if (IsTestingMic)
            {
                StopMicTest();
                return;
            }
            else
            {
                _testingMicDevice = CurrentMicDevice.DeviceID == device_os_default ? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console)
                                        : FindMicDevice(CurrentMicDevice.DeviceID);
                if (_testingMicDevice != null)
                {
                    LogTool.LogHelper.Debug("Start testing mic, current mic is {0}, {1}", _testingMicDevice.FriendlyName, _testingMicDevice.ID);
                    //start meter report
                    FRTCSDK.frtc_start_mic_test();
                    IsTestingMic = true;
                }
            }
        }

        public void StopMicTest()
        {
            if (IsTestingMic)
            {
                FRTCSDK.frtc_stop_mic_test();
                IsTestingMic = false;
                Action a = () => { CurrentMicDeviceMeterLevel = 0; };
                if (InCall)
                    CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCMeetingVideoViewModel>()._meetingVideoWnd?.Dispatcher?.BeginInvoke(a);
                else
                    Application.Current?.Dispatcher?.BeginInvoke(a);
                return;
            }
        }

        void HandleMicTestPeakValue(string peakValue)
        {
            if (IsTestingMic)
            {
                float fPeakValue = 0.0f;
                if (float.TryParse(peakValue, out fPeakValue))
                {
                    Action a = () =>
                    {
                        try
                        {
                            CurrentMicDeviceMeterLevel = (int)(long)Math.Round((double)fPeakValue * 200);
                        }
                        catch (Exception ex) { LogHelper.Exception(ex); }
                    };
                    try
                    {
                        if (InCall)
                            CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCMeetingVideoViewModel>()._meetingVideoWnd?.Dispatcher?.BeginInvoke(a);
                        else
                            Application.Current?.Dispatcher?.BeginInvoke(a);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Exception(e);
                    }
                }
            }
        }

        private void SetSpeakerVolume(string DeviceID, float volume)
        {
            string id = DeviceID;
            using (var output = id == device_os_default ? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console)
                : new MMDeviceEnumerator().GetDevice(CurrentSpeakerDevice.DeviceID))
            {
                if (output != null)
                {
                    volume = volume / 100.0f;
                    var setvolume = volume / output.AudioEndpointVolume.MasterVolumeLevelScalar;
                    if (setvolume <= 1)
                        output.AudioSessionManager.SimpleAudioVolume.Volume = setvolume;
                    else
                    {
                        output.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                        output.AudioSessionManager.SimpleAudioVolume.Volume = 1;
                    }
                }
            }
        }

        private MMDevice FindMicDevice(string DeviceID)
        {
            MMDevice ret = null;
            var devices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in devices)
            {
                if (device.ID.ToLower().Contains(DeviceID.ToLower()))
                {
                    ret = device;
                    break;
                }
            }
            return ret;
        }

        private MMDevice FindSpeakerDevice(string DeviceID)
        {
            MMDevice ret = null;
            var devices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in devices)
            {
                if (device.ID.ToLower().Contains(DeviceID.ToLower()))
                {
                    ret = device;
                    break;
                }
            }
            return ret;
        }

        private List<VideoLayout> _videoLayoutList;
        public List<VideoLayout> VideoLayoutList
        {
            get
            {
                return _videoLayoutList;
            }
        }

        private VideoLayout _currentVideoLayout;
        public VideoLayout CurrentVideoLayout
        {
            get
            {
                return _currentVideoLayout;
            }
            set
            {
                if (_currentVideoLayout != value)
                {
                    _currentVideoLayout = value;
                    RaisePropertyChanged();
                }
            }
        }

        private List<CameraPreference> _cameraPreferenceList;
        public List<CameraPreference> CameraPreferenceList
        {
            get
            {
                return _cameraPreferenceList;
            }
        }

        public RelayCommand<FrtcLayout> _setVideoLayout;
        public RelayCommand<FrtcLayout> SetVideoLayout
        {
            get
            {
                return _setVideoLayout;
            }
        }

        private CameraPreference _currentCameraPreference;
        public CameraPreference CurrentCameraPreference
        {
            get
            {
                return _currentCameraPreference;
            }
            set
            {
                if (_currentCameraPreference != value)
                {
                    _currentCameraPreference = value;
                    RaisePropertyChanged();
                }
            }
        }

        public RelayCommand<string> _setCameraPreference;
        public RelayCommand<string> SetCameraPreference
        {
            get
            {
                return _setCameraPreference;
            }
        }

        private bool _cameraMirroringEnabled = false;
        public bool CameraMirroringEnabled
        {
            get
            {
                return _cameraMirroringEnabled;
            }
            set
            {
                if (_cameraMirroringEnabled != value)
                {
                    _cameraMirroringEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        public RelayCommand<bool> _mirrorCameraCommand;
        public RelayCommand<bool> MirrorCameraCommand
        {
            get
            {
                return _mirrorCameraCommand;
            }
        }

        private bool _noiseBlockerEnabled = false;
        public bool NoiseBlockerEnabled
        {
            get
            {
                return _noiseBlockerEnabled;
            }
            set
            {
                if (_noiseBlockerEnabled != value)
                {
                    _noiseBlockerEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        public RelayCommand<bool> _noiseBlockerCommand;
        public RelayCommand<bool> NoiseBlockerCommand
        {
            get
            {
                return _noiseBlockerCommand;
            }
        }

        private bool _micShareModeEnabled = false;
        public bool MicShareModeEnabled
        {
            get
            {
                return _micShareModeEnabled;
            }
            set
            {
                if (_micShareModeEnabled != value)
                {
                    _micShareModeEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        public RelayCommand<bool> _microphoneShareModeCommand;
        public RelayCommand<bool> MicrophoneShareModeCommand
        {
            get
            {
                return _microphoneShareModeCommand;
            }
        }

        private bool _hardwareRenderDisabled = false;
        public bool HardwareRenderDisabled
        {
            get
            {
                return _hardwareRenderDisabled;
            }
            set
            {
                if (_hardwareRenderDisabled != value)
                {
                    _hardwareRenderDisabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        public RelayCommand<bool> _disableHardwareRenderCommand;
        public RelayCommand<bool> DisableHardwareRenderCommand
        {
            get
            {
                return _disableHardwareRenderCommand;
            }
        }

        private bool _enableRecordingAndStreaming = false;
        public bool EnableRecordingAndStreaming
        {
            get { return _enableRecordingAndStreaming; }
            set { if (_enableRecordingAndStreaming != value) { _enableRecordingAndStreaming = value; RaisePropertyChanged(); } }
        }

        private string _oldPwdPlainText = string.Empty;
        public string OldPwdPlainText
        {
            get
            {
                return _oldPwdPlainText;
            }
            set
            {
                if (_oldPwdPlainText != value)
                {
                    _oldPwdPlainText = value;
                    RaisePropertyChanged("PlainText");
                }
            }
        }

        private string _newPwdPlainText = string.Empty;
        public string NewPwdPlainText
        {
            get
            {
                return _newPwdPlainText;
            }
            set
            {
                if (_newPwdPlainText != value)
                {
                    _newPwdPlainText = value;
                    RaisePropertyChanged("PlainText");
                }
            }
        }

        private string _confirmPwdPlainText = string.Empty;
        public string ConfirmPwdPlainText
        {
            get
            {
                return _confirmPwdPlainText;
            }
            set
            {
                if (_confirmPwdPlainText != value)
                {
                    _confirmPwdPlainText = value;
                    RaisePropertyChanged("PlainText");
                }
            }
        }

        private string _signInUserSecurityLevel = "Normal";
        public string SignInUserSecurityLevel
        {
            get { return _signInUserSecurityLevel; }
            set
            {
                if (value != _signInUserSecurityLevel)
                {
                    _signInUserSecurityLevel = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string PWDRule { get; set; }

        private FRTCChangePWDCommand _frtcChangePWDCommand;
        public FRTCChangePWDCommand FRTCChangePWDCommand
        {
            get { return _frtcChangePWDCommand; }
            set { _frtcChangePWDCommand = value; }
        }

        private string _version = string.Empty;
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (_version != value)
                {
                    _version = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _inCall = false;
        public bool InCall
        {
            get
            {
                return _inCall;
            }
            set
            {
                if (_inCall != value)
                {
                    _inCall = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsNotAudioOnly = true;
        public bool IsNotAudioOnly
        {
            get
            {
                return _IsNotAudioOnly;
            }
            set
            {
                if (_IsNotAudioOnly != value)
                {
                    _IsNotAudioOnly = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _logUploadDescription = string.Empty;
        public string LogUploadDescription
        {
            get => _logUploadDescription;
            set
            {
                _logUploadDescription = value;
                RaisePropertyChanged("LogUploadDescription");
            }
        }

        private int _logUploadProgress = 0;
        public int LogUploadProgress
        {
            get => _logUploadProgress;
            set
            {
                _logUploadProgress = value;
                RaisePropertyChanged("LogUploadProgress");
            }
        }

        private string _logUploadSpeed = " 0";
        public string LogUploadSpeed
        {
            get => _logUploadSpeed;
            set
            {
                _logUploadSpeed = value;
                RaisePropertyChanged("LogUploadSpeed");
            }
        }

        private RelayCommand<string> _startUploadLogCommand;
        public RelayCommand<string> StartUploadLogCommand
        {
            get => _startUploadLogCommand;
        }

        private RelayCommand<Window> _cancelUploadLogCommand;
        public RelayCommand<Window> CancelUploadLogCommand
        {
            get => _cancelUploadLogCommand;
        }

        private bool _isCancelingUpload = false;
        public bool IsCancelingUpload
        {
            get => _isCancelingUpload;
            set { _isCancelingUpload = value; RaisePropertyChanged(nameof(IsCancelingUpload)); }
        }

        Int64 _uploadTractionId = 0;
        DispatcherTimer _queryUploadStatusTimer = null;
        int _fileType = 0;//fileType:  0, log file; 1, core dump file; 2, meta file;
        LogUploadWindow _uploadingWnd = null;
        void UploadLogs(string dscp)
        {
            System.Management.SelectQuery query = new System.Management.SelectQuery(@"Select * from Win32_ComputerSystem");
            string manufacturer = string.Empty;
            string model = string.Empty;
            using (System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query))
            {
                foreach (System.Management.ManagementObject process in searcher.Get())
                {
                    process.Get();
                    manufacturer = process["Manufacturer"].ToString(); ;
                    model = process["Model"].ToString();
                }
            }

            JObject jObj = new JObject
                {
                    { "version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() },
                    { "platform", "windows" },
                    { "os", string.Format("{0}.{1}.{2}", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, Environment.OSVersion.Version.Build) },
                    { "device", manufacturer + " " + model },
                    { "issue",  dscp }
                };

            string metaData = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(jObj.ToString()));

            string fileNames = string.Empty;
            int count = 0;
            if (Directory.Exists("logs"))
            {
                string[] appLogs = Directory.GetFiles("logs", "*.*");
                if (appLogs.Count() > 0)
                {
                    count += appLogs.Count();
                    fileNames = "logs\\FRTC_Windows_App.log";
                }
            }
            string dumpFileName = string.Empty;
            List<string> dmpFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dmp", SearchOption.TopDirectoryOnly).ToList();
            if (dmpFiles != null && dmpFiles.Count() > 0)
            {
                dmpFiles.Sort();
                dumpFileName = dmpFiles.Last();
            }

            if (!string.IsNullOrEmpty(fileNames))
            {
                _uploadTractionId = FRTCSDK.frtc_logs_upload_start(metaData, fileNames, count, dumpFileName);
                if (_uploadTractionId > 0)
                {
                    _queryUploadStatusTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal,
                        new EventHandler((o, ev) =>
                        {
                            try
                            {
                                if (_uploadTractionId > 0)
                                {
                                    int speed = 0;
                                    Int64 status = FRTCSDK.frtc_logs_upload_status_query(_uploadTractionId, _fileType, ref speed); //fileType:  0, log file; 1, core dump file; 2, meta file;
                                    if (status >= 0)
                                    {
                                        string strSpeed = "0";
                                        if (speed > 0 && speed < 1024)
                                        {
                                            strSpeed = speed.ToString() + "B/s";
                                        }
                                        else if (speed >= 1024 && speed < 1024 * 1024)
                                        {
                                            strSpeed = ((double)speed / 1024d).ToString("F2") + "KB/s";
                                        }
                                        else if (speed >= 1024 * 1024)
                                        {
                                            strSpeed = ((double)speed / (1024d * 1024d)).ToString("F2") + "MB/s";
                                        }
                                        LogUploadSpeed = " " + strSpeed;
                                        if (_fileType == 0)
                                        {
                                            LogUploadProgress = (int)(status / 5.0d);//set log file type weight to 20%
                                            if (LogUploadProgress == 20)
                                            {
                                                if (string.IsNullOrEmpty(dumpFileName))
                                                {
                                                    _fileType += 2;
                                                }
                                                else
                                                    _fileType++;
                                            }
                                        }
                                        else if (_fileType == 1)
                                        {
                                            int progress = 20 + (int)(status * 4.0d / 5.0d);//set log file type weight to 80%
                                            if (progress == 100)
                                            {
                                                progress = 99;
                                                _fileType++;
                                            }
                                            LogUploadProgress = progress;
                                        }
                                        else
                                        {
                                            if (status == 100)
                                            {
                                                LogUploadProgress = 100;
                                                LogUploadSpeed = "0";
                                                _queryUploadStatusTimer.Stop();
                                                _uploadTractionId = 0;
                                                _fileType = 0;
                                                try
                                                {
                                                    foreach (string f in dmpFiles)
                                                    {
                                                        File.Delete(f);
                                                        LogHelper.Debug("delete dump file {0}", f);
                                                    }
                                                }
                                                catch (Exception ex) { LogTool.LogHelper.Exception(ex); }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogUploadProgress = (int)status;
                                        LogTool.LogHelper.Error("Log upload failed error code is {0}", status);
                                        _queryUploadStatusTimer.Stop();
                                        _uploadTractionId = 0;
                                        _fileType = 0;
                                    }
                                }
                                else
                                {
                                    _queryUploadStatusTimer?.Stop();
                                }
                            }
                            catch (Exception ex) { LogHelper.Exception(ex); }
                        }), FRTCPopupViewManager.CurrentPopup.Dispatcher);


                    _uploadingWnd = new LogUploadWindow();
                    _uploadingWnd.Owner = FRTCPopupViewManager.CurrentPopup;
                    _uploadingWnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    _uploadingWnd.Closing += (s, e) =>
                    {
                        if (IsCancelingUpload)
                        {
                            e.Cancel = true;
                            return;
                        }
                        if (_uploadTractionId > 0)
                        {
                            FRTCSDK.frtc_logs_upload_cancel(_uploadTractionId);
                        }
                        _uploadTractionId = 0;
                        _queryUploadStatusTimer.Stop();
                        if (LogUploadProgress == 100)
                        {
                            LogUploadDescription = string.Empty;
                        }
                        LogUploadProgress = 0;
                        _fileType = 0;
                        LogUploadSpeed = "0";
                        e.Cancel = false;
                    };

                    _queryUploadStatusTimer.Start();

                    _uploadingWnd.ShowDialog();
                }
            }
        }
    }
}