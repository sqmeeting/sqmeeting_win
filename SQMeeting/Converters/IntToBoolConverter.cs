using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            int threshold = parameter == null ? 0 : int.Parse(parameter as string);

            return (int)value > threshold;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            int threshold = parameter == null ? 0 : int.Parse(parameter as string);
            return (bool)value ? threshold + 1 : 0;
        }
    }
}
