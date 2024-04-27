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
using System.Windows.Interop;
using SQMeeting.ViewModel;
using System.ComponentModel;
using SQMeeting.Model;
using GalaSoft.MvvmLight.Ioc;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.Windows.Media.Effects;

namespace SQMeeting.View
{
    /// <summary>
    /// Interaction logic for ContentSharingToolBar.xaml
    /// </summary>
    public partial class ContentSharingToolBar : Window
    {
        bool _isFold = false;

        public ContentSharingToolBar()
        {
            InitializeComponent();

            this.MouseEnter += ContentSharingToolBar_MouseEnter;
            this.SizeChanged += ContentSharingToolBar_SizeChanged;
        }

        private void ContentSharingToolBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustPosition(e.NewSize.Width);
            e.Handled = true;
        }

        private void ContentSharingToolBar_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (e.OldDpi.DpiScaleX == e.NewDpi.DpiScaleX
                && e.OldDpi.DpiScaleY == e.NewDpi.DpiScaleY
                && e.OldDpi.PixelsPerInchX == e.NewDpi.PixelsPerInchX
                && e.OldDpi.PixelsPerInchY == e.NewDpi.PixelsPerInchY)
                return;
            AdjustPosition(this.ActualWidth);
            e.Handled = true;
        }

        private void AdjustPosition(double width)
        {
            if (width == 0.0)
            {
                width = this.ActualWidth;
            }
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            System.Windows.Forms.Screen primaryScreen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => { return s.Primary; });
            if (primaryScreen == null)
            {
                Point pt = Mouse.GetPosition(self._meetingVideoWnd);
                primaryScreen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)pt.X, (int)pt.Y));
            }
            if (self.CurMonInfo != null || self.CurSharingWndHwnd != IntPtr.Zero)
            {
                double scale = 1.0;
                uint dpiX = 0;
                uint dpiY = 0;
                Utilities.ResolutionTools.GetDpi(primaryScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                if (dpiX > 96)
                    scale = (double)dpiX / 96;
                double left = 0.0;
                double right = 0.0;

                left = primaryScreen.Bounds.Left / scale;
                right = primaryScreen.Bounds.Right / scale;
                double top = primaryScreen.Bounds.Top / scale;
                if (this.Top != top)
                    this.Top = top;

                double revise = 0.0;
                if (left != 0)
                {
                    revise = left;
                }
                double targetLeft = (((primaryScreen.Bounds.Width / scale) - width) / 2) + revise;
                if (this.Left != targetLeft)
                    this.Left = targetLeft;
            }
        }

        public double _minHeight = 8;
        public double _maxHeight = 64;

        private void ContentSharingToolBar_MouseEnter(object sender, MouseEventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.TimerShowSharingBar();
            this.AdjustPos();
            this.Activate();

            this.Deactivated -= SharingBar_Deactivated;
            this.Deactivated += SharingBar_Deactivated;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.StopShareContent();
        }

        public Storyboard StoryboardMsg;
        public DoubleAnimation DoubleAnimationMsg;

        private bool IsMoving = false;

        public void updateWidth()
        {
            var TargethwndSource2 = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource2 == null)
            {
                return;
            }
            double scale = TargethwndSource2.CompositionTarget.TransformToDevice.M11;

            var hwnd2 = TargethwndSource2.Handle;
            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);

            int screenwidth = curScreen.Bounds.Right - curScreen.Bounds.Left;

            double shareingBarWidth = screenwidth / 3.0;
        }

        public void AdjustPos()
        {
            StartShowOrHide(_isFold);
        }
        public void StartShowOrHide(bool show)
        {
            try
            {
                _isFold = show;
                if (show)
                {
                    tbDuration.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                    layoutbd.Background = new SolidColorBrush(Colors.White);
                    layoutbd.Opacity = 1;
                    fullBar.Visibility = Visibility.Visible;
                    fullBar.Opacity = 1;
                    this.Height = 102;
                }
                else
                {
                    tbDuration.Foreground = new SolidColorBrush(Color.FromRgb(0xde, 0xde, 0xde));
                    layoutbd.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                    layoutbd.Opacity = 0.8;
                    fullBar.Visibility = Visibility.Collapsed;

                    this.Height = 34;
                    this.tbStreamingURL.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }
        }

        private void SharingBar_Deactivated(object sender, EventArgs e)
        {
            StartShowOrHide(false);
        }

        private void DoubleAnimationMsg_Completed(object sender, EventArgs e)
        {
            IsMoving = false;
        }
    }
}
