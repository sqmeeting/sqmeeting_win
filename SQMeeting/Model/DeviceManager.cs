using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Windows.Interop;
using System.Windows;
using SQMeeting.Utilities;

namespace SQMeeting.Model
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public struct VIDEO_DEVICE
    {
        [MarshalAs(UnmanagedType.LPTStr)]
        public string id;

        [MarshalAs(UnmanagedType.LPTStr)]
        public string name;

        [System.Runtime.Serialization.DataMember(IsRequired =false)]
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

    public class DeviceManager
    {
        private bool _startWatch = false;

        private ManagementEventWatcher _insertWatcher;

        private ManagementEventWatcher _removeWatcher;

        public event EventHandler OnSystemDeviceChanged;

        public DeviceManager()
        {
            StartWatchDeviceChange();
        }

        HwndSourceHook _hook = null;
        public void InitDeviceWatcher(IntPtr hWnd)
        {
            var hwndSource = HwndSource.FromHwnd(hWnd);
            if (_hook != null)
                hwndSource?.RemoveHook(_hook);
            _hook = new HwndSourceHook(WndProc);
            hwndSource?.AddHook(_hook);
        }

        private void _removeWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if(OnSystemDeviceChanged != null)
            {
                OnSystemDeviceChanged.Invoke(this, new EventArgs());
            }
        }

        private void _insertWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (OnSystemDeviceChanged != null)
            {
                OnSystemDeviceChanged.Invoke(this, new EventArgs());
            }
        }

        public List<VIDEO_DEVICE> GetCameraDevice()
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
            }catch(Exception ex)
            { }
            return ret;
        }

        public void SetCameraDevice(string deviceID)
        {
            FRTCSDK.frtc_media_device_set(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_CAMERA, deviceID);
        }

        public List<SOUND_DEVICE> GetMicrophoneDevices()
        {
            return GetSoundDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_MICROPHONE);
        }

        public void SetMicrophoneDevice(string deviceID)
        {
            LogTool.LogHelper.DebugMethodEnter();
            SetSoundDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_MICROPHONE, deviceID);
            LogTool.LogHelper.DebugMethodExit();
        }

        public List<SOUND_DEVICE> GetSpeakerDevices()
        {
            return GetSoundDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_SPEAKER);
        }

        public void SetSpeakerDevice(string deviceID)
        {
            SetSoundDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_SPEAKER, deviceID);
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

        private void SetSoundDevice(MEDIA_DEVICE_TYPE deviceType, string deviceID)
        {
            if(deviceType == MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_CAMERA)
            {
                return;
            }
            FRTCSDK.frtc_media_device_set(deviceType, deviceID);
        }

        public void StartWatchDeviceChange()
        {
            _startWatch = true;
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if(_startWatch && msg == 0x0219)//WM_DEVICECHANGE
            {
                OnSystemDeviceChanged?.Invoke(this, new EventArgs());
            }
            return IntPtr.Zero;
        }

        public void StopWatchDeviceChange()
        {
            _startWatch = false;
        }
    }
}
