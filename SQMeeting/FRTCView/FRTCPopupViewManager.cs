using SQMeeting.MvvMMessages;
using SQMeeting.Utilities;
using SQMeeting.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace SQMeeting.FRTCView
{
    public enum FRTCPopupViews
    {
        FRTCSetting,
        FRTCJoinMeeting,
        FRTCMeetingPassword,
        FRTCChangePassword,
        FRTCChangeDisplayName,
        FRTCMeetingHistoryDetail,
        FRTCHangUp,
        FRTCMuteAll,
        FRTCMuteOneParticipant,
        FRTCScheduleMeeting,
        FRTCScheduledMeetingDetail,
        FRTCUpdateRecurringMeetingGroup,
        FRTCUpdateRecurringMeetingSingle,
        FRTCMeetingRoomSetting,
        FRTCContentSource,
        FRTCReconnecting,
        FRTCSendMeetingMsg
    }
    public class FRTCPopupViewManager
    {
        public static Window CurrentPopup
        {
            get;
            private set;
        }

        public static bool? ShowPopupView(FRTCPopupViews View, object[] parameters)
        {
            bool? result = null;
            switch (View)
            {
                case FRTCPopupViews.FRTCSetting:
                    FRTCView.SettingWindow setting = new FRTCView.SettingWindow();
                    if (parameters == null)
                        setting.Owner = Application.Current.MainWindow;
                    else
                        setting.Owner = parameters[0] as Window;
                    CurrentPopup = setting;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    Messenger.Default.Send<OnFRTCViewShownMessage>(new OnFRTCViewShownMessage() { View = FrtcMeetingViews.FRTCSettingWindow, Param = setting });
                    result = setting.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCJoinMeeting:
                    GuestJoinMeetingWindow join = new GuestJoinMeetingWindow();
                    join.Owner = Application.Current.MainWindow;
                    CurrentPopup = join;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    Messenger.Default.Send<OnFRTCViewShownMessage>(new OnFRTCViewShownMessage() { View = FrtcMeetingViews.FRTCJoinMeetingView, Param = join });
                    result = join.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCMeetingPassword:
                    FRTCMeetingPasswordWindow pwdWnd = new FRTCMeetingPasswordWindow();
                    pwdWnd.Owner = Application.Current.MainWindow;
                    CurrentPopup = pwdWnd;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = pwdWnd.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCChangePassword:
                    FRTCView.ChangePwdDialog dlg = new FRTCView.ChangePwdDialog();
                    dlg.Owner = Application.Current.MainWindow;
                    CurrentPopup = dlg;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = dlg.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCChangeDisplayName:
                    FRTCView.ChangeDisplayNameWindow changeDisplay = new FRTCView.ChangeDisplayNameWindow();
                    changeDisplay.Owner = Application.Current.MainWindow;
                    CurrentPopup = changeDisplay;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    changeDisplay.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCMeetingHistoryDetail:
                    FRTCView.MeetingHistoryDetailWindow his = new FRTCView.MeetingHistoryDetailWindow();
                    his.Owner = Application.Current.MainWindow;
                    CurrentPopup = his;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = his.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCHangUp:
                    FRTCView.FRTCHangUpWindow hangup = new FRTCView.FRTCHangUpWindow();
                    hangup.Owner = parameters[0] as Window;
                    CurrentPopup = hangup;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    hangup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    hangup.SizeToContent = SizeToContent.WidthAndHeight;
                    result = hangup.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCMuteAll:
                    FRTCView.MuteAllWindow muteAll = new FRTCView.MuteAllWindow();
                    muteAll.Owner = parameters[0] as Window;
                    CurrentPopup = muteAll;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = muteAll.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCMuteOneParticipant:
                    FRTCView.MuteOneParticipant mute = new FRTCView.MuteOneParticipant();
                    mute.Owner = parameters[0] as Window;
                    CurrentPopup = mute;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = mute.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCScheduleMeeting:
                    ScheduleMeetingWindow schedule = new ScheduleMeetingWindow();
                    schedule.Owner = App.Current.MainWindow;
                    CurrentPopup = schedule;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = schedule.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCScheduledMeetingDetail:
                    ScheduledMeetingDetailWindow meetingDetail = new ScheduledMeetingDetailWindow();
                    meetingDetail.Owner = App.Current.MainWindow;
                    CurrentPopup = meetingDetail;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    meetingDetail.Show();
                    break;
                case FRTCPopupViews.FRTCUpdateRecurringMeetingGroup:
                    EditRecurringMeetingGroupWindow editRecurringGroup = new EditRecurringMeetingGroupWindow();
                    editRecurringGroup.Owner = App.Current.MainWindow;
                    CurrentPopup = editRecurringGroup;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = editRecurringGroup.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCUpdateRecurringMeetingSingle:
                    EditRecurringMeetingSingleWindow editRecurringSingle = new EditRecurringMeetingSingleWindow();
                    editRecurringSingle.Owner = App.Current.MainWindow;
                    CurrentPopup = editRecurringSingle;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    result = editRecurringSingle.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCContentSource:
                    break;
                case FRTCPopupViews.FRTCReconnecting:
                    FRTCView.FRTCReconnectingWindow reconnecting = new FRTCView.FRTCReconnectingWindow();
                    reconnecting.Owner = parameters[0] as Window;
                    double w = SystemParameters.ThickVerticalBorderWidth + SystemParameters.ResizeFrameVerticalBorderWidth;
                    double h = SystemParameters.BorderWidth + SystemParameters.ResizeFrameHorizontalBorderHeight;
                    reconnecting.Width = reconnecting.Owner.RenderSize.Width - (2 * w);
                    reconnecting.Height = reconnecting.Owner.RenderSize.Height - (2 * h);
                    reconnecting.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    CurrentPopup = reconnecting;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    CurrentPopup.Loaded += new RoutedEventHandler((s, e) => { CurrentPopup.Top -= (SystemParameters.FixedFrameHorizontalBorderHeight); });
                    reconnecting.ShowSpinner();
                    result = reconnecting.ShowDialog();
                    break;
                case FRTCPopupViews.FRTCSendMeetingMsg:
                    FRTCView.FRTCSendMeetingMsgWindow sendMeetingMsgWindow = new FRTCSendMeetingMsgWindow();
                    sendMeetingMsgWindow.Owner = parameters[0] as Window;
                    CurrentPopup = sendMeetingMsgWindow;
                    CurrentPopup.Closed += new EventHandler((s, e) => { CurrentPopup = null; });
                    sendMeetingMsgWindow.ShowDialog();
                    result = sendMeetingMsgWindow.DialogResult;
                    break;
                default:
                    break;
            }
            return result;
        }
    }
}
