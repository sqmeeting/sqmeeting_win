using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class AudioMeterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null || !(value is int) || parameter == null)
            {
                throw new ArgumentException();
            }
            int level = (int)value;
            int index = int.Parse(parameter as string);

            if (level > index)
                return "#0FD152";
            else
                return "#EAEAF0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
