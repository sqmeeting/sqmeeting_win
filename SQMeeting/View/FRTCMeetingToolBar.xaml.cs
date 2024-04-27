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
using System.Windows.Controls.Primitives;

namespace SQMeeting.View
{
    public partial class FRTCMeetingToolBar : Window
    {
        public FRTCMeetingToolBar()
        {
            InitializeComponent();
            Loaded += FRTCMeetingToolBar_Loaded;
        }

        private void FRTCMeetingToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            this.toolBar.IsVisibleChanged += FRTCMeetingToolBar_IsVisibleChanged;
            this.layoutBD.CornerRadius = Environment.OSVersion.Version.Build >= 2200/* windows11 */ ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(0, 0, 0, 0); 
        }

        private void FRTCMeetingToolBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue == false)
            {
                this.tbStreamingURL.IsChecked = false;
            }
        }

        public double VerticalOffset = 0;

        private double _dWidth = 230;
        public double dWidth
        {
            get
            {
                return _dWidth;
            }

            set
            {
                if(_dWidth !=value)
                {
                    _dWidth = value;
                    this.Width = value;
                }
            }
        }
    }
}
