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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SQMeeting.View
{
    /// <summary>
    /// Interaction logic for Spinner.xaml
    /// </summary>
    public partial class Spinner : UserControl
    {
        private Storyboard _storyboard;

        public Spinner()
        {
            InitializeComponent();
            this.IsVisibleChanged -= OnVisibleChanged;
            this.IsVisibleChanged += OnVisibleChanged;
        }

        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }

        private void StartAnimation()
        {
            _storyboard = (Storyboard)FindResource("spinnerAnimation");
            _storyboard.Begin(spinner, true);
        }

        private void StopAnimation()
        {
            _storyboard.Remove(spinner);
        }
    }
}
