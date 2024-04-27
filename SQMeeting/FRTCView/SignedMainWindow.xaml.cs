using SQMeeting.Model;
using SQMeeting.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// SignedMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SignedMainWindow : Window
    {
        public SignedMainWindow()
        {
            InitializeComponent();
            Loaded += SignedMainWindow_Loaded;
        }

        private void SignedMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            CommonServiceLocator.ServiceLocator.Current.GetInstance<DeviceManager>().InitDeviceWatcher(hwnd);
            HwndSource.FromHwnd(hwnd)
               .AddHook(new HwndSourceHook(WndProc));
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0112)//WM_SYSCOMMAND
            {
                int sc = (FRTCUIUtils.LOWORD(wParam) & 0xFFF0);
                if (sc == 0xF060)//SC_CLOSE
                {
                    LogTool.LogHelper.Debug("Shutdown by sys command signed main wnd");
                    (App.Current as App).RemoveMutex();
                    Environment.Exit(0);
                }
            }
            else if (msg == 0x0010)//WM_CLOSE
            {
                return IntPtr.Zero;
            }
            else if (msg == Win32API.WM_COPYDATA)
            {
                try
                {
                    tagCOPYDATASTRUCT data = Marshal.PtrToStructure<tagCOPYDATASTRUCT>(lParam);
                    if (data.lpData != IntPtr.Zero)
                    {
                        byte[] buffer = new byte[data.cbData];
                        Marshal.Copy(data.lpData, buffer, 0, (int)data.cbData);
                        string callUrl = Encoding.Unicode.GetString(buffer);
                        CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.MainViewModel>().CheckSchemaMsg(callUrl);
                        App.Current.MainWindow.Show();
                        App.Current.MainWindow.Activate();
                    }
                }
                catch { }
                handled = true;
                
            }
            return IntPtr.Zero;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta/10));
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void meetingMenu_Opened(object sender, EventArgs e)
        {
            listShield.Visibility = Visibility.Visible;
        }

        private void meetingMenu_Closed(object sender, EventArgs e)
        {
            listShield.Visibility = Visibility.Collapsed;
        }

        private void Grid_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
