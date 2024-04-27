using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SDKDemo.Utilities
{
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;

        public int dmDisplayFrequency;

        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;

        public int dmPanningWidth;
        public int dmPanningHeight;

    }
    public class FRTCUIUtils
    {
        #region P/Invoke imports & definitions
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 SWP_NOZORDER = 0x0004;
        public const UInt32 SWP_NOREDRAW = 0x0008;
        public const UInt32 SWP_NOACTIVATE = 0x0010;
        public const UInt32 SWP_FRAMECHANGED = 0x0020; /* The frame changed: send WM_NCCALCSIZE */
        public const UInt32 SWP_SHOWWINDOW = 0x0040;
        public const UInt32 SWP_HIDEWINDOW = 0x0080;
        public const UInt32 SWP_NOCOPYBITS = 0x0100;
        public const UInt32 SWP_NOOWNERZORDER = 0x0200; /* Don’t do owner Z ordering */
        public const UInt32 SWP_NOSENDCHANGING = 0x0400; /* Don’t send WM_WINDOWPOSCHANGING */

        public const UInt32 TOPMOST_FLAGS = SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOREDRAW | SWP_NOSENDCHANGING;

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        #endregion

        [DllImport("user32.dll", EntryPoint = "EnumDisplaySettings")]
        public static extern int EnumDisplaySettings(string lpszDeviceName, int iModeNum, IntPtr lpDevMode);

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int abc);

        [DllImport("user32")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern void SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
                                               int X, int Y, int width, int height, uint flags);

        [DllImport("user32")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32")]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        public static IntPtr MeetingWindowHandle = IntPtr.Zero;
        public static IntPtr FRTCWindowHandle = IntPtr.Zero;
        public static IntPtr RosterWindowHandle = IntPtr.Zero;
        public static IntPtr StasticsWindowHandle = IntPtr.Zero;

        static Window _meetingWindow = null;
        public static Window MeetingWindow
        {
            get => _meetingWindow;
        }

        public static void SetMeetingWindow(Window w)
        {
            _meetingWindow = w;
        }

        public static double _currentScreenDPI = -1;
        public static double CurrentScreenDPI
        {
            get
            {
                //if (_CurrentDPI == -1)
                {
                    FRTCUIUtils._currentScreenDPI = Math.Round(FRTCUIUtils.CurrentScreenResolution.Width / SystemParameters.PrimaryScreenWidth, 2) * 96;
                    Console.Out.WriteLine("ViewUtils.ScreenResolution.Width = {0}, SystemParameters.PrimaryScreenWidth, {1}, _CurrentDPI = {2}", FRTCUIUtils.CurrentScreenResolution.Width, SystemParameters.PrimaryScreenWidth, FRTCUIUtils._currentScreenDPI);
                }
                return _currentScreenDPI;
            }
        }

        private const int ENUM_CURRENT_SETTING = -1;

        public static Size _currentSreenResolution = Size.Empty;
        public static Size CurrentScreenResolution
        {
            get
            {
                IntPtr lpDevMode = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DevMode)));
                int ret = EnumDisplaySettings(null, ENUM_CURRENT_SETTING, lpDevMode);
                if (ret == 1)
                {
                    DevMode dev = (DevMode)Marshal.PtrToStructure(lpDevMode, typeof(DevMode));
                    _currentSreenResolution = new Size(dev.dmPelsWidth, dev.dmPelsHeight);
                }
                Marshal.FreeHGlobal(lpDevMode);

                return _currentSreenResolution;
            }
        }

        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        private static byte[] _key1 = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };

        public static byte[] AESEncrypt(string plainText, string strKey)
        {

            SymmetricAlgorithm des = Rijndael.Create();
            byte[] inputByteArray = Encoding.UTF8.GetBytes(plainText);

            byte[] keyByteArray = Encoding.UTF8.GetBytes(strKey);
            byte[] buffer = null;
            if (keyByteArray.Length != 16)
            {
                buffer = new byte[16];
                int len = Math.Min(keyByteArray.Length, 16);
                Array.Copy(keyByteArray, buffer, len);
            }
            else
            {
                buffer = keyByteArray;
            }

            des.Key = buffer;
            des.IV = _key1;
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            byte[] cipherBytes = ms.ToArray();
            cs.Close();
            ms.Close();
            return cipherBytes;
        }

        public static byte[] AESDecrypt(byte[] cipherText, string strKey)
        {
            SymmetricAlgorithm des = Rijndael.Create();
            byte[] keyByteArray = Encoding.UTF8.GetBytes(strKey);
            byte[] buffer = null;
            if (keyByteArray.Length != 16)
            {
                buffer = new byte[16];
                int len = Math.Min(keyByteArray.Length, 16);
                Array.Copy(keyByteArray, buffer, len);
            }
            else
            {
                buffer = keyByteArray;
            }
            des.Key = buffer;
            des.IV = _key1;
            byte[] decryptBytes = new byte[cipherText.Length];
            MemoryStream ms = new MemoryStream(cipherText);
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            cs.Read(decryptBytes, 0, decryptBytes.Length);
            cs.Close();
            ms.Close();
            return decryptBytes;
        }

        public static string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            if (nativeUtf8 == IntPtr.Zero)
            {
                return null;
            }
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public static string GetFRTCDeviceUUID()
        {
            string uuid = ConfigurationManager.AppSettings["FRTCUUID"];
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = System.Guid.NewGuid().ToString();
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["FRTCUUID"] != null)
                    {
                        config.AppSettings.Settings["FRTCUUID"].Value = uuid;
                        config.Save(ConfigurationSaveMode.Modified, true);
                        ConfigurationManager.RefreshSection("appSettings");
                    }
                    config = null;
                }
                catch (ConfigurationErrorsException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return uuid;
        }

        public static int LOWORD(int n)
        {
            return n & 0xffff;
        }

        public static int LOWORD(IntPtr n)
        {
            return LOWORD(unchecked((int)(long)n));
        }
    }
}
