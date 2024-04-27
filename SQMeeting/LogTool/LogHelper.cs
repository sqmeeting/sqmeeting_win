using NLog;
using NLog.Common;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.LogTool
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal,
        Off
    }
    public class LogHelper
    {

        const string ProductName = "FRTC_Windows_App";

        private static LogLevel threshold = LogLevel.Debug;
        private static readonly NLog.LogLevel[] nlogLevel =
        {
            NLog.LogLevel.Trace,
            NLog.LogLevel.Debug,
            NLog.LogLevel.Info,
            NLog.LogLevel.Warn,
            NLog.LogLevel.Error,
            NLog.LogLevel.Fatal,
            NLog.LogLevel.Off
        };
        private const int LOG_MAX_LENGTH = 1024;
        private string logPath = null;
        private static Logger logger = null;
        private static volatile LogHelper instance = null;
        private static object syncObj = new object();

        private LogHelper() { }

        public static void InitializeNullLogger()
        {
            if (instance == null)
            {
                instance = new LogHelper();
                if (logger == null)
                {
                    NullTarget target = new NullTarget();
                    target.Layout = "${message}";
                    target.FormatMessage = true;
                    NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, nlogLevel[(int)threshold]);
                    logger = LogManager.GetLogger(ProductName);
                }
            }
        }

        public static void InitializeLogger(String path)
        {
            if (instance == null)
            {
                lock (syncObj)
                {
                    if (instance == null)
                    {
                        instance = new LogHelper();
                        if (string.IsNullOrEmpty(path))
                        {
                            path = Environment.CurrentDirectory;
                        }
                        instance.InitializeNLogger(path);
                    }
                }
            }
        }

        private void InitializeNLogger(String path)
        {
            if (logger == null)
            {
                logPath = Path.Combine(path, "logs");

                SplitGroupTarget target = new SplitGroupTarget();
                target.Targets.Add(ConsoleLogger());
                target.Targets.Add(FileLogger());
                //NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, NLog.LogLevel.Info)
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, nlogLevel[(int)threshold]);
                //FileLogger();
                logger = LogManager.GetLogger(ProductName);
            }
        }

        private DebuggerTarget ConsoleLogger()
        {
            DebuggerTarget target = new DebuggerTarget();
            target.Layout = @"${longdate}|${pad:padding=5:inner=${level:uppercase=true}}|${message}${onexception:${newline}EXCEPTION\: ${exception:format=ToString}}";
            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, nlogLevel[(int)threshold]);
            return target;
        }

        private FileTarget FileLogger(bool configureTarget = true)
        {
            using (FileTarget target = new FileTarget
            {
                Layout = "${longdate} ${threadid} ${logger} ${message}",
                FileName = "${basedir}/logs/logfile.txt",
                KeepFileOpen = false,
                Encoding = Encoding.UTF8,
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ArchiveAboveSize = 1024000,
                ArchiveFileName = "${basedir}/logs/logfile.txt",
            })
            {
                target.Layout = @"${longdate}|${threadid}|${pad:padding=5:inner=${level:uppercase=true}}|${message}${onexception:${newline}EXCEPTION\: ${exception:format=ToString}}";
                target.FileName = Path.Combine(logPath, ProductName + ".log");
                target.KeepFileOpen = false;
                target.Encoding = Encoding.UTF8;

                //Archival Options
                target.ArchiveFileName = Path.Combine(logPath, ProductName + ".log.{#}");
                target.ArchiveNumbering = ArchiveNumberingMode.Sequence;
                target.ArchiveAboveSize = 1024 * 30 * 1000;
                //target.ArchiveDateFormat = "yyyyMMdd";
                target.MaxArchiveFiles = 5;

                if (configureTarget)
                    NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, nlogLevel[(int)threshold]);

                return target;
            }
        }

        private void AsynFileLogger()
        {
            AsyncTargetWrapper wrapper = new AsyncTargetWrapper
            {
                WrappedTarget = FileLogger(false),
                QueueLimit = 5000,
                OverflowAction = AsyncTargetWrapperOverflowAction.Discard
            };
            if (wrapper != null)
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(wrapper, nlogLevel[(int)threshold]);
        }

        private void TroubleshootLogger()
        {
            InternalLogger.LogToConsole = true;
            InternalLogger.LogFile = @"C:\Debug\log.txt";
            InternalLogger.LogWriter = new StringWriter();
            InternalLogger.LogLevel = NLog.LogLevel.Trace;
        }

        #region Public Static Methods

        public static void SetLevel(LogLevel level)
        {
            threshold = level;

            //Update the logger
            NLog.LogLevel targetLevel = NLog.LogLevel.FromString(level.ToString());
            foreach (var rule in LogManager.Configuration.LoggingRules)
            {
                rule.EnableLoggingForLevel(targetLevel);
            }
            LogManager.ReconfigExistingLoggers();

        }

        public static void Debug(string message)
        {
            SendLogMessage(LogLevel.Debug, message);
        }

        public static void Debug(string format, params object[] args)
        {
            SendLogMessage(LogLevel.Debug, string.Format(format, args));
        }

        public static void DebugEx(string format, params object[] args)
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            try
            {
                //When there is no pdb files, line number will be 0.
                string lineInfo = sf.GetFileLineNumber() <= 0
                    ? ""
                    : string.Format("#Line {0}: ", sf.GetFileLineNumber());
                string logMsg = string.Format("{0}{1}::{2}: {3}", lineInfo, sf.GetMethod().ReflectedType.Name,
                    sf.GetMethod(), string.Format(format, args));

                SendLogMessage(LogLevel.Debug, logMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public static void DebugMethodEnter()
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);
            try
            {
                //When there is no pdb files, line number will be 0.
                string lineInfo = sf.GetFileLineNumber() <= 0
                    ? ""
                    : string.Format("#Line {0}: ", sf.GetFileLineNumber());
                string logMsg = string.Format("{0}{1}::{2} Enter...", lineInfo, sf.GetMethod().ReflectedType.Name,
                    sf.GetMethod());

                SendLogMessage(LogLevel.Debug, logMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public static void DebugMethodExit()
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(1);

            try
            {
                //When there is no pdb files, line number will be 0.
                string lineInfo = sf.GetFileLineNumber() <= 0
                    ? ""
                    : string.Format("#Line {0}: ", sf.GetFileLineNumber());
                string logMsg = string.Format("{0}{1}::{2} Exit...", lineInfo, sf.GetMethod().ReflectedType.Name,
                    sf.GetMethod());

                SendLogMessage(LogLevel.Debug, logMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public static void Info(string message)
        {
            SendLogMessage(LogLevel.Info, message);
        }

        public static void Info(string format, params object[] args)
        {
            SendLogMessage(LogLevel.Info, string.Format(format, args));
        }

        public static void Warn(string message)
        {
            SendLogMessage(LogLevel.Warn, message);
        }

        public static void Warn(string format, params object[] args)
        {
            SendLogMessage(LogLevel.Warn, string.Format(format, args));
        }

        public static void Error(string message)
        {
            SendLogMessage(LogLevel.Error, message);
        }

        public static void Error(string format, params object[] args)
        {
            SendLogMessage(LogLevel.Error, string.Format(format, args));
        }

        public static void LogMessage(LogLevel level, string message)
        {
            SendLogMessage(level, message);
        }

        /// <summary>
        /// The function to trace exception.
        /// </summary>
        /// <param name="ex">The traced exception</param>
        public static void Exception(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            Error("Message: {0}, Callstack: {1}", ex.Message, ex.StackTrace != null ? ex.StackTrace.ToString() : "null");
        }

        #endregion

        #region Private Static Methods

        private static void SendLogMessage(LogLevel level, string message)
        {
            if (level >= threshold)
            {
                SaveMessage(level, message);
            }
        }


        /// <summary>
        /// The function to save messages in log file.
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">String message to write in text file</param>
        private static void SaveMessage(LogLevel level, string message)
        {
            if (instance == null)
            {
                InitializeLogger(null);
            }
            if (message.Length > LOG_MAX_LENGTH)
            {
                message = message.Substring(0, LOG_MAX_LENGTH);
            }

            try
            {
                if (logger != null)
                {
                    logger.Log(nlogLevel[(int)level], message);
                }
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine("IO Error: {0}, Callstack: {1}", ex.Message, ex.StackTrace != null ? ex.StackTrace.ToString() : "null");
                //Log to sytem event viewer.
            }
            catch (Exception ex)
            {
                Console.WriteLine("IO Error: {0}, Callstack: {1}", ex.Message, ex.StackTrace != null ? ex.StackTrace.ToString() : "null");
            }
        }
        #endregion
    }
}
