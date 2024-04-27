using System;
using System.Globalization;
using System.Drawing;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SQMeeting.Converters
{
    public class BitmapToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            BitmapSource targetValue = null;
            IntPtr ptrBitmap = IntPtr.Zero;
            try
            {
                Bitmap sourceValue = value as Bitmap;
                ptrBitmap = sourceValue.GetHbitmap();
                if (ptrBitmap != IntPtr.Zero)
                {
                    targetValue = Imaging.CreateBitmapSourceFromHBitmap(ptrBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }
            finally
            {
                if (ptrBitmap != IntPtr.Zero)
                    Win32API.DeleteObject(ptrBitmap);
            }

            return targetValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
