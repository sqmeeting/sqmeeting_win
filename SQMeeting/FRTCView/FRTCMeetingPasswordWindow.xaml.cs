using CommonServiceLocator;
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
    /// FRTCMeetingPasswordWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCMeetingPasswordWindow : Window
    {
        public FRTCMeetingPasswordWindow()
        {
            InitializeComponent();
            Loaded += FRTCMeetingPasswordWindow_Loaded;
        }

        private void FRTCMeetingPasswordWindow_Loaded(object sender, RoutedEventArgs e)
        {
            pbPwd.Focus();
            Keyboard.Focus(pbPwd);
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ServiceLocator.Current.GetInstance<ViewModel.JoinMeetingViewModel>().FRTCMeetingPWD = ((PasswordBox)sender).Password;
        }
    }
}
