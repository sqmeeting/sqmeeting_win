using SQMeeting.Utilities;
using SQMeeting.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// SettingLayoutView.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<GalaSoft.MvvmLight.Messaging.NotificationMessage<string>>(this, OnMsg);
            Closed += (s, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    var vm = (this.DataContext as SettingViewModel);
                    vm.StopPreview();
                    vm.StopMicTest();
                    vm.StopSpeakerDeviceTesting();
                    vm.LogUploadDescription = string.Empty;
                    Messenger.Default.Unregister(this);
                });
            };
        }

        void OnMsg(NotificationMessage<string> msg)
        {
            if (msg.Notification.ToLower() == "preview_camera")
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (this.settingContent.Content is VideoSettingsView)
                    {
                        var view = this.settingContent.Content as VideoSettingsView;
                        if (view != null)
                        {
                            view.videoArea.Play();
                        }
                    }
                });
            }
            else if (msg.Notification.ToLower() == "stop_preview")
            {
                if (!Dispatcher.HasShutdownStarted)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (this.settingContent.Content is VideoSettingsView)
                        {
                            var view = this.settingContent.Content as VideoSettingsView;
                            if (view != null && view.videoArea.IsPlaying)
                            {
                                view.videoArea.Stop();
                            }
                        }
                    });
                }
            }
        }
    }
}
