using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool) && targetType != typeof(Nullable<bool>))
            {
                throw new InvalidOperationException();
            }

            return value == null || !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool) && targetType != typeof(Nullable<bool>))
            {
                throw new InvalidOperationException();
            }
            return value == null || !(bool)value;
        }

        #endregion
    }
}
