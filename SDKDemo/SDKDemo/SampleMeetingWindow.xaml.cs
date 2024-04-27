using SDKDemo.Model.DataObj;
using SDKDemo.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SDKDemo
{
    /// <summary>
    /// SampleMeetingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SampleMeetingWindow : Window
    {
        public SampleMeetingWindow()
        {
            InitializeComponent();
            SourceInitialized += SampleMeetingWindow_SourceInitialized;
            _deviceSettings = new DeviceManager();
            Loaded += SampleMeetingWindow_Loaded;
        }

        private void SampleMeetingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            listMic.ItemsSource = _deviceSettings.MicrophoneDeviceList;
            listMic.SelectedIndex = 0;
            listSpeaker.ItemsSource = _deviceSettings.SpeakerDeviceList;
            listSpeaker.SelectedIndex = 0;
            listCamera.ItemsSource = _deviceSettings.CameraDeviceList;
            listCamera.SelectedIndex = 0;
        }

        public SampleMeetingWindow(HwndHost hwndHost) : this()
        {
            _hwndHost = hwndHost;
        }

        HwndHost? _hwndHost;
        public DeviceManager _deviceSettings { get; set; }

        private void SampleMeetingWindow_SourceInitialized(object? sender, EventArgs e)
        {
            videoArea.Child = _hwndHost;
        }

        private void TbtnMic_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = (ToggleButton)sender;
            if (tbtn.IsChecked.HasValue)
            {
                FRTCSDK.frtc_local_audio_mute(tbtn.IsChecked.Value);
            }
        }

        private void tbtnMuteCamera_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = (ToggleButton)sender;
            if (tbtn.IsChecked.HasValue)
            {
                if (tbtn.IsChecked.Value)
                    FRTCSDK.frtc_local_video_stop();
                else
                    FRTCSDK.frtc_local_video_start();
            }
        }

        private void tbtnVideoLayout_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = (ToggleButton)sender;
            if (tbtn.IsChecked.HasValue)
            {
                FRTCSDK.frtc_layout_config(tbtn.IsChecked.Value ? FrtcLayout.LAYOUT_GALLERY : FrtcLayout.LAYOUT_AUTO);
            }
        }

        private void btnDropCall_Click(object sender, RoutedEventArgs e)
        {
            FRTCSDK.frtc_call_leave();
        }

        private void listSpeaker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _deviceSettings.SetMediaDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_SPEAKER, ((SoundDevice)((ListView)sender).SelectedItem).DeviceID);
        }

        private void listMic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _deviceSettings.SetMediaDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_MICROPHONE, ((SoundDevice)((ListView)sender).SelectedItem).DeviceID);
        }

        private void listCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _deviceSettings.SetMediaDevice(MEDIA_DEVICE_TYPE.MEDIA_DEVICE_TYPE_CAMERA, ((CameraDevice)((ListView)sender).SelectedItem).DeviceID);
        }

        struct ContentSource
        {
            public string Title { get; set; }
            [DefaultValue(-1)]
            public int Type; //0:monitor,  1:window
            public string ID;
        }
        private void tbtbShareContent_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = (ToggleButton)sender;
            if (tbtn.IsChecked.HasValue)
            {
                if (tbtn.IsChecked.Value)
                {
                    List<ContentSource> contentSourceList = new List<ContentSource>();
                    IntPtr monitors = FRTCSDK.frtc_monitors_collect();
                    if (monitors != IntPtr.Zero)
                    {
                        string jStr = FRTCUIUtils.StringFromNativeUtf8(monitors);
                        JObject jObj = JObject.Parse(jStr);
                        JArray array = (JArray)jObj["monitors"];
                        for (int i = 0; i < array.Count; i++)
                        {
                            ContentSource it = new ContentSource();
                            JToken p = array[i].ToObject<JObject>()["index"];
                            it.ID = p.Value<string>();
                            it.Title = "显示器" + it.ID;
                            it.Type = 0;
                            contentSourceList.Add(it);
                        }
                    }

                    IntPtr windows = FRTCSDK.frtc_windows_collect();
                    if (windows != IntPtr.Zero)
                    {
                        string jStr = FRTCUIUtils.StringFromNativeUtf8(windows);
                        JObject jObj = JObject.Parse(jStr);
                        JArray array = (JArray)jObj["windows"];
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
                            contentSourceList.Add(new ContentSource() { Title = title, ID = hwnd.ToString(), Type = 1 }); ;
                        }
                    }

                    listContentSource.ItemsSource = contentSourceList;
                }
            }
        }

        private void listContentSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentSource source = (ContentSource)((ListView)sender).SelectedItem;
            if (source.Type == 0)
                FRTCSDK.frtc_desktop_share(int.Parse(source.ID), true);
            else
                FRTCSDK.frtc_window_share(int.Parse(source.ID), true);
        }

        private void btnStopShare_Click(object sender, RoutedEventArgs e)
        {
            FRTCSDK.frtc_content_stop();
        }
    }
}
