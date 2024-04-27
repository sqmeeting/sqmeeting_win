using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SQMeeting.FRTCView
{
    public class PasswordBoxMonitor : DependencyObject
    {
        public static bool GetIsMonitoring(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMonitoringProperty);
        }

        public static void SetIsMonitoring(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMonitoringProperty, value);
        }

        public static readonly DependencyProperty IsMonitoringProperty =
            DependencyProperty.RegisterAttached("IsMonitoring", typeof(bool), typeof(PasswordBoxMonitor), new UIPropertyMetadata(false, OnIsMonitoringChanged));



        public static int GetPasswordLength(DependencyObject obj)
        {
            return (int)obj.GetValue(PasswordLengthProperty);
        }

        private static void SetPasswordLength(DependencyObject obj, int value)
        {
            obj.SetValue(PasswordLengthProperty, value);
        }

        public static readonly DependencyProperty PasswordLengthProperty =
            DependencyProperty.RegisterAttached("PasswordLength", typeof(int), typeof(PasswordBoxMonitor), new UIPropertyMetadata(0));

        private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = d as PasswordBox;
            if (pb == null)
            {
                return;
            }
            if ((bool)e.NewValue)
            {
                pb.PasswordChanged += PasswordChanged;
            }
            else
            {
                pb.PasswordChanged -= PasswordChanged;
            }
        }

        static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (pb == null)
            {
                return;
            }
            SetPasswordLength(pb, pb.Password.Length);
        }
    }

    public class InputBoxIcon : DependencyObject
    {
        public static bool GetShowIcon(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowIconProperty);
        }

        public static void SetShowIcon(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowIconProperty, value);
        }

        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.RegisterAttached("ShowIcon", typeof(bool), typeof(InputBoxIcon), new UIPropertyMetadata(false));


        public static ImageSource GetInputIcon(DependencyObject obj)
        {
            return (ImageSource)obj.GetValue(InputIconProperty);
        }

        public static void SetInputIcon(DependencyObject obj, ImageSource value)
        {
            obj.SetValue(InputIconProperty, value);
        }

        public static readonly DependencyProperty InputIconProperty =
            DependencyProperty.RegisterAttached("InputIcon", typeof(ImageSource), typeof(InputBoxIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, null, null), null);
    }

    public class InputBoxClearButton : DependencyObject
    {
        public static bool GetDoClearText(DependencyObject obj)
        {
            return (bool)obj.GetValue(DoClearTextProperty);
        }

        public static void SetDoClearText(DependencyObject obj, bool value)
        {
            obj.SetValue(DoClearTextProperty, value);
        }

        public static readonly DependencyProperty DoClearTextProperty =
            DependencyProperty.RegisterAttached("DoClearText", typeof(bool), typeof(InputBoxClearButton), new PropertyMetadata(false, OnDoClearTextChanged));

        static void OnDoClearTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Button btn = d as Button;
            if (btn != null && e.OldValue != e.NewValue)
            {
                btn.Click -= ClearButton_Click;
                if ((bool)e.NewValue)
                {
                    btn.Click += ClearButton_Click;
                }
            }
        }
        private static void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                var parent = VisualTreeHelper.GetParent(btn);
                while (parent != null && !(parent is TextBox) && !(parent is PasswordBox))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parent != null)
                {
                    if (parent is TextBox)
                        ((TextBox)parent).Clear();
                    else if (parent is PasswordBox)
                        ((PasswordBox)parent).Clear();
                }
            }
        }
    }

    public class TextBoxEndingCaretIndexBehavior : DependencyObject
    {
        public static bool GetIsMonitoring(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMonitoringProperty);
        }

        public static void SetIsMonitoring(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMonitoringProperty, value);
        }

        public static readonly DependencyProperty IsMonitoringProperty =
            DependencyProperty.RegisterAttached("IsMonitoring", typeof(bool), typeof(TextBoxEndingCaretIndexBehavior), new UIPropertyMetadata(false, OnIsMonitoringChanged));

        private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = d as TextBox;
            if (tb == null)
            {
                return;
            }
            if ((bool)e.NewValue)
            {
                //tb.GotFocus += Tb_GotFocus;
                tb.GotKeyboardFocus += Tb_GotKeyboardFocus;
            }
            else
            {
                //tb.GotFocus -= Tb_GotFocus;
                tb.GotKeyboardFocus -= Tb_GotKeyboardFocus;
            }
        }

        private static void Tb_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).CaretIndex = int.MaxValue;
        }

        private static void Tb_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).CaretIndex = int.MaxValue;
        }
    }

    public static class ScrollHelper
    {
        public static readonly DependencyProperty ScrollToBottomProperty =
            DependencyProperty.RegisterAttached("ScrollToBottom", typeof(ICommand), typeof(ScrollHelper), new FrameworkPropertyMetadata(null, OnScrollToBottomPropertyChanged));

        public static readonly DependencyProperty ScrollToBottomCommandParameterProperty =
            DependencyProperty.RegisterAttached("ScrollToBottomCommandParameter", typeof(object), typeof(ScrollHelper), new FrameworkPropertyMetadata(null));


        public static ICommand GetScrollToBottom(DependencyObject ob)
        {
            return (ICommand)ob.GetValue(ScrollToBottomProperty);
        }

        public static void SetScrollToBottom(DependencyObject ob, ICommand value)
        {
            ob.SetValue(ScrollToBottomProperty, value);
        }

        public static object GetScrollToBottomCommandParameter(DependencyObject ob)
        {
            return ob.GetValue(ScrollToBottomCommandParameterProperty);
        }

        public static void SetScrollToBottomCommandParameter(DependencyObject ob, object value)
        {
            ob.SetValue(ScrollToBottomCommandParameterProperty, value);
        }

        private static void OnScrollToBottomPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = obj as ScrollViewer;

            scrollViewer.Loaded += OnScrollViewerLoaded;

        }

        private static void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            (sender as ScrollViewer).Loaded -= OnScrollViewerLoaded;

            (sender as ScrollViewer).Unloaded += OnScrollViewerUnloaded;
            (sender as ScrollViewer).ScrollChanged += OnScrollViewerScrollChanged;
        }

        private static void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.ScrollableHeight > 0 && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight)
            {
                var command = GetScrollToBottom(sender as ScrollViewer);
                if (command == null || !command.CanExecute(sender as ScrollViewer))
                    return;

                command.Execute(GetScrollToBottomCommandParameter(sender as ScrollViewer));
            }
        }

        private static void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
        {
            (sender as ScrollViewer).Unloaded -= OnScrollViewerUnloaded;
            (sender as ScrollViewer).ScrollChanged -= OnScrollViewerScrollChanged;
        }

    }

    public static class ComboBoxHelper
    {
        public static readonly DependencyProperty NullItemTextProperty =
            DependencyProperty.RegisterAttached("NullItemText", typeof(string), typeof(ComboBoxHelper), new UIPropertyMetadata(null, OnNullItemTextPropertyChanged));

        public static string GetNullItemText(DependencyObject ob)
        {
            return ob.GetValue(NullItemTextProperty) as string;
        }

        public static void SetNullItemText(DependencyObject ob, object value)
        {
            ob.SetValue(NullItemTextProperty, value);
        }

        private static void OnNullItemTextPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewValue as string))
            {
                (obj as ComboBox).SelectionChanged -= ComboBoxHelper_SelectionChanged;
            }
            else
            {
                (obj as ComboBox).SelectionChanged -= ComboBoxHelper_SelectionChanged;
                (obj as ComboBox).SelectionChanged += ComboBoxHelper_SelectionChanged;
            }
        }

        private static void ComboBoxHelper_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            TextBlock nullText = comboBox.Template.FindName("NullText", comboBox) as TextBlock;
            if (nullText != null)
            {
                if (comboBox.SelectedIndex == -1)
                {
                    nullText.Visibility = Visibility.Visible;
                }
                else
                    nullText.Visibility = Visibility.Collapsed;
            }
        }
    }
}
