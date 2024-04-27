using SQMeeting.Model;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace SQMeeting.Commands
{
    public class FRTCChangePWDCommand : ICommand
    {
        private bool _canExecute = true;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            object[] changePWDParams = parameter as object[];
            if (changePWDParams != null && changePWDParams.Length >= 3)
            {
                PasswordBox oldPwd = (PasswordBox)changePWDParams[0];
                PasswordBox newPwd = (PasswordBox)changePWDParams[1];
                PasswordBox confirm = (PasswordBox)changePWDParams[2];
                if (string.IsNullOrEmpty(oldPwd.Password) || string.IsNullOrWhiteSpace(newPwd.Password) || string.IsNullOrWhiteSpace(confirm.Password))
                {
                    Messenger.Default.Send<MvvMMessages.FRTCTipsMessage, FRTCView.ChangePwdDialog>(new MvvMMessages.FRTCTipsMessage() 
                    { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_PWD_EMPTY });
                    return;
                }
                else
                {
                    string strRegex = @"^[a-zA-Z0-9!@#$%*()[\]_+-=^&}{:;?.]{6,48}$";
                    int lenMin = 6;
                    if (changePWDParams.Length > 3
                        && !string.IsNullOrWhiteSpace(changePWDParams[3] as string)
                        && (changePWDParams[3] as string).ToLower() == "high")
                    {
                        strRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$!@#$%*()[\]_+^&}{:;?.])[A-Za-z\d$!@#$%*()[\]_+-=^&}{:;?.]{8,48}";
                        lenMin = 8;
                    }

                    //if (newPwd.Password.Length < lenMin || confirm.Password.Length < lenMin)
                    //{
                    //    Messenger.Default.Send<MvvMMessages.ShowTipsStringMessage, FRTCView.ChangePwdDialog>(new MvvMMessages.ShowTipsStringMessage()
                    //    {
                    //        TipMessage = lenMin == 6 ? Properties.Resources.FRTC_MEETING_SDKAPP_PWD_SHORT : Properties.Resources.FRTC_MEETING_SDKAPP_PWD_SHORT_THAN8
                    //    });
                    //    return;
                    //}
                    //else
                    {
                        Regex regex = new Regex(strRegex);
                        Match match = regex.Match(newPwd.Password);
                        if (!match.Success)
                        {
                            Messenger.Default.Send<MvvMMessages.FRTCTipsMessage, FRTCView.ChangePwdDialog>(new MvvMMessages.FRTCTipsMessage()
                            {
                                TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_RESETPWD_FAILED_COMPLEXITY
                            });
                            return;
                        }
                    }
                }

                if (newPwd.Password == confirm.Password)
                {
                    CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCUserManager>()
                        .FRTCResetUserPassword(((PasswordBox)changePWDParams[0]).Password
                                                 , ((PasswordBox)changePWDParams[1]).Password);
                }
                else
                {
                    Messenger.Default.Send<MvvMMessages.FRTCTipsMessage, FRTCView.ChangePwdDialog>(new MvvMMessages.FRTCTipsMessage() { TipMessage = Properties.Resources.FRTC_MEETING_SDKAPP_NEW_PWD_NOT_SAME });
                }
            }
        }
    }
}
