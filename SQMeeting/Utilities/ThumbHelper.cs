using System;
using System.Windows;
using System.Drawing;
using SQMeeting.LogTool;

namespace SQMeeting.Utilities
{
    public class ThumbHelper : IDisposable
    {
        IntPtr _targetHandle, _thumbHandle;
        Win32API.RECT _targetRect;

        public void Init(IntPtr Target, IntPtr ThumbSrcWnd, Win32API.RECT Location)
        {
            _targetHandle = Target;
            _targetRect = Location;
            if (_thumbHandle != IntPtr.Zero)
                Win32API.DwmUnregisterThumbnail(_thumbHandle);
            int ret = Win32API.DwmRegisterThumbnail(_targetHandle, ThumbSrcWnd, out _thumbHandle);
            if (0 == ret)
                Update();
        }

        public void Update()
        {
            if (_thumbHandle == IntPtr.Zero)
                return;

            int ret = Win32API.DwmQueryThumbnailSourceSize(_thumbHandle, out Win32API.PSIZE size);
            if(ret == 0)
            {
                var props = new Win32API.DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = Win32API.DWM_TNP_VISIBLE | Win32API.DWM_TNP_RECTDESTINATION | Win32API.DWM_TNP_OPACITY,
                    opacity = 255,
                    rcDestination = _targetRect,
                    fSourceClientAreaOnly = true,
                };

                if (size.x < (_targetRect.Right - _targetRect.Left))
                    props.rcDestination.Right = props.rcDestination.Left + size.x;

                if (size.y < (_targetRect.Bottom - _targetRect.Top))
                    props.rcDestination.Bottom = props.rcDestination.Top + size.y;

                ret = Win32API.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
                if(ret != 0)
                {
                    LogHelper.Error("DwmUpdateThumbnailProperties failed {0}", ret);
                }
            }
        }

        public void Update(Win32API.RECT Location)
        {
            if (_thumbHandle == IntPtr.Zero)
                return;

            _targetRect = Location;
            Update();
        }

        public static Bitmap GetSreenshot(Rect rect)
        {
            Bitmap bm = new Bitmap((int)rect.Width, (int)rect.Height);
            Graphics g = Graphics.FromImage(bm);
            g.CopyFromScreen((int)rect.Left, (int)rect.Top, 0, 0, bm.Size);
            //bm.Save("D:\\test.bmp");
            return bm;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            if (_thumbHandle != IntPtr.Zero)
                Win32API.DwmUnregisterThumbnail(_thumbHandle);
        }
    }
}
