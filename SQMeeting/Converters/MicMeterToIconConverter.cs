using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SQMeeting.Converters
{
    public class MicMeterToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            BitmapSource bitmapSource = null;
            int level = (int)value;
            if(level > 7) level = 7;
            Bitmap bitmap = null;
            string p = parameter as string;
            if(string.IsNullOrEmpty(p))
            {
                p = "toolbar";
            }
            if (p.ToLower() == "toolbar")
            {
                switch (level)
                {
                    case 0:
                        bitmap = Properties.Resources.frtc_toolbar_mute_mic;
                        break;
                    case 1:
                    case 2:
                        bitmap = Properties.Resources.frtc_toolbar_mic_lv1;
                        break;
                    case 3:
                    case 4:
                        bitmap = Properties.Resources.frtc_toolbar_mic_lv2;
                        break;
                    case 5:
                    case 6:
                        bitmap = Properties.Resources.frtc_toolbar_mic_lv3;
                        break;
                    case 7:
                        bitmap = Properties.Resources.frtc_toolbar_mic_lv4;
                        break;
                    default:
                        return null;
                }
            }
            else if(p.ToLower() == "rosterlist")
            {
                switch (level)
                {
                    case 0:
                        bitmap = Properties.Resources.frtc_audio_open_lv0;
                        break;
                    case 1:
                    case 2:
                        bitmap = Properties.Resources.frtc_audio_open_lv1;
                        break;
                    case 3:
                    case 4:
                        bitmap = Properties.Resources.frtc_audio_open_lv2;
                        break;
                    case 5:
                    case 6:
                        bitmap = Properties.Resources.frtc_audio_open_lv3;
                        break;
                    case 7:
                        bitmap = Properties.Resources.frtc_audio_open_lv4;
                        break;
                    default:
                        return null;
                }
            }
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap();
                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                    Win32API.DeleteObject(hBitmap);
            }

            return bitmapSource;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
