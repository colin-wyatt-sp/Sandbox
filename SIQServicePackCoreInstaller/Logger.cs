using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace SIQServicePackCoreInstaller
{
    public static class Logger
    {
        private static string _thisExeFolderPath;
        private static string _logFilePath;
        private static StreamWriter _writer;
        private static string _timeStamp;

        public static string Timestamp {
            get => _timeStamp;
            set {
                flushAndCloseFile();
                _thisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                _timeStamp = value;
                _logFilePath = Path.Combine(_thisExeFolderPath, "output-" + _timeStamp + ".txt");
                _writer = new StreamWriter(_logFilePath);
            }
        }

        public static event Action<LogItem> MessageLogged;

        public static void log(string message, bool writeToFile = true) {

            try
            {
                var logItem = getLogItemFrom(message);

                if (writeToFile)
                {
                    _writer.WriteLine(message);
                    _writer.Flush();
                }

                onMessageLogged(logItem);
            }
            catch (Exception e)
            {
                MessageBox.Show("There was a problem logging: " + e.Message + ", LOG Message: " + message);
            }
        }

        public static void logToFile(string message) {
            try
            {
                _writer.WriteLine(message);
                _writer.Flush();
            }
            catch (Exception e)
            {
                MessageBox.Show("There was a problem logging: " + e.Message + ", LOG Message: " + message);
            }
        }

        private static LogItem getLogItemFrom(string message) {
            LogItem logItem;
            if (message.StartsWith("ERROR"))
                logItem = new LogErrorItem {Message = message} as LogItem;
            else if (message.StartsWith("WARN"))
                logItem = new LogWarnItem {Message = message} as LogItem;
            else if (message.StartsWith("INFO"))
                logItem = new LogInfoItem { Message = message } as LogItem;
            else
                logItem = new LogDebugItem {Message = message} as LogItem;

            return logItem;
        }

        private static void onMessageLogged(LogItem obj) {
            MessageLogged?.Invoke(obj);
        }

        private static void flushAndCloseFile() {
            if (_writer != null) {
                log("Output written to file: " + _logFilePath, writeToFile: false);
                _writer.Close();
            }
        }
    }
}
