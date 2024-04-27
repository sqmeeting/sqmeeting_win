using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class EmptyStringToNONEConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string ret = string.IsNullOrEmpty(value as string) ? Properties.Resources.FRTC_MEETING_SDKAPP_NONE : value as string;
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string ret = value as string;
            if (!string.IsNullOrEmpty(ret) && ret == Properties.Resources.FRTC_MEETING_SDKAPP_NONE)
            {
                ret = string.Empty;
            }
            return ret;
        }
    }
}
