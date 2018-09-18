using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SIQServicePackCoreInstaller.VMs;

namespace SIQServicePackCoreInstaller
{
    public static class Logger
    {
        private static string ThisExeFolderPath;
        private static string LogFilePath;
        private static StreamWriter Writer = null;
        private static string timeStamp;

        public static string Timestamp {
            get {
                return timeStamp;
            }
            set {
                FlushAndCloseFile();
                ThisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                timeStamp = value;
                LogFilePath = Path.Combine(ThisExeFolderPath, "output-" + timeStamp + ".txt");
                Writer = new StreamWriter(LogFilePath);
            }
        }

        public static event Action<LogItem> MessageLogged;

        public static void Log(string message, bool writeToFile = true) {

            try
            {
                var logItem = GetLogItemFrom(message);

                if (writeToFile)
                {
                    Writer.WriteLine(message);
                    Writer.Flush();
                }

                OnMessageLogged(logItem);
            }
            catch (Exception e)
            {
                MessageBox.Show("There was a problem logging: " + e.Message + ", LOG Message: " + message);
            }
        }

        public static void LogToFile(string message) {
            try
            {
                Writer.WriteLine(message);
                Writer.Flush();
            }
            catch (Exception e)
            {
                MessageBox.Show("There was a problem logging: " + e.Message + ", LOG Message: " + message);
            }
        }

        private static LogItem GetLogItemFrom(string message) {
            LogItem logItem = message.StartsWith("ERROR") ? new LogErrorItem {Message = message} :
                message.StartsWith("WARN") ? new LogWarnItem {Message = message} as LogItem :
                new LogInfoItem {Message = message};
            return logItem;
        }

        private static void OnMessageLogged(LogItem obj) {
            MessageLogged?.Invoke(obj);
        }

        private static void FlushAndCloseFile() {
            if (Writer != null) {
                Log("Output written to file: " + LogFilePath, writeToFile: false);
                Writer.Close();
            }
        }
    }
}
