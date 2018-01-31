using System.Windows;
using System.Windows.Controls;

namespace incadea.WsCrm.DeploymentTool.Utils
{
    /// <summary>
    /// helper to bind password to property
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Password Dependency Property
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password",
                typeof (string), typeof (PasswordHelper),
                new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached("IsUpdating", typeof (bool),
                typeof (PasswordHelper));


        /// <summary>
        /// gets password
        /// </summary>
        /// <param name="dp">Dependency Property</param>
        /// <returns>password</returns>
        public static string GetPassword(DependencyObject dp)
        {
            return (string) dp.GetValue(PasswordProperty);
        }

        /// <summary>
        /// sets password
        /// </summary>
        /// <param name="dp">Dependency Property</param>
        /// <param name="value">new password</param>
        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        #region private stuff

        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool) dp.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;

            if (!GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string) e.NewValue;
            }
            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void Attach(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;

            if (passwordBox == null)
                return;

            if ((bool) e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }

            if ((bool) e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }

        #endregion
    }
}

