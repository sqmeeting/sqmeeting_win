using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// ScheduleMeetingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScheduleMeetingWindow : Window
    {
        public ScheduleMeetingWindow()
        {
            InitializeComponent();
        }

        private void Grid_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
