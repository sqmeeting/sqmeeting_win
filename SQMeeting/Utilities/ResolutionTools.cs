using System;
using System.Windows.Forms;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Interop;
using static SQMeeting.Win32API;
using System.Runtime.CompilerServices;

namespace SQMeeting.Utilities
{
    public static class ResolutionTools
    {
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

        public static bool IsOSVersionHigherThan8()
        {
            Version currentVersion = Environment.OSVersion.Version;

            Version compareToVersion = new Version("6.3");
            if (currentVersion.CompareTo(compareToVersion) >= 0)
            {   //win8.1 and higher OS
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void GetDpi(this Screen screen, MonitorDpiType dpiType, out uint dpiX, out uint dpiY)
        {
            var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);

            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            //GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
            GetDpiForMonitorInternal(mon, dpiType, out dpiX, out dpiY);
        }

        public static int GetDpiForMonitorInternal(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY)
        {
            IntPtr hUser32 = Win32API.GetModuleHandle("user32.dll");
            dpiX = dpiY = 0;
            int ret = -1;
            if (hUser32 != IntPtr.Zero)
            {
                IntPtr proc = Win32API.GetProcAddress(hUser32, "GetDpiForMonitorInternal");
                if (proc != IntPtr.Zero)
                {
                    Win32API.GetDpiForMonitorInternal funcGetDpiForMonitorInternal = Marshal.GetDelegateForFunctionPointer<Win32API.GetDpiForMonitorInternal>(proc);
                    if (funcGetDpiForMonitorInternal != null)
                    {
                        ret = funcGetDpiForMonitorInternal(hmonitor, dpiType, out dpiX, out dpiY);
                    }
                    else
                    {
                        LogTool.LogHelper.Error("Get GetDpiForMonitorInternal delegate failed");
                    }
                }
                else
                {
                    LogTool.LogHelper.Error("Get GetDpiForMonitorInternal proc address failed");
                }
            }
            else
            {
                LogTool.LogHelper.Error("Get user32.dll module failed");
            }
            return ret;
        }

        public static void GetMeetingWindowSize(HwndSource hwndSource, Screen currentScreen, out int width, out int height)
        {
            double scale = 1;

            Screen screen;
            if (currentScreen == null)
                screen = Screen.FromHandle(FRTCUIUtils.MeetingWindowHandle);
            else
                screen = currentScreen;

            MonitorDpiType type = MonitorDpiType.MDT_EFFECTIVE_DPI;
            uint dpiX = 0;
            uint dpiY = 0;
            GetDpi(screen, type, out dpiX, out dpiY);

            if (dpiX > 96)
                scale = (double)dpiX / 96;

            RECT rect = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out rect);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetWindowRect(FRTCUIUtils.MeetingWindowHandle, out rect1);

            int clientCx = 1280;
            int clientCy = 864;

            if (screen.Bounds.Height >= 2160)
            {
                if (scale >= 3.0)
                {
                    clientCx = 1920;
                    clientCy = 1296;
                }
                else if (scale >= 2.0)
                {
                    clientCx = 2240;
                    clientCy = 1512;
                }
                else
                {
                    clientCx = 2560;
                    clientCy = 1728;
                }
            }
            else if (screen.Bounds.Width >= 1400 && screen.Bounds.Height > 900)
            {
                if (scale >= 1.5)
                {
                    clientCx = 1120;
                    clientCy = 756;
                }
                else
                {
                    clientCx = 1280;
                    clientCy = 864;
                }
            }
            else if (screen.Bounds.Width >= 1024 && screen.Bounds.Height > 720)
            {
                if (scale > 1.25)
                {
                    clientCx = 880;
                    clientCy = 594;
                }
                else
                {
                    clientCx = 960;
                    clientCy = 648;
                }
            }
            else
            {
                clientCx = 720;
                clientCy = 486;
            }

            int cx = 0, cy = 0;
            if (_bSharingContent)
            {
                clientCx = 320;
                clientCy = 180;

                cx = (rect1.Right - rect1.Left) - (rect.Right - rect.Left) + clientCx;
                cy = (rect1.Bottom - rect1.Top) - (rect.Bottom - rect.Top) + clientCy;
            }
            else
            {
                cx = (rect1.Right - rect1.Left) - (rect.Right - rect.Left) + clientCx;
                cy = (rect1.Bottom - rect1.Top) - (rect.Bottom - rect.Top) + clientCy;
            }

            width = cx;
            height = cy;
        }

        public static void UpdateMeetingWindowResolution(Screen curScreen)
        {
            System.Windows.Forms.Screen screen;
            if (curScreen == null)
                screen = System.Windows.Forms.Screen.FromHandle(FRTCUIUtils.MeetingWindowHandle);
            else
                screen = curScreen;

            double scale = 1;


            if (IsOSVersionHigherThan8())
            {
                MonitorDpiType type = MonitorDpiType.MDT_EFFECTIVE_DPI;
                uint dpiX = 0;
                uint dpiY = 0;
                GetDpi(screen, type, out dpiX, out dpiY);

                if (dpiX > 96)
                    scale = (double)dpiX / 96;
            }
            else
            {
                scale = (double)(FRTCUIUtils.CurrentScreenDPI) / 96;
            }


            Console.Out.WriteLine("Current screen ====================================== {0} , {1}, {2}", screen.Bounds.Width, screen.Bounds.Height, screen.DeviceName);

            RECT rect = new RECT();
            FRTCUIUtils.GetClientRect(FRTCUIUtils.MeetingWindowHandle, out rect);

            RECT rect1 = new RECT();
            FRTCUIUtils.GetWindowRect(FRTCUIUtils.MeetingWindowHandle, out rect1);

            int clientCx = 1280;// 1282;
            int clientCy = 864;// 866;

            if (screen.Bounds.Height >= 2160)
            {
                if (scale >= 3.0)
                {
                    clientCx = 1920;
                    clientCy = 1296;
                }
                else if (scale >= 2.0)
                {
                    clientCx = 2240;
                    clientCy = 1512;
                }
                else
                {
                    clientCx = 2560;
                    clientCy = 1728;
                }
            }
            else if (screen.Bounds.Width >= 2560 || screen.Bounds.Height >= 1440)
            {
                if (scale >= 1.5)
                {
                    clientCx = 1600;
                    clientCy = 1080;
                }
                else
                {
                    clientCx = 1760;// 1840;//1952;
                    clientCy = 1188;// 1242;// 1320;
                }
            }
            else if (screen.Bounds.Width >= 1400 && screen.Bounds.Height > 900)
            {
                if (scale > 1.5)
                {
                    clientCx = 1120;// 1122;
                    clientCy = 756;// 758;
                }
                else
                {
                    clientCx = 1280;// 1282;
                    clientCy = 864;// 866;
                }
            }
            else if (screen.Bounds.Width >= 1024 && screen.Bounds.Height > 720)
            {
                if (scale > 1.25)
                {
                    clientCx = 880;// 882;
                    clientCy = 594;// 596;
                }
                else
                {
                    clientCx = 960;// 962;
                    clientCy = 648;// 650;
                }
            }
            else
            {
                clientCx = 720;// 722;// 802;
                clientCy = 486;// 488;// 542;
                if (FRTCUIUtils.MeetingWindow != null)
                {
                    FRTCUIUtils.MeetingWindow.MinWidth = 720;
                    FRTCUIUtils.MeetingWindow.MinHeight = 486;
                }
            }

            int x, y, cx, cy;
            if (_bSharingContent)
            {
                clientCx = 320;
                clientCy = 180;

                cx = (rect1.Right - rect1.Left) - (rect.Right - rect.Left) + clientCx;
                cy = (rect1.Bottom - rect1.Top) - (rect.Bottom - rect.Top) + clientCy;

                x = (int)(screen.WorkingArea.X + (screen.WorkingArea.Width - cx));
                y = (int)(screen.WorkingArea.Y + (screen.WorkingArea.Height - cy));
                Console.Out.WriteLine("sharing : x = {0}, y = {1}, cx = {2}, cy = {3}", x, y, cx, cy);
            }
            else
            {
                cx = (rect1.Right - rect1.Left) - (rect.Right - rect.Left) + clientCx;
                cy = (rect1.Bottom - rect1.Top) - (rect.Bottom - rect.Top) + clientCy;
                x = (int)(screen.WorkingArea.X + (screen.WorkingArea.Width - cx) / 2);
                y = (int)(screen.WorkingArea.Y + (screen.WorkingArea.Height - cy) / 2);

                Console.Out.WriteLine("stop: x = {0}, y = {1}, cx = {2}, cy = {3}", x, y, cx, cy);
            }
            FRTCUIUtils.SetWindowPos(FRTCUIUtils.MeetingWindowHandle, FRTCUIUtils.HWND_TOP, x, y, cx, cy, 64);
        }

        private static bool _bSharingContent = false;

        public static void SetContentSharing(bool isSharing)
        {
            _bSharingContent = isSharing;
        }
    }
}
