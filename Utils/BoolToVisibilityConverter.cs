using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace incadea.WsCrm.DeploymentTool.Utils
{
    /// <summary>
    /// Used to convert bool to visibility in Binding
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts bool to visibility
        /// </summary>
        /// <param name="value">bool value</param>
        /// <param name="targetType">bool</param>
        /// <param name="parameter">if set than vaue reverted</param>
        /// <param name="culture">used culture</param>
        /// <returns>Collapsed for not visible</returns>
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var val = (bool)value;
            if (parameter != null)
            {
                val = !val;
            }
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Used In TwoWay or ToSource mode binding. Converts visibility to bool
        /// </summary>
        /// <param name="value">visibility</param>
        /// <param name="targetType">visibility</param>
        /// <param name="parameter">if set than vaue reverted</param>
        /// <param name="culture">used culture</param>
        /// <returns>true if visible</returns>
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return parameter == null ? (Visibility)value == Visibility.Visible : (Visibility)value != Visibility.Visible;
        }
    }
}
