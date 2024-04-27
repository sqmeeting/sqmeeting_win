using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// ShareStreamingURLWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShareStreamingURLWindow : Window
    {
        public ShareStreamingURLWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DataObject dto = new DataObject();
            string text = new TextRange(rtbStreamingInfo.Document.ContentStart, rtbStreamingInfo.Document.ContentEnd).Text;
            dto.SetText(text + CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>().StreamingUrl);
            Clipboard.Clear();
            Clipboard.SetDataObject(dto);
            this.Close();
            CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>().ShowTips(FrtcReminderType.STREAMING_INFO_COPIED);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            string uri = (sender as Hyperlink).NavigateUri.ToString();
            Process.Start(uri);
        }
    }
}
