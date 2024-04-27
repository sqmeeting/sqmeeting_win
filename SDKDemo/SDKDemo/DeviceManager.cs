using SDKDemo.Utilities;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SDKDemo
{
    public class ObserverableObject : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Implement
        protected PropertyChangedEventHandler PropertyChangedHandler => this.PropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            TypeInfo typeInfo = GetType().GetTypeInfo();
            if (string.IsNullOrEmpty(propertyName) || (object)typeInfo.GetDeclaredProperty(propertyName) != null)
            {
                return;
            }

            bool flag = false;
            while (typeInfo.BaseType != null && (object)typeInfo.BaseType != typeof(object))
            {
                typeInfo = typeInfo.BaseType.GetTypeInfo();
                if ((object)typeInfo.GetDeclaredProperty(propertyName) != null)
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                throw new ArgumentException("Property not found", propertyName);
            }
        }

        public virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (this.PropertyChanged != null)
            {
                string propertyName = GetPropertyName(propertyExpression);
                if (!string.IsNullOrEmpty(propertyName))
                {
                    RaisePropertyChanged(propertyName);
                }
            }
        }

        protected static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                throw new ArgumentNullException("propertyExpression");
            }

            return ((((propertyExpression.Body as MemberExpression) ?? throw new ArgumentException("Invalid argument", "propertyExpression")).Member as PropertyInfo) ?? throw new ArgumentException("Argument is not a property", "propertyExpression")).Name;
        }

        protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            RaisePropertyChanged(propertyExpression);
            return true;
        }

        protected bool Set<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected bool Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            return Set(propertyName, ref field, newValue);
        }
        #endregion
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct VIDEO_DEVICE
    {
        [MarshalAs(UnmanagedType.LPTStr)]
        public string id;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string name;

        [System.Runtime.Serialization.DataMember(IsRequired = false)]
        public int inputIndex;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct SOUND_DEVICE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 300)]
        public string name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string id;

        [MarshalAs(UnmanagedType.I4)]
        public int type;
    }

    public struct VIDEO_DEVICE_DATA
    {
        public VIDEO_DEVICE[] devices;
    }

    public struct SOUND_DEVICE_DATA
    {
        public SOUND_DEVICE[] devices;
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
    public class DeviceManager : ObserverableObject
    {
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

        object _deviceChangeHandlerLocker = new object();
        const string device_os_default = "os_default";

        public DeviceManager()
        {
            UpdateCameraDeviceList();
            UpdateMicrophoneDeviceList();
            UpdateSpeakerDeviceList();
        }

        public void SetMediaDevice(MEDIA_DEVICE_TYPE type, string id)
        {
            FRTCSDK.frtc_media_device_set(type, id);
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

        private void UpdateCameraDeviceList()
        {
            List<VIDEO_DEVICE> video_devices = null;
            lock (_deviceChangeHandlerLocker)
            {
                List<VIDEO_DEVICE> ret = null;
                try
                {
                    IntPtr ptrJStr = FRTCSDK.frtc_media_device_get(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_CAMERA);
                    string jStr = FRTCUIUtils.StringFromNativeUtf8(ptrJStr);
                    if (!string.IsNullOrEmpty(jStr))
                    {
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jStr)))
                        {
                            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(VIDEO_DEVICE_DATA));
                            ret = new List<VIDEO_DEVICE>(((VIDEO_DEVICE_DATA)jsonSerializer.ReadObject(ms)).devices);
                            ms.Close();
                        }
                    }
                }
                catch (Exception ex)
                { }
                video_devices = ret;
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
                        string currentCameraID = ConfigurationManager.AppSettings["CurrentCameraID"];
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

        private List<SOUND_DEVICE> GetSoundDevice(MEDIA_DEVICE_TYPE deviceType)
        {
            List<SOUND_DEVICE> ret = null;
            try
            {
                IntPtr ptrJStr = FRTCSDK.frtc_media_device_get(deviceType);
                string jStr = FRTCUIUtils.StringFromNativeUtf8(ptrJStr);
                if (!string.IsNullOrEmpty(jStr))
                {
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jStr)))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(SOUND_DEVICE_DATA));
                        ret = new List<SOUND_DEVICE>(((SOUND_DEVICE_DATA)jsonSerializer.ReadObject(ms)).devices);
                        ms.Close();
                    }
                }
            }
            catch (Exception ex)
            { }
            return ret;
        }
        private void UpdateSpeakerDeviceList()
        {
            List<SOUND_DEVICE> sound_devices = null;
            lock (_deviceChangeHandlerLocker)
            {
                sound_devices = GetSoundDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_SPEAKER);
            }
            if (sound_devices != null && sound_devices.Count > 0)
            {
                List<SoundDevice> tmp = new List<SoundDevice>(sound_devices.Count);
                foreach (SOUND_DEVICE speaker in sound_devices)
                {
                    tmp.Add(new SoundDevice(speaker));
                }

                tmp.Insert(0, new SoundDevice() { DeviceID = device_os_default, DeviceName = "与系统一致", DeviceType = 0 });
                string currentSpeakerID = ConfigurationManager.AppSettings["CurrentSpeakerID"];

                //bool bSet = false;

                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (this.SpeakerDeviceList != null)
                    {
                        bool isEqual = this.SpeakerDeviceList.SequenceEqual(tmp, new SoundDeviceComparer());
                        if (isEqual && this.SpeakerDeviceList.Count == tmp.Count)
                        {
                            return;
                        }
                    }

                    this.SpeakerDeviceList = tmp;


                    if (string.IsNullOrEmpty(currentSpeakerID))
                    {
                        CurrentSpeakerDevice = this.SpeakerDeviceList[0];

                        //bSet = true;
                        lock (_deviceChangeHandlerLocker)
                        {
                            FRTCSDK.frtc_media_device_set(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_SPEAKER, CurrentSpeakerDevice.DeviceID);
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
                            //bSet = true;
                            lock (_deviceChangeHandlerLocker)
                            {
                                FRTCSDK.frtc_media_device_set(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_SPEAKER, CurrentSpeakerDevice.DeviceID);
                            }
                        }
                    }
                }));
            }
        }

        private void UpdateMicrophoneDeviceList()
        {
            List<SOUND_DEVICE> sound_devices = null;
            lock (_deviceChangeHandlerLocker)
            {
                sound_devices = GetSoundDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_MICROPHONE);
            }
            if (sound_devices != null && sound_devices.Count > 0)
            {
                List<SoundDevice> tmp = new List<SoundDevice>(sound_devices.Count);
                foreach (SOUND_DEVICE mic in sound_devices)
                {
                    tmp.Add(new SoundDevice(mic));
                }

                tmp.Insert(0, new SoundDevice() { DeviceID = device_os_default, DeviceName = "与系统一致", DeviceType = 0 });
                string currentMicID = ConfigurationManager.AppSettings["CurrentMicID"];
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
                            FRTCSDK.frtc_media_device_set(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_MICROPHONE, CurrentMicDevice.DeviceID);
                        }
                    }
                    else
                    {
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
                                FRTCSDK.frtc_media_device_set(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_MICROPHONE, CurrentMicDevice.DeviceID);
                            }
                        }
                    }
                }));
            }
        }
    }
}
