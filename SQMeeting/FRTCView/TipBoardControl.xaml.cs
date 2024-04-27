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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// TipBoardControl.xaml 的交互逻辑
    /// </summary>
    public partial class TipBoardControl : UserControl
    {
        //Thread _ownerThread;
        public TipBoardControl()
        {
            InitializeComponent();
            //_ownerThread = ;
            this.IsVisibleChanged += TipBoardControl_IsVisibleChanged;
            this.Loaded += TipBoardControl_Loaded;
        }

        private void TipBoardControl_Loaded(object sender, RoutedEventArgs e)
        {
            UIElement owner = GetMessageRecipient(this);
            if (owner != null)
            {
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<MvvMMessages.FRTCTipsMessage>
                        (owner, new Action<MvvMMessages.FRTCTipsMessage>(ShowMsg));
            }
            else
            {
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<MvvMMessages.FRTCTipsMessage>
                        (this, new Action<MvvMMessages.FRTCTipsMessage>(ShowMsg));
            }
        }

        private void ShowMsg(MvvMMessages.FRTCTipsMessage m)
        {
            Dispatcher d = Dispatcher.FromThread(Thread.CurrentThread);
            if (d == null)
            {
                d = Application.Current.Dispatcher;
            }
            d.Invoke(() =>
            {
                if (this.Parent is UIElement && ((UIElement)this.Parent).IsVisible == false)
                {
                    return;
                }
                this.UpdateLayout();
                if (!string.IsNullOrEmpty(m.TipMessage))
                {
                    this.MessageText.Text = m.TipMessage;
                    this.Visibility = Visibility.Visible;
                }
                else
                    this.Visibility = Visibility.Hidden;
            });
        }

        private void TipBoardControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue == true)
            {
                new DispatcherTimer(TimeSpan.FromSeconds(3),DispatcherPriority.Normal, new EventHandler((s, ev) => 
                {
                    this.Visibility = Visibility.Hidden;
                    this.MessageText.Text = string.Empty;
                    ((DispatcherTimer)s).Stop();
                }), Dispatcher.FromThread(Thread.CurrentThread));
            }
        }

        public static UIElement GetMessageRecipient(DependencyObject obj)
        {
            return (UIElement)obj.GetValue(MessageRecipientProperty);
        }

        public static void SetMessageRecipient(DependencyObject obj, UIElement value)
        {
            obj.SetValue(MessageRecipientProperty, value);
        }

        public static readonly DependencyProperty MessageRecipientProperty =
            DependencyProperty.RegisterAttached("MessageRecipient", typeof(UIElement), typeof(TipBoardControl), new UIPropertyMetadata(null, null));

    }
}
