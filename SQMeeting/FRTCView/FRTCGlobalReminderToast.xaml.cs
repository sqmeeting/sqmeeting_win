using SQMeeting.Model;
using SQMeeting.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// FRTCGlobalReminderToast.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCGlobalReminderToast : Window
    {
        public FRTCGlobalReminderToast()
        {
            InitializeComponent();
        }

        static FRTCGlobalReminderToast window = null;
        public static void ShowGlobalReminder(ICollection<ScheduledMeetingDislpayData> schedules)
        {
            if (window != null)
            {
                window.Close();
            }
            window = new FRTCGlobalReminderToast();
            window.Closed += (s, e) => window = null;
            window.reminderList.ItemsSource = schedules;
            window.tbTitle.Text = string.Format(
                schedules.Count > 1 ?
                Properties.Resources.FRTC_MEETING_MULTI_MEETING_REMINDER_TITLE
                : Properties.Resources.FRTC_MEETING_MEETING_REMINDER_TITLE,
                schedules.Count());
            window.Show();
            var desktopWorkingArea = SystemParameters.WorkArea;
            window.Left = desktopWorkingArea.Right - window.ActualWidth;
            window.Top = desktopWorkingArea.Bottom - window.ActualHeight;
            window.Activate();
        }

        public static void CloseGlobalReminder()
        {
            if (window != null)
            {
                window.Close();
            }
        }

        public static void RemoveReminder(ScheduledMeetingDislpayData data)
        {
            try
            {
                if (window != null && window.reminderList != null && window.reminderList.ItemsSource != null)
                {
                    var source = window.reminderList.ItemsSource as ICollection<ScheduledMeetingDislpayData>;
                    source.Remove(data);
                    window.reminderList.ItemsSource = null;
                    window.reminderList.ItemsSource = source;
                    window.tbTitle.Text = string.Format(
                        source.Count > 1 ?
                        Properties.Resources.FRTC_MEETING_MULTI_MEETING_REMINDER_TITLE
                        : Properties.Resources.FRTC_MEETING_MEETING_REMINDER_TITLE,
                        source.Count());
                }
            }
            catch (Exception e) { LogTool.LogHelper.Exception(e); }
        }

        public static ICollection<ScheduledMeetingDislpayData> GetReminders()
        {
            if (window != null && window.reminderList != null && window.reminderList.ItemsSource != null)
            {
                return window.reminderList.ItemsSource as ICollection<ScheduledMeetingDislpayData>;
            }
            else
                return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            window?.Close();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 10));
            e.Handled = true;
        }

        private void Grid_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        private void JoinMeeting_Click(object sender, RoutedEventArgs e)
        {
            if (CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCCallManager>().CurrentCallState == FrtcCallState.CONNECTED)
            {
                Messenger.Default.Send<NotificationMessage<string>>(
                    new NotificationMessage<string>(string.Empty, "join_other_meeting_notify"));
                window?.Close();
                return;
            }
            ScheduledMeetingDislpayData d = (sender as Button).DataContext as ScheduledMeetingDislpayData;
            if (d != null)
            {
                Messenger.Default.Send<NotificationMessage<ScheduledMeetingDislpayData>>(
                    new NotificationMessage<ScheduledMeetingDislpayData>(d, "join_meeting_reminder"));
            }
            window?.Close();
        }

        private void Rectangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
