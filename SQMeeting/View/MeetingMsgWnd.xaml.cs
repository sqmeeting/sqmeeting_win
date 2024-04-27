using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SQMeeting.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using System.Windows.Media.Animation;

namespace SQMeeting.View
{
    public partial class MeetingMsgWnd : Window
    {
        public MeetingMsgWnd()
        {
            InitializeComponent();

            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            border.DataContext = self.MeetingMsgInfo;
        }

        public Storyboard StoryboardMsg;
        public DoubleAnimation DoubleAnimationMsg;

        public void StopShowMSg()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (DoubleAnimationMsg != null)
                {
                    DoubleAnimationMsg.From = this.Owner.ActualWidth;
                    DoubleAnimationMsg.To = text_msg.ActualWidth * -1;
                    StoryboardMsg.Stop();
                }

                HideMsg();
            }));
        }

        public void HideMsg()
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            msgCanvas.Visibility = Visibility.Hidden;
            MeetingMsg2.Visibility = Visibility.Hidden;
            self.ShowSmall = false;
        }

        public void StartShowMSg()
        {
            this.Dispatcher.Invoke(new Action(() => {
               
                FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
                if (self.MeetingMsgInfo.MsgDisplayRepetition > 0)
                {
                    if (DoubleAnimationMsg != null)
                    {
                        StoryboardMsg.Stop();
                        DoubleAnimationMsg = null;
                        StoryboardMsg = null;
                    }

                    if (!self.IsSmallWnd)
                    {
                        msgCanvas.Visibility = Visibility.Visible;
                        MeetingMsg2.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        msgCanvas.Visibility = Visibility.Hidden;
                        MeetingMsg2.Visibility = Visibility.Hidden;
                    }
                    text_msg.Text = self.MeetingMsgInfo.MsgContent;
           
                    text_msg.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    text_msg.Arrange(new Rect(0, 0, text_msg.DesiredSize.Width, text_msg.DesiredSize.Height));

                    double txtWidth = text_msg.ActualWidth;
                    double winWidth = this.ActualWidth;

                    Canvas.SetTop(text_msg, (msgCanvas.ActualHeight - text_msg.ActualHeight) / 2);

                    int second = 0;
                    int val = (int)(txtWidth / winWidth);
                    if (val == 0)
                        val = 1;
                    if (self.MeetingMsgInfo.MsgDisplaySpeed == "fast")
                    {
                        second = (int)val * 15;
                    }
                    else
                    {
                        second = (int)val * 30;
                    }
                    LogTool.LogHelper.Debug("msg duration: {0}", second);
                    LogTool.LogHelper.Debug("msg from: {0}", winWidth);
                    LogTool.LogHelper.Debug("msg to: {0}", txtWidth * -1);

                    StoryboardMsg = new Storyboard();
                    msgCanvas.RenderTransform = (Transform)new TranslateTransform(0,0);

                    DoubleAnimationMsg = new DoubleAnimation();
                    DoubleAnimationMsg.Duration = new Duration(TimeSpan.FromSeconds(second));
                    DoubleAnimationMsg.To = txtWidth * -1;
                    DoubleAnimationMsg.From = winWidth;
                    DoubleAnimationMsg.AutoReverse = false;
                    DoubleAnimationMsg.RepeatBehavior = new RepeatBehavior((double)self.MeetingMsgInfo.MsgDisplayRepetition);

                    Storyboard.SetTarget((Timeline)DoubleAnimationMsg, (DependencyObject)msgCanvas);
                    Storyboard.SetTargetProperty((Timeline)DoubleAnimationMsg, new PropertyPath("RenderTransform.X"));
                    ((ICollection<Timeline>)StoryboardMsg.Children).Add((Timeline)DoubleAnimationMsg);
                    DoubleAnimationMsg.Completed += DoubleAnimationMsg_Completed;
                    StoryboardMsg.Begin();
                }
                else
                {
                    if (DoubleAnimationMsg != null)
                    {
                        DoubleAnimationMsg.From = this.Owner.ActualWidth;
                        DoubleAnimationMsg.To = text_msg.ActualWidth * -1;
                        StoryboardMsg.Stop();
                    }

                    text_msg2.HorizontalAlignment = HorizontalAlignment.Center;
                    text_msg2.TextTrimming = TextTrimming.CharacterEllipsis;

                    if (!self.IsSmallWnd)
                    {
                        MeetingMsg2.Visibility = Visibility.Visible;
                        msgCanvas.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        MeetingMsg2.Visibility = Visibility.Hidden;
                        msgCanvas.Visibility = Visibility.Hidden;
                    }
                }

             
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void DoubleAnimationMsg_Completed(object sender, EventArgs e)
        {
            FRTCMeetingVideoViewModel self = SimpleIoc.Default.GetInstance<FRTCMeetingVideoViewModel>();
            StoryboardMsg.Stop();
            self.ShowMeetingMsgWnd = false;
            msgCanvas.Visibility = Visibility.Hidden;
            MeetingMsg2.Visibility = Visibility.Hidden;
            this.Close();
        }
    }
}
