using SQMeeting.FRTCView;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace SQMeeting
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string UniqueID = @"54DC899A-7A1B-4A5E-A760-26F73235F25C";
        private const string UriScheme = "frtcmeeting://";
        private Mutex frtcMeetingUnique;
        public string StartupArgSchemaString = string.Empty;

        public App()
        {

        }

        public void RemoveMutex()
        {
            if (frtcMeetingUnique != null)
            {
                frtcMeetingUnique.ReleaseMutex();
                frtcMeetingUnique.Close();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string path = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Directory.SetCurrentDirectory(path);
            SetCulture();

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            bool createNew = true;
            frtcMeetingUnique = new Mutex(true, UniqueID, out createNew);
            if (!createNew)
            {
                if (e.Args.Count() > 0)
                {
                    if (e.Args[0].ToLower().StartsWith(UriScheme))
                    {
                        IntPtr url = IntPtr.Zero;
                        IntPtr pData = IntPtr.Zero;
                        bool sendSuccess = false;
                        //MessageBox.Show("1");
                        try
                        {
                            StartupArgSchemaString = e.Args[0].Substring(UriScheme.Length).TrimEnd('/');
                            byte[] buffer = Encoding.Unicode.GetBytes(StartupArgSchemaString);
                            int size = buffer.Length;
                            url = Marshal.AllocHGlobal((int)size);
                            Marshal.Copy(buffer, 0, url, size);

                            tagCOPYDATASTRUCT data = new tagCOPYDATASTRUCT();
                            data.cbData = (uint)size;
                            data.lpData = url;
                            data.dwData = IntPtr.Zero;

                            pData = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                            Marshal.StructureToPtr<tagCOPYDATASTRUCT>(data, pData, true);

                            try
                            {
                                Process[] p = Process.GetProcessesByName("SQMeeting");
                                if (p.Length > 0)
                                {
                                    var process = p.OrderBy(x => x.StartTime);
                                    foreach (Process proc in process)
                                    {
                                        if (proc.MainModule.FileName == Process.GetCurrentProcess().MainModule.FileName)
                                        {
                                            IntPtr hwnd = proc.MainWindowHandle;
                                            if (hwnd != IntPtr.Zero)
                                            {
                                                Win32API.SendMessage(hwnd, Win32API.WM_COPYDATA, IntPtr.Zero, pData);
                                            }
                                        }
                                    }
                                    sendSuccess = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogTool.LogHelper.Exception(ex);
                                LogTool.LogHelper.Debug("launch args is {0}", e.Args[0]);
                            }
                            if (!sendSuccess && pData != null && pData != IntPtr.Zero)
                            {
                                string title = "SQMeeting_Windows_" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                                LogTool.LogHelper.Debug("Try find frtc window {0}", title);
                                StringBuilder sb = new StringBuilder(255);
                                bool foundAndSend = false;
                                Win32API.EnumWindows(new Win32API.EnumWindowsCallback((hwnd, lparam) =>
                                {
                                    sb.Clear();
                                    if (0 < Win32API.GetWindowText(hwnd, sb, 255))
                                    {
                                        LogTool.LogHelper.Debug("Enum at window {0}", sb.ToString());
                                        if (title == sb.ToString())
                                        {
                                            LogTool.LogHelper.Debug("Find frtc window {0}", sb.ToString());
                                            Win32API.SendMessage(hwnd, Win32API.WM_COPYDATA, IntPtr.Zero, pData);
                                            foundAndSend = true;
                                            return false;
                                        }
                                        else
                                        {
                                            return true;
                                        }
                                    }
                                    return true;
                                }), 0);
                                if (!foundAndSend) { LogTool.LogHelper.Debug("Can't find or send msg to exist frtc meeting window"); }
                            }
                        }
                        catch (Exception eex)
                        {
                            LogTool.LogHelper.Exception(eex);
                            LogTool.LogHelper.Debug("launch args is {0}", e.Args[0]);
                            MessageBox.Show("Invoke exists process failed.");
                        }
                        finally
                        {
                            if (url != null && url != IntPtr.Zero)
                                Marshal.FreeHGlobal(url);
                            if (pData != null && pData != IntPtr.Zero)
                                Marshal.FreeHGlobal(pData);
                        }
                    }
                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show(SQMeeting.Properties.Resources.FRTC_MEETING_INSTANCE_EXIST);
                    Environment.Exit(0);
                }
                return;
            }
            else
            {
                if (e.Args.Count() > 0)
                {
                    if (e.Args[0].ToLower().StartsWith(UriScheme))
                    {
                        StartupArgSchemaString = e.Args[0].Substring(UriScheme.Length).TrimEnd('/');
                    }

                }
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogTool.LogHelper.Exception(e.Exception);
            e.SetObserved();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
                LogTool.LogHelper.Exception(e.ExceptionObject as Exception);
            else
                LogTool.LogHelper.Error(e.ExceptionObject.ToString());
            MessageBox.Show(e.ExceptionObject.ToString(), "FRTC Meeting Crashed");
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogTool.LogHelper.Exception(e.Exception);
            MessageBox.Show(e.Exception.ToString(), "FRTC Meeting Crashed");
        }

        private void SetCulture()
        {
            string setCulture = string.Empty;

            if (ConfigurationManager.AppSettings["AppLanguage"] != null)
            {
                setCulture = ConfigurationManager.AppSettings["AppLanguage"];
            }
            else
            {
                string language = UIHelper.GetResourceCultureName();
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings.Add("AppLanguage", language);
                    config.Save(ConfigurationSaveMode.Modified, true);
                    ConfigurationManager.RefreshSection("appSettings");
                    config = null;
                }
                catch (Exception ex) { LogTool.LogHelper.Exception(ex); }
            }
            string resourceCultureName = UIHelper.GetResourceCultureName(setCulture);
            try
            {
                SQMeeting.Properties.Resources.Culture = new CultureInfo(resourceCultureName);
            }
            catch (Exception ex)
            {
                SQMeeting.Properties.Resources.Culture = new CultureInfo("zh-CHS");
            }

            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(
                  System.Windows.Markup.XmlLanguage.GetLanguage(SQMeeting.Properties.Resources.Culture.IetfLanguageTag)));
        }

    }
}
