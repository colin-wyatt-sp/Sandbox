using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {

                ThisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                dialog.SelectedPath = ThisExeFolderPath;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK
                    || result == System.Windows.Forms.DialogResult.Yes) {

                    ServicePackLocationTextBox.Text = dialog.SelectedPath;
                    ApplyButton.IsEnabled = true;
                }
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {

            CurrentDateTime = DateTime.Now;
            TimeStamp = CurrentDateTime.ToString("yyyyMMddHHmmss");
            LogFilePath = Path.Combine(ThisExeFolderPath, "output-" + TimeStamp + ".txt");
            Writer = new StreamWriter(LogFilePath);

            try {
                Log("Starting Core ServicePack install...");
                TryApplyAllServices();
                Log("Finished patching all services.");
            }
            catch (Exception ex) {
                Log("ERROR: " + ex.Message);
            }
            finally {
                Writer.Close();
            }
        }

        private void TryApplyAllServices() {

            string[] jsonFiles = Directory.GetFiles(ServicePackLocationTextBox.Text, "*service.json", SearchOption.AllDirectories);
            Log("service config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            ServiceController[] services = ServiceController.GetServices();
            foreach (var jsonFile in jsonFiles) {

                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
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

        private void Log(string message) {

            outputTextBlock.Text = outputTextBlock.Text + Environment.NewLine + message;
            scrollViewer.ScrollToEnd();
            scrollViewer.InvalidateVisual();

            Writer.WriteLine(message);
        }

        private void TryApply(ServiceController serviceController, DirectoryInfo servicePackFolder) {
            
            try {
                Log("Applying patch for service: " + serviceController.DisplayName);
                PerformApply(serviceController, servicePackFolder);
            }
            catch (Exception e) {
                Log("ERROR applying service pack: " + e.Message);
            }
        }

        private void PerformApply(ServiceController serviceController, DirectoryInfo servicePackFolder) {

            Log("Stopping service " + serviceController.DisplayName + " ...");
            if (serviceController.Status != ServiceControllerStatus.Stopped) {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 3, 0));
                if (serviceController.Status != ServiceControllerStatus.Stopped) {
                    Log("WARN: Service \"" + serviceController.DisplayName +
                        "\" has not stopped in a timely manner. This service will not be patched; skipping.");
                    return;
                }
            }

            var serviceDirectory = new FileInfo(GetImagePath(serviceController.ServiceName)).Directory;
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
