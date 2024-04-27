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
    /// FRTCSendMeetingMsgWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCSendMeetingMsgWindow : Window
    {
        public FRTCSendMeetingMsgWindow()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnDecrease_Click(object sender, RoutedEventArgs e)
        {
            string timesStr = tbRepeatTimes.Text;
            if (string.IsNullOrEmpty(timesStr))
            {
                timesStr = "1";
            }
            int nRepeat = 1;
            if (int.TryParse(timesStr, out nRepeat))
            {
                if (nRepeat > 1)
                    tbRepeatTimes.Text = nRepeat--.ToString();
            }
        }
        private void btnIncrease_Click(object sender, RoutedEventArgs e)
        {
            string timesStr = tbRepeatTimes.Text;
            if (string.IsNullOrEmpty(timesStr))
            {
                timesStr = "1";
            }
            int nRepeat = 1;
            if (int.TryParse(timesStr, out nRepeat))
            {
                tbRepeatTimes.Text = nRepeat++.ToString();
            }
        }

        private void Grid_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
