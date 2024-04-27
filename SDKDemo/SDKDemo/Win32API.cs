using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SDKDemo
{
    public struct tagCOPYDATASTRUCT
    {
        public IntPtr dwData;
        public uint cbData;
        public IntPtr lpData;
    }
    public enum MonitorDpiType
    {
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
    }



    public static class Win32API
    {
        [DllImport("Kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hwnd);

        public const Int32 MONITOR_DEFAULTTOPRIMARY = 0x00000001;
        public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, Int32 flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public delegate int GetDpiForMonitorInternal(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);


        public const int WM_COPYDATA = 0x004A;
        public const int WM_ACTIVE = 0x0006;
        public const int WM_NCLBUTTONUP = 0x00A2;
        public const int WM_NCMOUSEMOVE = 0x00A0;
        public const int WM_DISPLAYCHANGE = 0x007E;
        public const int WM_EXITSIZEMOVE = 0x0232;

        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSIZE
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }


        private const int LF_FACESIZE = 32;
        [StructLayout(LayoutKind.Sequential)]
        public struct LOGFONT
        {
            int lfHeight;
            int lfWidth;
            int lfEscapement;
            int lfOrientation;
            int lfWeight;
            byte lfItalic;
            byte lfUnderline;
            byte lfStrikeOut;
            byte lfCharSet;
            byte lfOutPrecision;
            byte lfClipPrecision;
            byte lfQuality;
            byte lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            string lfFaceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NONCLIENTMETRICS
        {
            public uint cbSize;
            public int iBorderWidth;
            public int iScrollWidth;
            public int iScrollHeight;
            public int iCaptionWidth;
            public int iCaptionHeight;
            public LOGFONT lfCaptionFont;
            public int iSmCaptionWidth;
            public int iSmCaptionHeight;
            public LOGFONT lfSmCaptionFont;
            public int iMenuWidth;
            public int iMenuHeight;
            public LOGFONT lfMenuFont;
            public LOGFONT lfStatusFont;
            public LOGFONT lfMessageFont;
            public int iPaddedBorderWidth;
        }


        public const int LOGPIXELSX = 88;
        /// <summary>
        /// Logical pixels inch in Y
        /// </summary>
        public const int LOGPIXELSY = 90;
        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);


        [DllImport("user32.dll")]
        public static extern uint RegisterWindowMessage(string Message);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int DefWindowProc(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint eFlag);

   
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

        [DllImport("User32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("User32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hwnd);


        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);

        [DllImport("User32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);


        public const uint SPI_GETNONCLIENTMETRICS = 0x0029;

        [DllImport("User32.dll")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);


        [DllImport("User32.dll")]
        internal static extern bool DestroyWindow(IntPtr hwnd);

        public const int GWL_STYLE = -16;

        public const ulong WS_VISIBLE = 0x10000000L,
            WS_BORDER = 0x00800000L,
            TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        [DllImport("User32.dll")]
        public static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        [DllImport("User32.dll")]
        public static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);

        [DllImport("User32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public const int DWM_TNP_VISIBLE = 0x8,
            DWM_TNP_OPACITY = 0x4,
            DWM_TNP_RECTDESTINATION = 0x1;

        [DllImport("dwmapi")]
        public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi")]
        public static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi")]
        public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        [DllImport("dwmapi")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);



        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);


    }
}
