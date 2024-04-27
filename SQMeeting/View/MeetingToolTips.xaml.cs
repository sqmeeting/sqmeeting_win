using System;
using System.Windows;
using System.Windows.Interop;
using SQMeeting.ViewModel;
using SQMeeting.Utilities;

namespace SQMeeting.View
{
    public partial class MeetingToolTips : Window
    {
        public MeetingToolTips()
        {
            InitializeComponent();
            Loaded += MeetingToolTips_Loaded;
        }

        private void MeetingToolTips_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCMeetingVideoViewModel>();
            if (vm != null)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsShowTips")
            {
                var vm = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCMeetingVideoViewModel>();
                if (vm != null && vm.IsShowTips)
                {
                    if (vm.rosterListWindow != null && vm.rosterListWindow.Visibility == Visibility.Visible)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var thisHWNDSource = (PresentationSource.FromVisual(this)) as HwndSource;
                            if (thisHWNDSource != null)
                            {
                                RECT clientRect = new RECT();
                                FRTCUIUtils.GetClientRect(thisHWNDSource.Handle, out clientRect);
                                RECT wndRect = new RECT();
                                FRTCUIUtils.GetWindowRect(thisHWNDSource.Handle, out wndRect);
                                FRTCUIUtils.SetWindowPos(
                                    thisHWNDSource.Handle,
                                    FRTCUIUtils.HWND_TOP,
                                    wndRect.Left, wndRect.Top,
                                    clientRect.Right - clientRect.Left,
                                    clientRect.Bottom - clientRect.Top, 64);
                            }
                        }));
                    }
                }
            }
        }
    }
}
