using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIQServicePackCoreInstaller.VMs;
using Path = System.IO.Path;

namespace SIQServicePackCoreInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime CurrentDateTime;
        private string TimeStamp;
        private string ThisExeFolderPath;
        private string LogFilePath;
        private StreamWriter Writer;
        private ServicePackInstallerViewModel ViewModel;
        

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new ServicePackInstallerViewModel();
            
            this.DataContext = ViewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {

                ThisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                dialog.SelectedPath = ThisExeFolderPath;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK
                    || result == System.Windows.Forms.DialogResult.Yes) {

                    ViewModel.ServicePackLocation = dialog.SelectedPath;
                    ApplyButton.IsEnabled = true;
                }
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {

            CurrentDateTime = DateTime.Now;
            TimeStamp = CurrentDateTime.ToString("yyyyMMddHHmmss");
            LogFilePath = Path.Combine(ThisExeFolderPath, "output-" + TimeStamp + ".txt");
            Writer = new StreamWriter(LogFilePath);

            Task.Factory.StartNew(TryApplyAllServices);
        }

        private void TryApplyAllServices() {

            try {
                Log("Starting Core ServicePack install...");
                PerformApplyAllServices();
                Log("Finished patching all services.");
            }
            catch (Exception e) {
                Log("ERROR: " + e.Message);
            }
            finally {
                Log("Output written to file: " + LogFilePath, writeToFile: false);
                Writer.Close();
            }
        }

        private void PerformApplyAllServices() {
            string[] jsonFiles =
                Directory.GetFiles(ViewModel.ServicePackLocation, "*service.json", SearchOption.AllDirectories);
            Log("service config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            ServiceController[] services = ServiceController.GetServices();
            foreach (var jsonFile in jsonFiles) {
                JObject jsonObject = (JObject) JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
                var serviceName = jsonObject["serviceName"].Value<string>();
                Log("Searching for service with name: " + serviceName);
                if (services.All(x => x.ServiceName != serviceName)) {
                    Log("Unable to find installed service matching name: " + serviceName + ".  Continuing.");
                    continue;
                }

                var serviceController = services.First(x => x.ServiceName == serviceName);
                DirectoryInfo servicePatchDirectory = new FileInfo(jsonFile).Directory;

                TryApply(serviceController, servicePatchDirectory);
            }
        }

        private void Log(string message, bool writeToFile=true) {

            try {
                LogItem logItem = message.StartsWith("ERROR") ? new LogErrorItem { Message = message } :
                    message.StartsWith("WARN") ? new LogWarnItem { Message = message } as LogItem :
                    new LogInfoItem { Message = message };

                ViewModel.LogItems.Add(logItem);

                if (writeToFile)
                    Writer.WriteLine(message);

                this.Dispatcher.BeginInvoke(new Action(() => { scrollViewer.ScrollToEnd(); }));
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
            
        }

        private void TryApply(ServiceController serviceController, DirectoryInfo servicePackFolder) {
            
            try {
                Log("Applying patch for service: " + serviceController.DisplayName);
                PerformApply(serviceController, servicePackFolder);
            }
            catch (Exception e) {
                Log("ERROR applying service pack for service \"" + serviceController.DisplayName + "\" : " + e.Message);
            }
        }

        private void PerformApply(ServiceController serviceController, DirectoryInfo servicePackFolder) {

            if (serviceController.Status != ServiceControllerStatus.Stopped) {
                Log("Stopping service " + serviceController.DisplayName + " ...");
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 3, 0));
                if (serviceController.Status != ServiceControllerStatus.Stopped) {
                    Log("WARN: Service \"" + serviceController.DisplayName +
                        "\" has not stopped in a timely manner. This service will not be patched; skipping.");
                    return;
                }
            }

            Log("Getting service path...");
            var serviceDirectory = new FileInfo(GetImagePath(serviceController.ServiceName)).Directory;
            
            Log("Checking for service processes still running...");
            TryKillRogueProcesses(serviceDirectory);

            var serviceDirectoryBackupPath = serviceDirectory.FullName + "_BAK-" + TimeStamp;
            
            Log("Backing up service folder \"" + serviceDirectory + "\" to \"" + new DirectoryInfo(serviceDirectoryBackupPath).Name + "\"");
            var backupDirectoryInfo = Directory.CreateDirectory(serviceDirectoryBackupPath);
            Copy(serviceDirectory.FullName, backupDirectoryInfo.FullName);

            Log("Copying service pack files from \"" + servicePackFolder.FullName + "\" to \"" + serviceDirectory.FullName + "\"");
            Copy(servicePackFolder.FullName, serviceDirectory.FullName, overwrite: true);

            Log("Starting service " + serviceController.DisplayName + " ...");
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 3, 0));
            if (serviceController.Status != ServiceControllerStatus.Running) {
                Log("WARN: Service \"" + serviceController.DisplayName + "\" has been patched, but has not started in a timely manner. This may indicate a problem with the service. Continuing.");
            }
            else {
                Log("Completed patching service: " + serviceController.DisplayName);
            }
        }

        private void TryKillRogueProcesses(DirectoryInfo serviceDirectory) {

            //Log("calling GetProcesses...");
            Process[] runningProcesses = Process.GetProcesses();
            //Log("iterating over running processes...");
            foreach (var runningProcess in runningProcesses) {

                string processFileName;

                try {
                    //Log("DEBUG: process : " + runningProcess.ProcessName);
                    processFileName = runningProcess.MainModule.FileName;
                }
                catch (Exception) {
                    // some processes don't like you looking at them.
                    //Log("ERROR: access denied. " + runningProcess.ProcessName);
                    //Log("WARN: test");
                    continue;
                }
                    
                //Log("DEBUG: process: " + processFileName);
                var directory = new FileInfo(processFileName).DirectoryName;
                //Log("DEBUG: " + directory);
                if (string.Compare(serviceDirectory.FullName, directory,
                        StringComparison.InvariantCultureIgnoreCase) == 0) {

                    try { 
                        Log("Killing " + processFileName);
                        runningProcess.Kill();
                    }
                    catch (Exception e) {
                        Log("ERROR killing service process: " + processFileName + " : " + e.Message);
                    }
                }
            }
        }

        private void Copy(string sourceDir, string targetDir, bool overwrite=false) {

            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir)) {
                if (file.EndsWith("service.json")) continue;
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        private string GetImagePath(string serviceName)
        {
            string registryPath = @"SYSTEM\CurrentControlSet\Services\" + serviceName;
            RegistryKey keyHKLM = Registry.LocalMachine;

            RegistryKey key;
            //if (MachineName != "")
            //{
            //    key = RegistryKey.OpenRemoteBaseKey
            //        (RegistryHive.LocalMachine, this.MachineName).OpenSubKey(registryPath);
            //}
            //else
            //{
                key = keyHKLM.OpenSubKey(registryPath);
            //}

            string value = key.GetValue("ImagePath").ToString();
            key.Close();

            return Environment.ExpandEnvironmentVariables(value).Replace("\\\"", "").Replace("\"", "");
            //return value;
        }
    }
}
