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
    public class Logger : IDisposable
    {
        private string ThisExeFolderPath;
        private string LogFilePath;
        private StreamWriter Writer;
        private string TimeStamp;

        public Logger(string timestamp) {

            ThisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            TimeStamp = timestamp;
            LogFilePath = Path.Combine(ThisExeFolderPath, "output-" + TimeStamp + ".txt");
            Writer = new StreamWriter(LogFilePath);
        }

        public event Action<LogItem> MessageLogged;

        public void Log(string message, bool writeToFile = true) {

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

        public void LogToFile(string message) {
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

        protected virtual void OnMessageLogged(LogItem obj) {
            MessageLogged?.Invoke(obj);
        }

        public void Dispose() {
            Log("Output written to file: " + LogFilePath, writeToFile: false);
            Writer.Close();
        }
    }
}
