using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// FRTCMessageBox.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCMessageBox : Window
    {
        private bool isOK = false;

        private bool? do_OK = false;
        public FRTCMessageBox()
        {
            InitializeComponent();
        }

        static FRTCMessageBox _notificationBox = null;
        public static bool ShowNotificationMessage(string title, string message, string btnOKText = "", Window Owner = null)
        {
            bool ret = false;
            if (_notificationBox != null)
            {
                _notificationBox.Close();
            }
            _notificationBox = new FRTCMessageBox();
            _notificationBox.ShowActivated = true;
            _notificationBox.title.Text = title;
            _notificationBox.msg.Text = message;
            //if (!withCancel)
            {
                _notificationBox.btnCancel.Visibility = Visibility.Collapsed;
                Grid.SetColumn(_notificationBox.btnOK, 0);
                Grid.SetColumnSpan(_notificationBox.btnOK, 2);
            }
            if (!string.IsNullOrEmpty(btnOKText))
                _notificationBox.btnOK.Content = btnOKText;
            if (Owner != null)
            {
                _notificationBox.Owner = Owner;
            }
            else
            {
                try
                {
                    _notificationBox.Owner = FRTCPopupViewManager.CurrentPopup == null ? App.Current.MainWindow : FRTCPopupViewManager.CurrentPopup;
                }
                catch (Exception ex)
                {
                    _notificationBox.Owner = null;
                }
            }
            if (_notificationBox.Owner != null)
            {
                _notificationBox.Owner.Activate();
            }
            _notificationBox.WindowStartupLocation = _notificationBox.Owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            bool? dialogRet = _notificationBox.ShowDialog();
            ret = dialogRet.HasValue ? dialogRet.Value : false;
            _notificationBox = null;
            return ret;
        }

        public static bool ShowConfirmMessage(string title, string message, string btnOKText = "", string btnCancelText = "", bool redOKButton = false, Window Owner = null, bool showInCenterScreen = false)
        {
            bool ret = false;
            FRTCMessageBox w = new FRTCMessageBox();

            w.title.Text = title;
            w.msg.Text = message;
            if (!string.IsNullOrEmpty(btnOKText))
                w.btnOK.Content = btnOKText;
            if (!string.IsNullOrEmpty(btnCancelText))
                w.btnCancel.Content = btnCancelText;
            if (redOKButton)
                w.btnOK.Foreground = new SolidColorBrush(Color.FromRgb(0xE3, 0x27, 0x26));
            if (Owner != null)
            {
                w.Owner = Owner;
            }
            else
            {
                try
                {
                    w.Owner = FRTCPopupViewManager.CurrentPopup == null ? App.Current.MainWindow : FRTCPopupViewManager.CurrentPopup;
                }
                catch (Exception ex)
                {
                    w.Owner = null;
                }
            }
            w.WindowStartupLocation = showInCenterScreen ? WindowStartupLocation.CenterScreen : (w.Owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner);
            //w.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            bool? dlgRet = w.ShowDialog();
            ret = dlgRet.HasValue ? dlgRet.Value : false;

            return ret;
        }

        static FRTCMessageBox saveMeetingUrlServerAddrDialog = null;
        public static bool? ShowLongConfirmMessage(string message, string P_S, string btnOKText = "", string btnCancelText = "", bool redOKButton = false)
        {
            if (saveMeetingUrlServerAddrDialog != null)
            {
                return null;
            }
            saveMeetingUrlServerAddrDialog?.Close();
            saveMeetingUrlServerAddrDialog = new FRTCMessageBox();

            //w.SizeToContent = SizeToContent.Height;
            saveMeetingUrlServerAddrDialog.Height = Double.NaN;


            Thickness newMargin = saveMeetingUrlServerAddrDialog.title.Margin;
            newMargin.Left = 10;
            newMargin.Right = 10;
            saveMeetingUrlServerAddrDialog.title.Margin = newMargin;
            saveMeetingUrlServerAddrDialog.title.HorizontalAlignment = HorizontalAlignment.Left;
            saveMeetingUrlServerAddrDialog.title.TextWrapping = TextWrapping.Wrap;
            saveMeetingUrlServerAddrDialog.title.FontSize = 14;
            saveMeetingUrlServerAddrDialog.title.FontWeight = FontWeights.Bold;
            saveMeetingUrlServerAddrDialog.title.Text = message;

            saveMeetingUrlServerAddrDialog.msg.HorizontalAlignment = HorizontalAlignment.Left;
            saveMeetingUrlServerAddrDialog.msg.FontSize = 12;
            saveMeetingUrlServerAddrDialog.msg.Text = P_S;
            if (!string.IsNullOrEmpty(btnOKText))
                saveMeetingUrlServerAddrDialog.btnOK.Content = btnOKText;
            if (!string.IsNullOrEmpty(btnCancelText))
                saveMeetingUrlServerAddrDialog.btnCancel.Content = btnCancelText;
            if (redOKButton)
                saveMeetingUrlServerAddrDialog.btnOK.Foreground = new SolidColorBrush(Color.FromRgb(0xE3, 0x27, 0x26));
            saveMeetingUrlServerAddrDialog.Owner = FRTCPopupViewManager.CurrentPopup == null ? App.Current.MainWindow : FRTCPopupViewManager.CurrentPopup;
            saveMeetingUrlServerAddrDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            bool? ret = saveMeetingUrlServerAddrDialog.ShowDialog();
            saveMeetingUrlServerAddrDialog = null;
            return ret;
        }


        static FRTCMessageBox askUnmuteDialog = null;
        public static bool ShowAskUnmuteMessage(Window owner)
        {
            bool ret = false;
            try
            {
                if (askUnmuteDialog != null)
                {
                    try
                    {
                        Dispatcher dlgDispatcher = askUnmuteDialog.Dispatcher;
                        dlgDispatcher.Invoke(new Action(() =>
                        {
                            askUnmuteDialog.isOK = false;
                            askUnmuteDialog?.Close();
                            askUnmuteDialog = null;
                        }));
                    }
                    catch (Exception ex)
                    {
                        LogTool.LogHelper.Exception(ex);
                    }
                }
                askUnmuteDialog = new FRTCMessageBox();
                askUnmuteDialog.title.Text = Properties.Resources.FRTC_MEETING_SDKAPP_UNMUTEAUDIO;
                askUnmuteDialog.msg.Text = Properties.Resources.FRTC_MEETING_SDKAPP_ASK_UNMUTE_DESC;
                askUnmuteDialog.btnOK.Content = Properties.Resources.FRTC_MEETING_SDKAPP_ASK_UNMUTE_UNMUTE;
                askUnmuteDialog.btnCancel.Content = Properties.Resources.FRTC_MEETING_SDKAPP_ASK_UNMUTE_STAYMUTE;
                askUnmuteDialog.Owner = owner;
                askUnmuteDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                bool? dlgRet = askUnmuteDialog.ShowDialog();
                ret = dlgRet.HasValue ? dlgRet.Value : false;
                ;
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }

            return ret;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            do_OK = false;
            this.DialogResult = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            isOK = true;
            do_OK = true;
            this.Close();
        }
    }
}
