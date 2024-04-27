/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:SQMeeting"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using SQMeeting.Model;
using System.Configuration;
using GalaSoft.MvvmLight.Messaging;

namespace SQMeeting.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<FRTCCallManager>();
            SimpleIoc.Default.Register<DeviceManager>();
            SimpleIoc.Default.Register<FRTCUserManager>();
            SimpleIoc.Default.Register<MeetingScheduleManager>(true);
            SimpleIoc.Default.Register<MeetingHistoryManager>(true);

            SimpleIoc.Default.Register<MainViewModel>(true);
            SimpleIoc.Default.Register<SettingViewModel>(true);
            SimpleIoc.Default.Register<FRTCUserViewModel>();
            SimpleIoc.Default.Register<FRTCMeetingVideoViewModel>(true);
            SimpleIoc.Default.Register<JoinMeetingViewModel>(true);

            Messenger.Default.Send(new NotificationMessage("viewmodel_locator_initialized"));
            
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public FRTCUserViewModel FRTCUser
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FRTCUserViewModel>();
            }
        }

        public SettingViewModel Setting
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SettingViewModel>();
            }
        }

        public JoinMeetingViewModel JoinMeeting
        {
            get
            {
                return ServiceLocator.Current.GetInstance<JoinMeetingViewModel>();
            }
        }

        public FRTCMeetingVideoViewModel FRTCMeetingVideo
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FRTCMeetingVideoViewModel>();
            }
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}