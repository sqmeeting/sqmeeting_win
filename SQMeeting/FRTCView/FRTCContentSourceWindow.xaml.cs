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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SQMeeting.ViewModel;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// FRTCContentSourceWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FRTCContentSourceWindow : Window
    {
        public FRTCContentSourceWindow()
        {
            InitializeComponent();
            this.scrollV.ScrollChanged += ScrollV_ScrollChanged;
            this.ContentRendered += FRTCContentSourceWindow_ContentRendered;
        }

        private void FRTCContentSourceWindow_ContentRendered(object sender, EventArgs e)
        {
            FRTCMeetingVideoViewModel vm = this.DataContext as FRTCMeetingVideoViewModel;
            IntPtr thisHWND = new WindowInteropHelper(this).Handle;
            foreach (var dataItem in this.contentSourceList.Items)
            {
                ListViewItem item = contentSourceList.ItemContainerGenerator.ContainerFromItem(dataItem) as ListViewItem;
                ContentSourceItem source = item.DataContext as ContentSourceItem;
                if (source.SourceType == 0)
                {
                    source.StartScreenThumb();
                }
                else
                {
                    var child = Utilities.FRTCUIUtils.FindChild<ContentPresenter>(item, string.Empty);
                    if(child != null)
                    {
                        Rectangle thumbRect = item.ContentTemplate.FindName("thumbArea", child) as Rectangle;
                        Win32API.RECT rect = GetActualThumbRect(thumbRect);
                        source.StartThumb(thisHWND, rect);
                    }
                }
            }
        }

        private void ScrollV_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            foreach (var dataItem in this.contentSourceList.Items)
            {
                ListViewItem item = contentSourceList.ItemContainerGenerator.ContainerFromItem(dataItem) as ListViewItem;
                ContentSourceItem source = item.DataContext as ContentSourceItem;
                if (source.SourceType == 1)
                {
                    Rectangle thumbRect = item.ContentTemplate.FindName("thumbArea", Utilities.FRTCUIUtils.FindChild<ContentPresenter>(item, string.Empty)) as Rectangle;

                    Win32API.RECT rect = GetActualThumbRect(thumbRect);

                    source.UpdateThumbLocation(rect);
                }
            }
        }

        private Win32API.RECT GetActualThumbRect(Rectangle thumbRect)
        {
            double thumbWidth = thumbRect.RenderSize.Width;
            double thumbHeight = thumbRect.RenderSize.Height;

            Win32API.RECT rect = new Win32API.RECT();
            Point ptThumb = thumbRect.TranslatePoint(new Point(0, 0), this);
            Point ptList = this.scrollV.TranslatePoint(new Point(0, 0), this);
            double thumbX = ptThumb.X;
            double thumbY = ptThumb.Y;

            if (ptThumb.Y < ptList.Y && ptList.Y - ptThumb.Y < thumbRect.RenderSize.Height)//上边界在列表上方, 下边界位于列表内
            {
                thumbHeight = thumbRect.RenderSize.Height - (ptList.Y - ptThumb.Y);
                thumbY = ptList.Y;
            }
            else if (ptThumb.Y < ptList.Y + this.scrollV.RenderSize.Height && ptThumb.Y + thumbHeight > ptList.Y + this.scrollV.RenderSize.Height)//上边界在列表内, 下边界位于列表下方
            {
                thumbHeight = thumbRect.RenderSize.Height - ((ptThumb.Y + thumbRect.RenderSize.Height) - (ptList.Y + this.scrollV.RenderSize.Height));
            }
            else if(ptThumb.Y + thumbHeight < ptList.Y || ptThumb.Y > ptList.Y + this.scrollV.RenderSize.Height)//完全在列表外
            {
                thumbHeight = 0;
            }

            double scalerate = 1.0d;
            var source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                scalerate = source.CompositionTarget.TransformToDevice.M11;
            }
            rect.Left = (int)(thumbX * scalerate);
            rect.Top = (int)(thumbY * scalerate);
            rect.Right = (int)((thumbX + thumbWidth) * scalerate);
            rect.Bottom = (int)((thumbY + thumbHeight) * scalerate);

            return rect;
        }

        private void FRTCContentSourceWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 10));
            e.Handled = true;
        }

        private void Grid_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
