using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
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
        private ServicePackInstallerViewModel ViewModel;
        private Logger Logger;
        private ProcessUtility processUtility;
        private ServiceUpdater serviceUpdater;
        private WebsiteUpdater websiteUpdater;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new ServicePackInstallerViewModel();
            DataContext = ViewModel;
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

            ViewModel.LogItems.Clear();
            Task.Factory.StartNew(TryApplyAll);
        }

        private void TryApplyAll() {

            try {
                InitializeUpdaters();

                Logger.Log("Starting Core ServicePack install...");

                serviceUpdater.Update();
                Logger.Log("Finished patching all services.");

                websiteUpdater.Update();
                Logger.Log("Finished patching all services.");
            }
            catch (Exception e) {
                Logger.Log("ERROR: " + e.Message);
            }
            finally {
                DisposeLogger();
            }
        }

        private void InitializeUpdaters() {

            InitializeLogger();

            processUtility = new ProcessUtility(Logger);
            serviceUpdater = new ServiceUpdater(ViewModel.ServicePackLocation, processUtility, TimeStamp, Logger);
            websiteUpdater = new WebsiteUpdater(ViewModel.ServicePackLocation, processUtility, TimeStamp, Logger);
        }

        private void InitializeLogger() {

            CurrentDateTime = DateTime.Now;
            TimeStamp = CurrentDateTime.ToString("yyyyMMddHHmmss");
            Logger = new Logger(TimeStamp);
            Logger.MessageLogged += Logger_MessageLogged;
        }


        private void Logger_MessageLogged(LogItem logItem)
        {
            ViewModel.LogItems.Add(logItem);
            Dispatcher.BeginInvoke(new Action(() => { scrollViewer.ScrollToEnd(); }));
        }

        private void DisposeLogger() {

            Logger.MessageLogged -= Logger_MessageLogged;
            Logger.Dispose();
        }
    }
}
