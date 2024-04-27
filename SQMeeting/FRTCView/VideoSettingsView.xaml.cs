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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// VideoSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class VideoSettingsView : UserControl
    {
        public VideoSettingsView()
        {
            InitializeComponent();
            Loaded += VideoSettingsView_Loaded;
        }

        private void VideoSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            videoArea.Width = this.ActualWidth;
            videoArea.Height = this.ActualWidth * 9 / 16;

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            (this.videoArea.RenderTransform as ScaleTransform).ScaleX = (sender as CheckBox).IsChecked.Value ? -1 : 1;
        }

        private void videoArea_MediaOpened(object sender, RoutedEventArgs e)
        {

        }

        private void videoArea_MediaFailed(object sender, WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs e)
        {
            LogTool.LogHelper.Error(e.Message);
        }

        private void videoArea_MediaClosed(object sender, RoutedEventArgs e)
        {

        }
    }
}
