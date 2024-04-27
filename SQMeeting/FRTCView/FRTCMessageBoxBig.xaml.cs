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
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// FRTCMessageBoxBig.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCMessageBoxBig : Window
    {
        private bool isOK = false;

        public bool StreamingPassword = true;
        public bool CancelRecurrence = false;
        public bool UpdateRecurrence = false;


        public FRTCMessageBoxBig()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            isOK = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            isOK = true;
            this.Close();
        }

        public static bool ShowRecordingConfirmWindow()
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.panelCheckBox.Visibility = Visibility.Collapsed;
            w.Owner = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Title = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_START_RECORDING;
            w.ShowDialog();
            return w.isOK;
        }

        public static bool ShowRecordingMutedConfirmWindow()
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.panelCheckBox.Visibility = Visibility.Collapsed;
            w.Owner = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Title = Properties.Resources.FRTC_MEETING_RECORDING_MUTED;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_RECORDING_MUTED;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_RECORDING_MUTED_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_RECORDING_UNMUTE;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_RECORDING_CONTINUE_MUTED;
            w.ShowDialog();
            return w.isOK;
        }

        public static bool ShowStopRecordingConfirmWindow()
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.panelCheckBox.Visibility = Visibility.Collapsed;
            w.Owner = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Title = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING_STOP;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING_STOP;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING_STOP_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_STOP_RECORDING;
            w.ShowDialog();
            return w.isOK;
        }
        public static bool ShowStreamingConfirmWindow(out bool enablePwd)
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.panelCheckBox.Visibility = Visibility.Visible;
            w.Owner = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Title = Properties.Resources.FRTC_MEETING_CONFIRM_STREAMING;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_CONFIRM_STREAMING;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_CONFIRM_STREAMING_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_START_STREAMING;
            w.ShowDialog();
            enablePwd = w.StreamingPassword;
            return w.isOK;
        }

        public static bool ShowStreamingMutedConfirmWindow()
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.panelCheckBox.Visibility = Visibility.Collapsed;
            w.Owner = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Title = Properties.Resources.FRTC_MEETING_STREAMING_MUTED;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_STREAMING_MUTED;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_STREAMING_MUTED_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_STREAMING_UNMUTE;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_STREAMING_CONTINUE_MUTED;
            w.ShowDialog();
            return w.isOK;
        }

        public static bool ShowStopStreamingConfirmWindow()
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.panelCheckBox.Visibility = Visibility.Collapsed;
            w.Owner = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Title = Properties.Resources.FRTC_MEETING_CONFIRM_STREAMING_STOP;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_CONFIRM_STREAMING_STOP;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_CONFIRM_STREAMING_STOP_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_STOP_STREAMING;
            w.ShowDialog();
            return w.isOK;
        }

        public static bool ShowCancelRecurringMeetingConfirmWindow(out bool cancelRecurrence)
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();
            w.cb.Content = Properties.Resources.FRTC_SDKAPP_RECURRING_MEETING_CANCEL;
            w.cb.IsChecked = false;
            w.cb.HorizontalAlignment = HorizontalAlignment.Center;
            w.cb.HorizontalContentAlignment = HorizontalAlignment.Center;
            w.tbText.Text = "";
            w.panelCheckBox.Visibility = Visibility.Visible;
            w.Owner = Application.Current.MainWindow;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Title = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE;
            w.tbTip.Visibility = Visibility.Collapsed;
            //w.tbText.Text = Properties.Resources.FRTC_MEETING_CANCEL_SCHEDULE_MSG;
            w.btnCancel.Content = Properties.Resources.FRTC_MEETING_LET_ME_HAVE_A_THINK;
            w.btnOK.Content = Properties.Resources.FRTC_MEETING_SDKAPP_CANCEL_SCHEDULE_OK;
            w.ShowDialog();
            cancelRecurrence = w.CancelRecurrence;
            return w.isOK;
        }

        public static bool ShowUpdateRecurringMeetingOptionWindow(out bool updateRecurring)
        {
            FRTCMessageBoxBig w = new FRTCMessageBoxBig();

            w.panelCheckBox.Visibility = Visibility.Collapsed;

            w.tbText.Text = Properties.Resources.FRTC_SDKAPP_UPDATE_RECURRING_TIP;
            w.Title = Properties.Resources.FRTC_MEETING_SDKAPP_EDIT_MEETING;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_SDKAPP_EDIT_MEETING;


            w.btnCancel.Content = Properties.Resources.FRTC_SDKAPP_RECURRING_MEETING_SINGLE_EDIT;
            w.btnOK.Content = Properties.Resources.FRTC_SDKAPP_RECURRING_MEETING_EDIT_SHORT;

            w.btnCancel.Width = 150;
            w.btnCancel.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            w.btnCancel.Foreground = new SolidColorBrush(Color.FromRgb(0x02, 0x6F, 0xFE));
            w.btnCancel.BorderBrush = new SolidColorBrush(Color.FromRgb(0x02, 0x6F, 0xFE));
            w.btnOK.Width = 150;

            w.btnOK.Click -= w.btnOK_Click;
            w.btnOK.Click += (s, e) => { w.isOK = true; w.UpdateRecurrence = true; w.Close(); };

            w.btnCancel.Click -= w.btnCancel_Click;
            w.btnCancel.Click += (s, e) => { w.isOK = true; w.UpdateRecurrence = false; w.Close(); };

            w.MinHeight = 160;
            w.Height = 160;

            w.btnClose.Visibility = Visibility.Visible;

            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Owner = Application.Current.MainWindow;

            w.ShowDialog();
            updateRecurring = w.UpdateRecurrence;
            return w.isOK;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            this.StreamingPassword = ((CheckBox)sender).IsChecked.Value;
            this.CancelRecurrence = ((CheckBox)sender).IsChecked.Value;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            isOK = false;
            this.Close();
        }
    }
}
