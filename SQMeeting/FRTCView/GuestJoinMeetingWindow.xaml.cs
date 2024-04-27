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
using GalaSoft.MvvmLight.Messaging;
using SQMeeting.MvvMMessages;


namespace SQMeeting.FRTCView
{
    /// <summary>
    /// GuestJoinMeetingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GuestJoinMeetingWindow : Window
    {
        public GuestJoinMeetingWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<FRTCCallStateChangeMessage>(this, OnCallStateChanged);
        }

        private void OnCallStateChanged(FRTCCallStateChangeMessage msg)
        {
            if (msg.callState == FrtcCallState.CONNECTED)
            {
                this.Close();
            }
        }
    }
}
