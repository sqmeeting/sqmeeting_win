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
using WPFMediaKit.DirectShow.MediaPlayers;
using System.ComponentModel;
using SQMeeting.Model;
using System.Collections.ObjectModel;
using SQMeeting.ViewModel;
using System.Threading;
using System.Windows.Threading;
using SQMeeting.Model.DataObj;
using GalaSoft.MvvmLight.Ioc;
using SQMeeting.Utilities;
using System.Windows.Interop;

namespace SQMeeting.View
{
    /// <summary>
    /// Interaction logic for RosterListWindow.xaml
    /// </summary>
    public partial class RosterListWindow : Window
    {
        public RosterListWindow()
        {
            InitializeComponent();
            this.Loaded += RosterListWindow_Loaded;
            Closed += RosterListWindow_Closed;
            this.Closing += RosterListWindow_Closing;
            this.Deactivated += RosterListWindow_Deactivated;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
        }

        private void RosterListWindow_Closing(object sender, CancelEventArgs e)
        {
            Owner?.Activate();
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            self.rosterListWindow = null;
        }

        private void RosterListWindow_Deactivated(object sender, EventArgs e)
        {

        }

        private void RosterListWindow_Closed(object sender, EventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= new EventHandler(SystemEvents_DisplaySettingsChanged);
        }
        private void RosterListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FRTCUIUtils.RosterWindowHandle = new WindowInteropHelper(this).Handle;
        }
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
        }
    }
}