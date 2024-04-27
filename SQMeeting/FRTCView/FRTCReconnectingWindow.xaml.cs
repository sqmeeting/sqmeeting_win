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
    /// FRTCReconnectingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCReconnectingWindow : Window
    {
        public FRTCReconnectingWindow()
        {
            InitializeComponent();
        }

        public void ShowSpinner()
        {
            this.dlg.Visibility = Visibility.Collapsed;
            this.spinner.Visibility = Visibility.Visible;
        }

        public void ShowDlg()
        {
            this.spinner.Visibility = Visibility.Collapsed;
            this.dlg.Visibility = Visibility.Visible;
        }
    }
}
