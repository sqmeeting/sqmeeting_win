using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class IntToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int sourceValue = (int)value;
            if (parameter != null)
            {
                switch (parameter.ToString())
                {
                    case "Reverse":
                        sourceValue = sourceValue >= 0 ? -1 : 1;
                        break;
                    case "Hide0":
                        sourceValue = sourceValue == 0 ? -1 : sourceValue;
                        break;
                    case "ReverseHide0":
                        sourceValue = sourceValue > 0 ? -1 : 1;
                        break;
                    default:
                        break;
                }
            }
            Visibility ret = Visibility.Collapsed;
            if (sourceValue >= 0)
            {
                ret = Visibility.Visible;
            }
            else
            {
                ret = Visibility.Collapsed;
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility v = (Visibility)value;
            int ret = -1;
            if (v == Visibility.Visible)
                ret = 1;
            return ret;
        }
    }
}
