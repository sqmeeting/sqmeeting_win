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
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Ioc;
using SQMeeting.Utilities;
using SQMeeting.ViewModel;

namespace SQMeeting.View
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow()
        {
            InitializeComponent();
            Loaded += StatisticsWindow_Loaded;
            Closed += StatisticsWindow_Closed;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
        }
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
        }
        private void StatisticsWindow_Closed(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= new EventHandler(SystemEvents_DisplaySettingsChanged);
        }

        private void StatisticsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FRTCUIUtils.StasticsWindowHandle = new WindowInteropHelper(this).Handle;
            adjustStatisticWnd();
        }

        public void adjustStatisticWnd()
        {
            double scale = 0;
            if (this.Owner == null || !((FRTCMeetingVideoViewModel)DataContext).ShareContentEnable)
                return;
            var TargethwndSource = (PresentationSource.FromVisual(this.Owner)) as HwndSource;
            if (TargethwndSource == null)
            {
                return;
            }

            var hwnd2 = TargethwndSource.Handle;
            Win32API.RECT Rect;
            Win32API.GetWindowRect(hwnd2, out Rect);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetClientRect(hwnd2, out rect1);
            double borderWidth = ((Rect.Bottom - Rect.Top) - (rect1.Bottom - rect1.Top)) / 2;
            var TargethwndSource2 = (PresentationSource.FromVisual(this)) as HwndSource;
            if (TargethwndSource2 == null)
            {
                scale = 1;
            }
            else
            {
                scale = TargethwndSource2.CompositionTarget.TransformToDevice.M11;
            }

            double dW2H = this.Width / this.Height;
            this.Height =  (Rect.Bottom - Rect.Top) * 0.9 / scale;
            this.Width =  this.Height * dW2H;
            this.stDataGrid.Height = this.Height - borderWidth / scale - System.Windows.Forms.SystemInformation.CaptionHeight/ scale - 30 * 3 / scale-20/ scale;
            this.Left = (Rect.Left + ((Rect.Right - Rect.Left) - this.Width* scale) / 2)/ scale;
            this.Top = (Rect.Top + ((Rect.Bottom - Rect.Top) - this.Height * scale) / 2) / scale;
        }
    }
}
