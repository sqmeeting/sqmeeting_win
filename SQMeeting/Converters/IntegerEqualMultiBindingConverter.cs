using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class IntegerEqualMultiBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int v0 = -1;
            int v1 = -2;
            int offset = 0;
            if(parameter != null)
            {
                int.TryParse(parameter.ToString(), out offset);
            }
            if (values.Count() == 2)
            {
                if (values[0] is int || values[0] is Enum)
                {
                    v0 = (int)values[0];
                }
                else if (values[0] is string)
                {
                    int.TryParse(values[0].ToString(), out v0);
                }

                if (values[1] is int)
                {
                    v1 = (int)values[1];
                }
                else if (values[1] is string)
                {
                    int.TryParse(values[1].ToString(), out v1);
                }
            }
            return (v0 + offset)  == v1;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
