using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SQMeeting.Converters
{
    public class CallRateOptionsListConverter : IValueConverter
    {
        static Dictionary<int, int> rate2IndexMap = new Dictionary<int, int>()
        {  
            { 0,    0 },
            { 256,  1 },
            { 384,  2 },
            /*{ 512,  3 },*/
            { 576,  3 },
            { 768,  4 },
            { 1024, 5 },
            { 1536, 6 },
            { 2048, 7 },
            { 2560, 8 },
            { 3072, 9 },
            { 4096,10 },
        };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rate = (int)value;
            if (rate2IndexMap.ContainsKey(rate))
            {
                return rate2IndexMap[rate];
            }
            else
            {
                return -1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int index = (int)value;
            int rate = 512;
            switch (index)
            {
                case 0:
                    rate = 0;
                    break;
                case 1:
                    rate = 256;
                    break;
                case 2:
                    rate = 384;
                    break;
                case 3:
                    rate = 576;
                    break;
                case 4:
                    rate = 768;
                    break;
                case 5:
                    rate = 1024;
                    break;
                case 6:
                    rate = 1536;
                    break;
                case 7:
                    rate = 2048;
                    break;
                case 8:
                    rate = 2560;
                    break;
                case 9:
                    rate = 3072;
                    break;
                case 10:
                    rate = 4096;
                    break;
                default:
                    rate = 1024;
                    break;
            }
            return rate;
        }
    }
}
