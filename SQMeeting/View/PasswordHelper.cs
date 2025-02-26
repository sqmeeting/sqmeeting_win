﻿using System.Windows;
using System.Windows.Controls;
namespace SQMeeting.Helper
{
    public class PasswordHelper : DependencyObject
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password",
            typeof(string), typeof(PasswordHelper),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach",
            typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, Attach));

        public static readonly DependencyProperty PlainTextButtonProperty =
            DependencyProperty.RegisterAttached("PlainTextButton",
            typeof(bool), typeof(PasswordHelper), new UIPropertyMetadata(false));

        public static readonly DependencyProperty HasPasswordProperty =
            DependencyProperty.RegisterAttached("HasPassword",
            typeof(bool), typeof(PasswordHelper), new UIPropertyMetadata(false));

        private static readonly DependencyProperty IsUpdatingProperty =
           DependencyProperty.RegisterAttached("IsUpdating", typeof(bool),
           typeof(PasswordHelper));


        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        public static bool GetPlainTextButton(DependencyObject dp)
        {
            return (bool)dp.GetValue(PlainTextButtonProperty);
        }

        public static void SetPlainTextButton(DependencyObject dp, bool value)
        {
            dp.SetValue(PlainTextButtonProperty, value);
        }

        public static bool GetHasPassword(DependencyObject dp)
        {
            return (bool)dp.GetValue(HasPasswordProperty);
        }
        private static void SetHasPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(HasPasswordProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;

            if (!(bool)GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue;
            }
            passwordBox.PasswordChanged += PasswordChanged;
            SetHasPassword(passwordBox, !string.IsNullOrEmpty(passwordBox.Password));
        }

        private static void Attach(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;

            if (passwordBox == null)
                return;

            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }

            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetHasPassword(passwordBox, !string.IsNullOrEmpty(passwordBox.Password));
            SetIsUpdating(passwordBox, false);
        }
    }
}