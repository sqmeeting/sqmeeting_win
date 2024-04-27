using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace SDKDemo
{
    public delegate IntPtr GetWindowCore(IntPtr hostHwnd);
    public delegate void DestoryWindowCore();
    public class MeetingWindowHost : HwndHost
    {
        IntPtr hwnd = IntPtr.Zero;

        GetWindowCore _getWindowCore = null;
        public event DestoryWindowCore _onDestoryWindowCore = null;

        public MeetingWindowHost(GetWindowCore getWindowCore)
        {
            _getWindowCore = getWindowCore;
        }

        public IntPtr GetHostHandle()
        {
            return hwnd;
        }


        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {

            hwnd = _getWindowCore(hwndParent.Handle);

            return new HandleRef(this, hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Win32API.DestroyWindow(hwnd.Handle);

            if (this._onDestoryWindowCore != null)
            {
                _onDestoryWindowCore.Invoke();
            }
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
    }
}

