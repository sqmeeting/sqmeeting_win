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
    /// ChangeDisplayNameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChangeDisplayNameWindow : Window
    {
        public ChangeDisplayNameWindow()
        {
            InitializeComponent();
        }

        private void Rectangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void tbName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            tbName.Text = e.Text.Trim();
            e.Handled = true;
        }

        private void tbName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbName.Text) && e.Key == Key.Space)
            {
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }
    }
}
