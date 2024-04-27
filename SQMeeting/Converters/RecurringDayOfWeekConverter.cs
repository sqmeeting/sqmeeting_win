using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class RecurringDayOfWeekConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is List<int>) || parameter == null)
            {
                throw new ArgumentException();
            }
            List<int> lst = (List<int>)value;
            int tag = int.Parse(parameter as string);
            return lst.Contains(tag);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
