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
    /// RecurringMeetingSeriesWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RecurringMeetingSeriesWindow : Window
    {
        public RecurringMeetingSeriesWindow()
        {
            InitializeComponent();
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

        private void meetingMenu_Opened(object sender, EventArgs e)
        {
            //listShield.Visibility = Visibility.Visible;
        }

        private void meetingMenu_Closed(object sender, EventArgs e)
        {
            //listShield.Visibility = Visibility.Collapsed;
        }
    }
}
