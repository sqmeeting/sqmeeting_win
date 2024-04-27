using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using SQMeeting.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using SQMeeting.Utilities;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using GalaSoft.MvvmLight.Messaging;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace SQMeeting.View
{
    /// <summary>
    /// Interaction logic for MeetingVideoWindow.xaml
    /// </summary>
    public partial class MeetingVideoWindow : Window
    {
        private MeetingVideoWindow()
        {
            InitializeComponent();
            registerEvents();

            Loaded += MeetingVideoWindow_Loaded;
            Closed += MeetingVideoWindow_Closed;
            //Closing += MeetingVideoWindow_Closing;
            SourceInitialized += MeetingVideoWindow_SourceInitialized;
            videoArea.SizeChanged += VideoArea_SizeChanged;
            this.SizeChanged += MeetingVideoWindow_SizeChanged; ;

            KeyUp += MeetingVideoWindow_KeyUp;
            LocationChanged += MeetingVideoWindow_LocationChanged;

            CommandBinding closeCommand = new CommandBinding(SystemCommands.CloseWindowCommand);
            closeCommand.Executed += (s, e) => { SystemCommands.CloseWindow((Window)e.Parameter); };
            this.CommandBindings.Add(closeCommand);

            this.ContentRendered += MeetingVideoWindow_ContentRendered;

            StateChanged += MeetingVideoWindow_StateChanged;
        }

        private void MeetingVideoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            this.Dispatcher.Invoke(() =>
            {
                adjustGloablMsg();
                //adjustToobarMsg();
                adjustSharingBar();
                adjustShareFrame();
                adjustToolTips();
                adjustStateMsg();
                adjustToobarMsg();
                self.AdjustMeetingMsgWnd();
            });
        }

        WindowState _lastState = WindowState.Normal;
        private void MeetingVideoWindow_StateChanged(object sender, EventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (_lastState == WindowState.Minimized)
            {
                self.RestartMessage();
            }
            if (WindowState == WindowState.Maximized)
            {
                this.Dispatcher.Invoke(() =>
                {
                    //adjustToobarMsg();
                    adjustSharingBar();
                    adjustShareFrame();
                    adjustToolTips();
                    adjustStateMsg();
                    adjustToobarMsg();
                    self.AdjustMeetingMsgWnd();
                    adjustGloablMsg();
                });
            }
            _lastState = this.WindowState;
        }

        private void MeetingVideoWindow_ContentRendered(object sender, EventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            this.Dispatcher.Invoke(() =>
            {
                self.ShowMeetingStateBar();
                self.ShowToolbar();
                self.ShowToolTips();
                self.AdjustMeetingMsgWnd();
            });
        }

        private void MeetingVideoWindow_SourceInitialized(object sender, EventArgs e)
        {
            this.videoArea.Child = _hwndHost;
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.OnWindowLoad();
            bEnableCLose = false;
        }

        ~MeetingVideoWindow()
        {
        }


        public void registerEvents()
        {
            SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {

        }

        private Rect _PrePostion;
        private bool _isFullScreen = false;

        public void Init()
        {
            ResolutionTools.SetContentSharing(false);
            _isFullScreen = false;

            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self.MeetingToolBar != null)
            {
                self.MeetingToolBar.dWidth = this.ActualWidth - (2 * (SystemParameters.BorderWidth + SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth));
                self.MeetingToolBar.Height = 60;
                self.MeetingToolBar.VerticalOffset = 0;
            }
        }


        double _minWidth = 0.0;
        double _minHeight = 0.0;
        public void ShrinkWindow(bool _isFull)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();

            if (self.MeetingToolBar != null)
            {
                self.MeetingToolBar.Hide();
                self.MeetingStateBar.Hide();
            }

            _isFullScreen = _isFull;

            self.IsSmallWnd = true;
            self.ShowSmall = self.ShowMeetingMsgWnd;
            self.IsShowMoreBtn = false;

            if (self.meetingMsgWnd != null)
            {
                self.meetingMsgWnd.HideMsg();
            }

            _PrePostion.X = this.Left;
            _PrePostion.Y = this.Top;
            _PrePostion.Width = this.Width;
            _PrePostion.Height = this.Height;

            _hwndHost.UpdateWindowPos();

            ResolutionTools.SetContentSharing(true);

            _minHeight = this.MinHeight;
            _minWidth = this.MinWidth;
            this.MinWidth = 0.0;
            this.MinHeight = 0.0;
            this.WindowState = WindowState.Normal;
            ResolutionTools.UpdateMeetingWindowResolution(System.Windows.Forms.Screen.PrimaryScreen);

            this.WindowStyle = WindowStyle.None;
            this.BorderBrush = new SolidColorBrush(Colors.Black);
            this.BorderThickness = new Thickness(1);
            this.captionBar.Visibility = Visibility.Visible;
            this.Topmost = true;
            this.ResizeMode = ResizeMode.NoResize;

            adjustToolTips();
            adjustRecordingStatusWidget();
            adjustStreamingStatusWidget();

        }

        public void RestoreWindow()
        {

            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.IsSmallWnd = false;

            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.captionBar.Visibility = Visibility.Collapsed;

            //this.ApplyTemplate();

            if (self.MeetingToolBar != null)
            {
                self.MeetingToolBar.dWidth = this.ActualWidth - (2 * (SystemParameters.BorderWidth + SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth));
                self.MeetingToolBar.Height = 60;
                self.MeetingToolBar.VerticalOffset = 0;
                self.MeetingToolBar.Show();
            }
            self.MeetingStateBar?.Show();

            this.Left = _PrePostion.X;
            this.Top = _PrePostion.Y;
            this.Width = _PrePostion.Width;
            this.Height = _PrePostion.Height;
            ResolutionTools.SetContentSharing(false);
            this.MinWidth = _minWidth;
            this.MinHeight = _minHeight;
            ResolutionTools.UpdateMeetingWindowResolution(self.FRTCMeetingWndScreen);
            RECT clientRect = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out clientRect);
            Console.Out.WriteLine("Set sdk incall wnd size: {0}, {1}", clientRect.Right - clientRect.Left, clientRect.Bottom - clientRect.Top);
            FRTCUIUtils.SetWindowPos(self.GetWrapperMainWndHandle(), FRTCUIUtils.HWND_TOP, 0, 0, clientRect.Right - clientRect.Left, clientRect.Bottom - clientRect.Top, 64);

            if (_isFullScreen)
            {
                this.WindowStyle = System.Windows.WindowStyle.None;
                //this.WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                this.ResizeMode = System.Windows.ResizeMode.CanResize;
                this.WindowState = _lastState;
            }

            self.ShowSmall = self.ShowMeetingMsgWnd;

            if (self.IsShowToolBar)
            {
                self.IsShowMoreBtn = true;
            }

            if (self.ShowMeetingMsgWnd && self.meetingMsgWnd != null)
            {
                self.meetingMsgWnd.StartShowMSg();
            }

            adjustToolTips();
            adjustStateMsg();
            adjustToobarMsg();
            adjustRecordingStatusWidget();
            adjustStreamingStatusWidget();

            this.Topmost = false;
        }

        private void MeetingVideoWindow_LocationChanged(object sender, EventArgs e)
        {
            //Tips.UpdateWindow(Tips.Topmost);
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            this.Dispatcher.Invoke(() =>
            {
                adjustSharingBar();
                adjustShareFrame();
                adjustToolTips();
                adjustStateMsg();
                adjustToobarMsg();
                self.AdjustMeetingMsgWnd();

                adjustRecordingStatusWidget();
                adjustStreamingStatusWidget();
            });
        }

        public void adjustShareFrame()
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();

            if (self == null)
            {
                return;
            }
            self.adjustSharingFrame();

        }

        public void adjustToobarMsg(bool isFullScreen = false)
        {
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self.MeetingToolBar == null)
                return;

            double scale = 1;
            if (self == null)
            {
                return;
            }
            var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }
            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT Rect;
            Win32API.GetWindowRect(hwnd2, out Rect);

            LogTool.LogHelper.Debug("adjustToobarMsg incall window rect is {0},{1},{2},{3}", Rect.Left, Rect.Top, Rect.Right, Rect.Bottom);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out rect1);

            LogTool.LogHelper.Debug("adjustToobarMsg incall client rect is {0},{1},{2},{3}", rect1.Left, rect1.Top, rect1.Right, rect1.Bottom);

            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
            uint dpiX = 0, dpiY = 0;
            ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            LogTool.LogHelper.Debug("adjustToobarMsg current screen dpiX is {0}, dpiY is {1}", dpiX, dpiY);

            if (dpiX > 96)
                scale = (double)dpiX / 96;

            LogTool.LogHelper.Debug("adjustToobarMsg current screen sclae factor is {0}", scale);

            double x = 0;
            double y = 0;
            double dwidth = 0;
            {
                int left2 = curScreen.Bounds.Left;
                int top2 = curScreen.Bounds.Top;

                dwidth = rect1.Right - rect1.Left;
                double borderWidth = ((Rect.Right - Rect.Left) - (rect1.Right - rect1.Left)) / 2;
                x = Rect.Left + borderWidth;

                if (self.IsFullScreen && self.ShareContentEnable)
                    y = Rect.Bottom;
                else
                    y = Rect.Bottom - borderWidth;

            }

            if (dwidth == 0)
                return;

            if (self.MeetingToolBar != null)
            {
                self.MeetingToolBar.Left = x / scale;
                self.MeetingToolBar.Top = (y / scale) - self.MeetingToolBar.ActualHeight;

                self.MeetingToolBar.MinWidth = dwidth / scale;
                self.MeetingToolBar.MaxWidth = dwidth / scale;
            }
        }

        public void adjustSharingBar()
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (!self.IsSendingContent)
                return;
            if (self._sharingToolBar == null)
                return;

            if (self.CurMonInfo == null || self.CurSharingWndHwnd == IntPtr.Zero)
                return;

            self._sharingToolBar.AdjustPos();
            return;
        }

        public void adjustToolTips()
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self.MeetingToolTips == null)
                return;

            double scale = 1;
            if (self == null)
            {
                return;
            }
            var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }
            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT Rect;
            Win32API.GetWindowRect(hwnd2, out Rect);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out rect1);

            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
            uint dpiX = 0, dpiY = 0;
            ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            if (dpiX > 96)
                scale = (double)dpiX / 96;

            double x = 0;
            double y = 0;
            double dwidth = 0;


            dwidth = rect1.Right - rect1.Left;

            double borderWidth = ((Rect.Right - Rect.Left) - (rect1.Right - rect1.Left)) / 2;
            double borderHeight = ((Rect.Bottom - Rect.Top) - (rect1.Bottom - rect1.Top)) / 2;

            self.MeetingToolTips.MaxWidth = this.Width;// (rect1.Right - rect1.Left) / scale;

            if (self.MeetingToolTips.MaxWidth < 144)
            {
                self.MeetingToolTips.MinWidth = 120 / scale;
                self.MeetingToolTips.MinHeight = 48 / scale;
                self.MeetingToolTips.MaxHeight = 48 / scale;
            }
            else if (self.MeetingToolTips.MaxWidth < 900)
            {
                self.MeetingToolTips.MinWidth = 400 / scale;
                self.MeetingToolTips.MinHeight = 60 / scale;
                self.MeetingToolTips.MaxHeight = 60 / scale;
            }
            else if (self.MeetingToolTips.MaxWidth < 1800)
            {
                self.MeetingToolTips.MinWidth = 600 / scale;
                self.MeetingToolTips.MinHeight = 70 / scale;
                self.MeetingToolTips.MaxHeight = 70 / scale;
            }
            else
            {
                self.MeetingToolTips.MinWidth = 800 / scale;
                self.MeetingToolTips.MinHeight = 80 / scale;
                self.MeetingToolTips.MaxHeight = 80 / scale;
            }

            self.MeetingToolTips.Width = double.NaN;

            if (self.IsSendingContent)
            {
                self.MeetingToolTips.Left = (((double)curScreen.Bounds.Width / scale) - self.MeetingToolTips.ActualWidth) / 2;
                self.MeetingToolTips.Top = (((double)curScreen.Bounds.Height / scale) - self.MeetingToolTips.ActualHeight) / 2;
            }
            else
            {
                self.MeetingToolTips.Left = (Rect.Left + ((Rect.Right - Rect.Left) - self.MeetingToolTips.ActualWidth * scale) / 2) / scale;
                self.MeetingToolTips.Top = (Rect.Top + ((Rect.Bottom - Rect.Top) - self.MeetingToolTips.Height * scale) / 2) / scale;
            }
        }

        public void adjustStateMsg()
        {
            double scale = 1;
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self == null || self.IsSendingContent)
            {
                return;
            }
            var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }
            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT Rect;
            Win32API.GetWindowRect(hwnd2, out Rect);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out rect1);

            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
            uint dpiX = 0, dpiY = 0;
            ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            if (dpiX > 96)
                scale = (double)dpiX / 96;

            double x = 0;
            double y = 0;
            double dwidth = 0;
            {
                int left2 = curScreen.Bounds.Left;
                int top2 = curScreen.Bounds.Top;

                {
                    dwidth = rect1.Right - rect1.Left;
                    double borderWidth = ((Rect.Right - Rect.Left) - (rect1.Right - rect1.Left)) / 2;
                    x = Rect.Left + borderWidth;

                    if (self.IsFullScreen && self.ShareContentEnable)
                        y = Rect.Top;// + System.Windows.Forms.SystemInformation.CaptionHeight + borderWidth;//((Rect.Bottom - Rect.Left) - (rect1.Right - rect1.Left)) / 2;
                    else
                        y = Rect.Top + System.Windows.Forms.SystemInformation.CaptionHeight + borderWidth;
                }
            }

            if (dwidth == 0)
                return;

            if (self.MeetingStateBar != null)
            {
                self.MeetingStateBar.Left = x / scale;
                self.MeetingStateBar.Top = y / scale;

                self.MeetingStateBar.MinWidth = dwidth / scale;
                self.MeetingStateBar.MaxWidth = dwidth / scale;

                int widthstate = 37;
                if (self.MicMuted)
                {
                    widthstate += 37;
                }

                if (self.CameraMuted)
                {
                    widthstate += 37;
                }

                if ((dwidth - widthstate * 2) > 0)
                {
                    self.MeetingStateBar.textCtrl.MinWidth = (dwidth - widthstate * 2) / scale;
                    self.MeetingStateBar.textCtrl.MaxWidth = (dwidth - widthstate * 2) / scale;
                }

            }
        }

        public void adjustGloablMsg()
        {
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            double scale = 1.0;
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self == null)
            {
                return;
            }
            var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }

            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT Rect;
            Win32API.GetWindowRect(hwnd2, out Rect);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetClientRect(hwnd2, out rect1);

            if (self.meetingMsgWnd != null)
            {
                System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
                uint dpiX = 0, dpiY = 0;
                ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                if (dpiX > 96)
                    scale = (double)dpiX / 96;

                double x = 0;
                double y = 0;
                double dwidth = 0;
                {
                    int left2 = curScreen.Bounds.Left;
                    int top2 = curScreen.Bounds.Top;

                    if (self.IsFullScreen)
                    {
                        x = curScreen.Bounds.Left;
                        y = (double)((top2 + curScreen.Bounds.Height) / scale) * ((double)self.MeetingMsgInfo.MsgVerticalPosition / 100);
                        dwidth = curScreen.Bounds.Right - curScreen.Bounds.Left;

                        if (self.MeetingMsgInfo.MsgVerticalPosition == 0)
                            y = y + (self.MeetingStateBar == null ? 0 : self.MeetingStateBar.ActualHeight);
                        else if (self.MeetingMsgInfo.MsgVerticalPosition == 100)
                            y = y - (self.MeetingToolBar == null ? 56 : self.MeetingToolBar.ActualHeight) - self.meetingMsgWnd.Height;
                        else
                            y = y - (self.meetingMsgWnd.ActualHeight / 2);
                    }
                    else
                    {

                        dwidth = rect1.Right - rect1.Left;
                        double borderWidth = ((Rect.Right - Rect.Left) - (rect1.Right - rect1.Left)) / 2;
                        x = Rect.Left + borderWidth;
                        if (this.WindowState == WindowState.Maximized)
                        {
                            y = curScreen.WorkingArea.Top + SystemParameters.CaptionHeight + 8 + ((double)(this.ActualHeight) * ((double)self.MeetingMsgInfo.MsgVerticalPosition / 100.0));
                        }
                        else
                        {
                            y = this.Top + SystemParameters.CaptionHeight + 8 /* frame width */ + ((double)(this.ActualHeight) * ((double)self.MeetingMsgInfo.MsgVerticalPosition / 100.0));
                        }

                        if (self.MeetingMsgInfo.MsgVerticalPosition == 0)
                            y += self.MeetingStateBar == null ? 0 : self.MeetingStateBar.ActualHeight;
                        else if (self.MeetingMsgInfo.MsgVerticalPosition == 100)
                        {
                            y = y - (self.MeetingToolBar == null ? 56 : self.MeetingToolBar.ActualHeight) - self.meetingMsgWnd.Height - (8 * 2)/* top and botton frame width */ - SystemParameters.CaptionHeight;
                        }
                    }

                }

                self.meetingMsgWnd.Left = x / scale;
                self.meetingMsgWnd.Top = y;
                self.meetingMsgWnd.MinWidth = dwidth / scale;
                self.meetingMsgWnd.MaxWidth = dwidth / scale;
                self.meetingMsgWnd.MinHeight = 40 / scale;
                self.meetingMsgWnd.MaxHeight = 40 / scale;

            }

            //}));
        }

        public void adjustRecordingStatusWidget()
        {
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            double scale = 1.0;
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self == null || self.recordingStatusWidget == null)
            {
                return;
            }
            var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }
            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT wndRect;
            Win32API.GetWindowRect(hwnd2, out wndRect);

            RECT clientRect = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out clientRect);

            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
            uint dpiX = 0, dpiY = 0;
            ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            if (dpiX > 96)
                scale = (double)dpiX / 96;

            LogTool.LogHelper.Debug("adjustRecordingStatusWidget incall window rect is {0},{1},{2},{3}", wndRect.Left, wndRect.Top, wndRect.Right, wndRect.Bottom);

            LogTool.LogHelper.Debug("adjustRecordingStatusWidget incall client rect is {0},{1},{2},{3}", clientRect.Left, clientRect.Top, clientRect.Right, clientRect.Bottom);

            LogTool.LogHelper.Debug("adjustRecordingStatusWidget current screen dpiX is {0}, dpiY is {1}", dpiX, dpiY);

            LogTool.LogHelper.Debug("adjustRecordingStatusWidget current screen sclae factor is {0}", scale);


            double x = 0;
            double y = 0;
            double dwidth = 0;
            double marginY = 20;
            double marginX = 0;
            {
                int left2 = curScreen.Bounds.Left;
                int top2 = curScreen.Bounds.Top;

                dwidth = clientRect.Right - clientRect.Left;
                double borderWidth = ((wndRect.Right - wndRect.Left) - (clientRect.Right - clientRect.Left)) / 2;
                x = wndRect.Left + borderWidth;

                double screen_ratio = (double)curScreen.Bounds.Height / (double)curScreen.Bounds.Width;

                if (self.IsFullScreen && self.ShareContentEnable)
                {
                    y = wndRect.Top + (self.MeetingStateBar != null ? self.MeetingStateBar.ActualHeight : 40) + (self.meetingMsgWnd != null ? self.meetingMsgWnd.Height : 40)
                        + curScreen.Bounds.Height - ((double)(curScreen.Bounds.Width) * screen_ratio);
                    x += marginX;
                }
                else if (self.IsSendingContent)
                {

                    y = wndRect.Top + System.Windows.Forms.SystemInformation.CaptionHeight + borderWidth;
                    x += marginX + borderWidth;
                }
                else
                {
                    y = wndRect.Top + System.Windows.Forms.SystemInformation.CaptionHeight + borderWidth;
                    x += (marginX + borderWidth);
                }
            }

            if (dwidth == 0)
                return;

            if (self.recordingStatusWidget != null)
            {
                self.recordingStatusWidget.Width = 106;
                self.recordingStatusWidget.Height = 28;
                self.recordingStatusWidget.Left = x / scale;

                if (!self.IsFullScreen && !self.IsSendingContent)
                {
                    double vMargin = dwidth * (9d / 16d);
                    //if (WindowState == WindowState.Normal)
                    {
                        if (this.ActualWidth / this.ActualHeight > (16d / 9d))
                        {
                            double videoW = (this.ActualHeight / 9 * 16) / 5;
                            vMargin = videoW * (9d / 16d);
                        }
                        else
                        {
                            double videoW = this.ActualWidth / 5;
                            vMargin = videoW * (9d / 16d);
                        }
                    }
                    self.recordingStatusWidget.Top = (y / scale) + marginY + vMargin;
                }
                else
                {
                    self.recordingStatusWidget.Top = (y / scale) + marginY;
                }

                LogTool.LogHelper.Debug("recording widget left {0}, top {1}, width {2}, height {3}", self.recordingStatusWidget.Left, self.recordingStatusWidget.Top, self.recordingStatusWidget.Width, self.recordingStatusWidget.Height);

                int widthstate = 37;
                if (self.MicMuted)
                {
                    widthstate += 37;
                }

                if (self.CameraMuted)
                {
                    widthstate += 37;
                }
            }
            //}));
        }

        public void adjustStreamingStatusWidget()
        {
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            double scale = 1;
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            if (self == null || self.streamingStatusWidget == null)
            {
                return;
            }
            var TargethwndSource = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }
            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT wndRect;
            Win32API.GetWindowRect(hwnd2, out wndRect);

            RECT clientRect = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out clientRect);

            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd2);
            uint dpiX = 0, dpiY = 0;
            ResolutionTools.GetDpi(curScreen, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            if (dpiX > 96)
                scale = (double)dpiX / 96;


            double x = 0;
            double y = 0;
            double dwidth = 0;
            double marginY = 20;
            double marginX = 0;
            {
                int left2 = curScreen.Bounds.Left;
                int top2 = curScreen.Bounds.Top;

                dwidth = clientRect.Right - clientRect.Left;
                double borderWidth = ((wndRect.Right - wndRect.Left) - (clientRect.Right - clientRect.Left)) / 2;
                x = wndRect.Left + borderWidth;

                double screen_ratio = (double)curScreen.Bounds.Height / (double)curScreen.Bounds.Width;

                if (self.recordingStatusWidget != null)
                    marginY += (self.recordingStatusWidget.Height + 10);

                if (self.IsFullScreen && self.ShareContentEnable)
                {
                    y = wndRect.Top + (self.MeetingStateBar != null ? self.MeetingStateBar.ActualHeight : 40) + (self.meetingMsgWnd != null ? self.meetingMsgWnd.Height : 40)
                        + curScreen.Bounds.Height - ((double)(curScreen.Bounds.Width) * screen_ratio);
                    x += marginX;
                }
                else if (self.IsSendingContent)
                {
                    y = wndRect.Top + System.Windows.Forms.SystemInformation.CaptionHeight + borderWidth;
                    x += marginX + borderWidth;
                }
                else
                {
                    y = wndRect.Top + System.Windows.Forms.SystemInformation.CaptionHeight + borderWidth;
                    x += (marginX + borderWidth);
                }
            }

            if (dwidth == 0)
                return;

            if (self.streamingStatusWidget != null)
            {
                self.streamingStatusWidget.Width = 106;
                self.streamingStatusWidget.Height = 28;
                self.streamingStatusWidget.Left = x / scale;

                if (!self.IsFullScreen && !self.IsSendingContent)
                {
                    double vMargin = dwidth * (9d / 16d);
                    //if (WindowState == WindowState.Normal)
                    {
                        if (this.ActualWidth / this.ActualHeight > (16d / 9d))
                        {
                            double videoW = (this.ActualHeight / 9 * 16) / 5;
                            vMargin = videoW * (9d / 16d);
                        }
                        else
                        {
                            double videoW = this.ActualWidth / 5;
                            vMargin = videoW * (9d / 16d);
                        }
                    }
                    self.streamingStatusWidget.Top = (y / scale) + marginY + vMargin;
                }
                else
                {
                    self.streamingStatusWidget.Top = (y / scale) + marginY;
                }
            }
            //}));
        }


        public void Update()
        {
            //Tips.UpdateWindow(Tips.Topmost);   
        }


        private void MeetingVideoWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
                self.PressEsc();
            }
        }

        public MeetingVideoWindow(HwndHost hwndHost) : this()
        {
            _hwndHost = hwndHost;
        }

        HwndHost _hwndHost;

        public bool bEnableCLose = false;
        private void MeetingVideoWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            {
                Task.Run(new Action(() =>
                {
                    FRTCSDK.frtc_call_leave();
                }));
            }

        }

        private void MeetingVideoWindow_Closed(object sender, EventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            self.HideMeetingMSg();
            self.HideMeetingSateBar();
            self.HideToolbar();
            self.HideToolTips();

            Loaded -= MeetingVideoWindow_Loaded;
            Closed -= MeetingVideoWindow_Closed;
            //Closing -= MeetingVideoWindow_Closing;
            SourceInitialized -= MeetingVideoWindow_SourceInitialized;
            videoArea.SizeChanged -= VideoArea_SizeChanged;
            SizeChanged -= MeetingVideoWindow_SizeChanged;
            KeyUp -= MeetingVideoWindow_KeyUp;
            LocationChanged -= MeetingVideoWindow_LocationChanged;

            this.ContentRendered -= MeetingVideoWindow_ContentRendered;


            self._meetingVideoWnd.Close();
            self.m_meetingWndThread = null;
            self._videoWndHost = null;
            self._meetingVideoWnd = null;

            this.Dispatcher.InvokeShutdown();

        }

        private void VideoArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_hwndHost != null)
            {
                _hwndHost.Width = videoArea.Width;
                _hwndHost.Height = videoArea.Height;
            }
        }

        private void MeetingVideoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();

            var TargethwndSource = (PresentationSource.FromVisual(self._meetingVideoWnd)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }
            var hwnd = TargethwndSource.Handle;
            System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd);

            self.FRTCMeetingWndScreen = curScreen;

            adjustToolTips();
            adjustStateMsg();
            adjustToobarMsg();

            if (currentScreen == null)
            {
                currentScreen = curScreen;
            }

            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            currentDPI_X = 96.0 * TargethwndSource.CompositionTarget.TransformToDevice.M11;
            currentDPI_Y = 96.0 * TargethwndSource.CompositionTarget.TransformToDevice.M22;
            self.AdjustMeetingMsgWnd();


            adjustRecordingStatusWidget();
            adjustStreamingStatusWidget();
        }

        System.Windows.Forms.Screen currentScreen = null;
        double currentDPI_X = 1.0;
        double currentDPI_Y = 1.0;



        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32API.WM_NCMOUSEMOVE)
            {
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();

                bool screenChanged = false;
                System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd);

                if (self._meetingVideoWnd.currentScreen == null)
                {
                    self._meetingVideoWnd.currentScreen = curScreen;
                }
                if (!self._meetingVideoWnd.currentScreen.Equals(curScreen))
                {
                    screenChanged = true;
                    self._meetingVideoWnd.currentScreen = curScreen;
                }

                if (screenChanged)
                {
                    var TargethwndSource = (PresentationSource.FromVisual(self._meetingVideoWnd)) as HwndSource;

                    ResolutionTools.UpdateMeetingWindowResolution(curScreen);

                    self._meetingVideoWnd.adjustSharingBar();
                    self._meetingVideoWnd.adjustShareFrame();
                    self._meetingVideoWnd.adjustToolTips();
                    self._meetingVideoWnd.adjustStateMsg();
                    self._meetingVideoWnd.adjustToobarMsg(true);
                    self._meetingVideoWnd.adjustToobarMsg(false);

                    if (self.ShowMeetingMsgWnd && self.meetingMsgWnd != null)
                    {
                        self.meetingMsgWnd.StartShowMSg();
                    }
                    self.AdjustMeetingMsgWnd();
                    self.meetingMsgWnd?.Show();
                    self.meetingMsgWnd?.Activate();


                    self._meetingVideoWnd.adjustRecordingStatusWidget();
                    self._meetingVideoWnd.adjustStreamingStatusWidget();
                }

                handled = false;
            }
            else if (msg == Win32API.WM_DISPLAYCHANGE)
            {
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();

                if (!self.ShareContentEnable && self.GetMonitorChanged())
                {
                    bool willStopContent = true;
                    if (self.CurSharingWndHwnd != IntPtr.Zero)
                    {
                        uint processId = 0;
                        if (0 != GetWindowThreadProcessId(self.CurSharingWndHwnd, out processId))
                        {
                            if (processId != 0)
                            {
                                string processName = Process.GetProcessById((int)processId).ProcessName.ToLower();
                                if (processName == "powerpnt" || processName == "wps")
                                {
                                    willStopContent = false;
                                }
                            }
                        }
                    }
                    if (willStopContent)
                        self.StopShareContent();
                }
                else
                {
                    Console.Out.WriteLine("DisplaySettingsChanged");
                }
                System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd);

                PresentationSource source = PresentationSource.FromVisual(self._meetingVideoWnd);
                double dpiX = 1.0;
                double dpiY = 1.0;
                if (source != null)
                {
                    dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                }

                bool screenChanged = false;
                if (self._meetingVideoWnd.currentScreen == null)
                {
                    self._meetingVideoWnd.currentScreen = curScreen;
                }
                if (!self._meetingVideoWnd.currentScreen.Equals(curScreen)
                    || self._meetingVideoWnd.currentScreen.Bounds.Width != curScreen.Bounds.Width
                    || self._meetingVideoWnd.currentScreen.Bounds.Height != curScreen.Bounds.Height
                    || self._meetingVideoWnd.currentDPI_X != dpiX
                    || self._meetingVideoWnd.currentDPI_Y != dpiY)
                {
                    screenChanged = true;
                    self._meetingVideoWnd.currentScreen = curScreen;
                    self._meetingVideoWnd.currentDPI_X = dpiX;
                    self._meetingVideoWnd.currentDPI_Y = dpiY;
                }
                if (screenChanged)
                {
                    ResolutionTools.UpdateMeetingWindowResolution(self.FRTCMeetingWndScreen);
                }

                self._meetingVideoWnd.adjustSharingBar();
                self._meetingVideoWnd.adjustShareFrame();
                self._meetingVideoWnd.adjustToolTips();
                self._meetingVideoWnd.adjustStateMsg();
                self._meetingVideoWnd.adjustToobarMsg(true);
                self._meetingVideoWnd.adjustToobarMsg(false);
                self.AdjustMeetingMsgWnd();
                self._meetingVideoWnd.adjustRecordingStatusWidget();
                self._meetingVideoWnd.adjustStreamingStatusWidget();
            }

            else if (msg == Win32API.WM_EXITSIZEMOVE)
            {
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
                System.Windows.Forms.Screen curScreen = System.Windows.Forms.Screen.FromHandle(hwnd);
                bool screenChanged = false;
                if (self._meetingVideoWnd.currentScreen == null)
                {
                    self._meetingVideoWnd.currentScreen = curScreen;
                }
                if (!self._meetingVideoWnd.currentScreen.Equals(curScreen))
                {
                    screenChanged = true;
                    self._meetingVideoWnd.currentScreen = curScreen;
                }
                if (screenChanged)
                {
                    ResolutionTools.UpdateMeetingWindowResolution(self.FRTCMeetingWndScreen);
                }

                self._meetingVideoWnd.adjustSharingBar();
                self._meetingVideoWnd.adjustShareFrame();
                self._meetingVideoWnd.adjustToolTips();
                self._meetingVideoWnd.adjustStateMsg();
                self._meetingVideoWnd.adjustToobarMsg(true);
                self._meetingVideoWnd.adjustToobarMsg(false);
                self.AdjustMeetingMsgWnd();
                self._meetingVideoWnd.adjustRecordingStatusWidget();
                self._meetingVideoWnd.adjustStreamingStatusWidget();
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
                        string dataStr = Encoding.Unicode.GetString(buffer);
                        JObject info = null;
                        if (CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.MainViewModel>().TryGetSchemaMsgParamPlainText(dataStr, out info))
                        {
                            if (info != null)
                            {
                                CommonServiceLocator.ServiceLocator.Current.GetInstance<ViewModel.MainViewModel>().HandleAddToMeetingListMsg(info);
                            }
                        }
                        else
                        {
                            Messenger.Default.Send(new NotificationMessage("join_other_meeting_notify"));
                        }
                    }
                }
                catch { }
                handled = true;
            }

            return IntPtr.Zero;
        }
    }
}
