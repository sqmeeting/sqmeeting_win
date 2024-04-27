using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace SQMeeting.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool sourceValue = (bool)value;
            bool doHide = false;
            if (parameter != null)
            {
                switch (parameter.ToString())
                {
                    case "Reverse":
                        sourceValue = !sourceValue;
                        break;
                    case "Hidden":
                        doHide = true;
                        break;
                    default:
                        break;
                }
            }
            return sourceValue ? Visibility.Visible : (doHide ? Visibility.Hidden : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility v = (Visibility)value;
            return v == Visibility.Visible;
        }
    }

}
