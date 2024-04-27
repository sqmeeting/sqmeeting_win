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
using SQMeeting.ViewModel;
using System.ComponentModel;
using SQMeeting.Model;
using GalaSoft.MvvmLight.Ioc;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SQMeeting.View
{
    /// <summary>
    /// Interaction logic for SharingFrame.xaml
    /// </summary>
    public partial class SharingFrame : Window
    {
        DispatcherTimer _sharingWndFrameTimer = null;
        double scalerate = 1.0;
        public SharingFrame()
        {
            InitializeComponent();
            this.Closed += SharingFrame_Closed;
        }

        private void SharingFrame_Closed(object sender, EventArgs e)
        {
            if (_sharingWndFrameTimer != null)
                _sharingWndFrameTimer.Stop();
        }

        public void UpdateFrame()
        {
            var source = PresentationSource.FromVisual(this);
            if (source == null) return;

            scalerate = source.CompositionTarget.TransformToDevice.M11;

            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self.CurMonInfo != null)
            {
                self.CurMonInfo = self.GetMonitorList(self.CurMonInfo.index);

                this.Left = self.CurMonInfo.left / scalerate;
                this.Top = self.CurMonInfo.top / scalerate;
                this.Width = (self.CurMonInfo.right - self.CurMonInfo.left) / scalerate;
                this.Height = (self.CurMonInfo.bottom - self.CurMonInfo.top) / scalerate;

                var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
                if (TargethwndSource == null)
                {
                    return;
                }
                var hwnd2 = TargethwndSource.Handle;

                System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
                this.Left = curScreen.Bounds.Left / scalerate;
                this.Top = curScreen.Bounds.Top / scalerate;
                this.Width = (curScreen.Bounds.Right - curScreen.Bounds.Left) / scalerate;
                this.Height = (curScreen.Bounds.Bottom - curScreen.Bounds.Top) / scalerate;

                this.lefttop.Width = this.lefttop.Height = this.Height * (160d / 900d);
                this.leftbottom.Width = this.leftbottom.Height = this.Height * (160d / 900d);
                this.rightbottom.Width = this.rightbottom.Height = this.Height * (160d / 900d);
                this.righttop.Width = this.righttop.Height = this.Height * (160d / 900d);
            }
            else if (self.CurSharingWndHwnd != IntPtr.Zero)
            {
                updateWndFrame(self.CurSharingWndHwnd);
                if (_sharingWndFrameTimer == null)
                {
                    _sharingWndFrameTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Render, (s, e) =>
                    {
                        updateWndFrame(self.CurSharingWndHwnd);
                    }, this.Dispatcher);
                }
                _sharingWndFrameTimer.Start();
            }
        }

        private void updateWndFrame(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return;
            Win32API.RECT rect = new Win32API.RECT();
            if (Win32API.GetWindowRect(hwnd, out rect))
            {
                double scale = 1.0d;
                IntPtr hMonitor = Win32API.MonitorFromWindow(hwnd, 0x00000002);//MONITOR_DEFAULTTONEAREST
                if (hMonitor != IntPtr.Zero)
                {
                    uint dpiX, dpiY;
                    int ret = Utilities.ResolutionTools.GetDpiForMonitorInternal(hMonitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                    if (0 < ret)
                    {
                        scale = dpiX / 96d;
                    }
                    else
                    {
                        int err = Win32API.GetLastError();
                        LogTool.LogHelper.Error("GetDpiForMonitor Failed, return value is {0}, error code is {1}", ret, err);
                    }
                }
                else
                {
                    LogTool.LogHelper.Error("MonitorFromWindow Failed");
                }

                this.Left = rect.Left / scale;
                this.Top = rect.Top / scale;
                this.Width = (rect.Right - rect.Left) / scale;
                this.Height = (rect.Bottom - rect.Top) / scale;

                this.lefttop.Width = this.lefttop.Height = this.Height * (160d / 900d);
                this.leftbottom.Width = this.leftbottom.Height = this.Height * (160d / 900d);
                this.rightbottom.Width = this.rightbottom.Height = this.Height * (160d / 900d);
                this.righttop.Width = this.righttop.Height = this.Height * (160d / 900d);
            }
            IntPtr activeWnd = Win32API.GetForegroundWindow();
            if (activeWnd == hwnd)
            {
                this.Topmost = true;
            }
            this.Topmost = false;
            this.ShowInTaskbar = false;
        }
    }
}
