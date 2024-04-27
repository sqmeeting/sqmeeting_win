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

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// PeopleVideoCollapsedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PeopleVideoCollapsedWindow : Window
    {
        public PeopleVideoCollapsedWindow()
        {
            InitializeComponent();
        }
        bool _isDragging = false;
        private Point _clickPoint;

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(sender as Button);
            if (e.LeftButton == MouseButtonState.Pressed &&
                (sender as Button).IsMouseCaptured &&
                (Math.Abs(currentPoint.X - _clickPoint.X) >
                    SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(currentPoint.Y - _clickPoint.Y) >
                    SystemParameters.MinimumVerticalDragDistance))
            {
                // Prevent Click from firing
                (sender as Button).ReleaseMouseCapture();
                DragMove();
            }
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _clickPoint = e.GetPosition(sender as Button);
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
