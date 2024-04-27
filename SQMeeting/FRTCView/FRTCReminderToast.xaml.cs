using SQMeeting.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// FRTCReminderToast.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCReminderToast : Window
    {
        static FRTCReminderToast w = null;
        private FRTCReminderToast()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            w = null;
        }

        public static void ShowRecordingStartReminder(bool showInCenterScreen = false)
        {
            if (w != null)
            {
                w.Close();
            }
            w = new FRTCReminderToast();
            w.Title = Properties.Resources.FRTC_MEETING_MEETING_RECORDING;
            w.tbTitle.Text = Properties.Resources.FRTC_MEETING_MEETING_RECORDING;
            w.tbText.Text = Properties.Resources.FRTC_MEETING_CONFIRM_RECORDING_STOP_MSG;
            Window incallWnd = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.Owner = incallWnd;
            if (showInCenterScreen)
            {
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                if (incallWnd.WindowState == WindowState.Maximized)
                {
                    Screen curScreen = System.Windows.Forms.Screen.FromHandle(FRTCUIUtils.MeetingWindowHandle);
                    uint dpiX = 0, dpiY = 0;
                    double scale = 1;
                    ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                    if (dpiX > 96)
                        scale = (double)dpiX / 96;


                    w.Left = (curScreen.WorkingArea.Right / scale) - w.Width - 5;

                    Window toolbar = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>().MeetingToolBar;
                    double toobarHeight = toolbar == null ? 56 : toolbar.ActualHeight;
                    w.Top = (curScreen.WorkingArea.Bottom / scale) - w.Height - toobarHeight - 5;
                }
                else
                {
                    w.Left = incallWnd.Left + incallWnd.ActualWidth - w.Width - 4;
                    Window toolbar = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>().MeetingToolBar;
                    double toobarHeight = toolbar == null ? 56 : toolbar.ActualHeight;
                    w.Top = incallWnd.Top + incallWnd.ActualHeight - w.Height - 4 - toobarHeight;
                }
            }
            w.Closed += (s, e) => { w = null; };
            w.LostFocus += (s, e) => { w?.Dispatcher.InvokeAsync(() => w?.Close()); };
            w.Show();
            w.Activate();
        }

        public static void ShowUnmuteApplicationReminder(string userName, bool showInCenterScreen = false)
        {
            if (w != null)
            {
                w.Close();
            }
            w = new FRTCReminderToast();
            w.btnGotIt.Visibility = Visibility.Collapsed;
            w.btnIgnore.Visibility = Visibility.Visible;
            w.btnWatch.Visibility = Visibility.Visible;
            w.Title = string.Empty;
            w.tbTitle.Text = string.Empty;
            w.tbText.Text = string.Format(Properties.Resources.FRTC_MEETING_UNMUTE_APPLICATION_NOTIFY, userName);
            Window incallWnd = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>()._meetingVideoWnd;
            w.Owner = incallWnd;
            if (showInCenterScreen)
            {
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                if (incallWnd.WindowState == WindowState.Maximized)
                {
                    IntPtr hwnd = new WindowInteropHelper(w).Handle;
                    Screen s = Screen.FromHandle(hwnd);
                    System.Drawing.Rectangle bounds = s.WorkingArea;
                    double scale = PresentationSource.FromVisual(incallWnd).CompositionTarget.TransformToDevice.M11;
                    w.Left = (bounds.Right / scale) - w.Width - 4;
                    w.Top = (bounds.Bottom / scale) - w.Height  - 4;
                }
                else
                {
                    w.Left = incallWnd.Left + incallWnd.ActualWidth - w.Width - 4;
                    Window toolbar = CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>().MeetingToolBar;
                    double toobarHeight = toolbar == null ? 56 : toolbar.ActualHeight;
                    w.Top = incallWnd.Top + incallWnd.ActualHeight - w.Height - 4 - toobarHeight;
                }
            }
            w.Closed += (s, e) => { w = null; };
            w.LostFocus += (s, e) => { w?.Dispatcher.InvokeAsync(() => w?.Close()); };
            w.Show();
            w.Activate();
        }

        public static void CloseReminder()
        {
            w?.Close();
            w = null;
        }

        private void btnIgnore_Click(object sender, RoutedEventArgs e)
        {
            w?.Close();
            w = null;
        }

        private void btnWatch_Click(object sender, RoutedEventArgs e)
        {
            CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.FRTCMeetingVideoViewModel>().PopupUnmuteApplicationList.Execute(null);
            w?.Close();
            w = null;
        }
    }
}
