﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQMeeting.FRTCView
{
    /// <summary>
    /// GuestHomeView.xaml 的交互逻辑
    /// </summary>
    public partial class GuestHomeView : UserControl
    {
        public GuestHomeView()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.pop_tip.IsOpen = true;
        }

        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            this.pop_tip.IsOpen = true;
        }
    }
}
