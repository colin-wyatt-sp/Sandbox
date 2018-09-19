using System;
using System.Collections.Generic;
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
using SIQServicePackCoreInstaller.Properties;
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
        private ProcessUtility processUtility;
        private ServiceUpdateJobFactory _serviceUpdateJobFactory;
        private WebsiteUpdateJobFactory _websiteUpdateJobFactory;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new ServicePackInstallerViewModel();
            DataContext = ViewModel;
            Logger.MessageLogged += Logger_MessageLogged;

            if (Settings.Default.runFromCurrentDir) {
                ThisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                ViewModel.ServicePackLocation = ThisExeFolderPath;
                ApplyButton_Click(null, null);
            }
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
                InitializeLogger();

                var updateJobFactories = getUpdateFactories();

                Logger.Log("Starting ServicePack install...");
                foreach (var factory in updateJobFactories) {
                    var jobs = factory.GetJobs().ToList();
                    if (jobs.Any()) {
                        Logger.Log($"Beginning {factory.Name} updates");
                        foreach (var updateJob in jobs) {
                            updateJob.PerformUpdate();
                        }
                        Logger.Log($"Finished updating for {factory.Name} types");
                    }
                    else {
                        Logger.Log($"No update jobs created for {factory.Name} update type");
                    }
                }
            }
            catch (Exception e) {
                Logger.Log("ERROR: " + e.Message);
            }
        }

        private IEnumerable<IUpdateJobFactory> getUpdateFactories() {

            processUtility = new ProcessUtility();
            yield return new ServiceUpdateJobFactory(ViewModel.ServicePackLocation, processUtility);
            yield return new WebsiteUpdateJobFactory(ViewModel.ServicePackLocation, processUtility);
        }

        private void InitializeLogger() {

            CurrentDateTime = DateTime.Now;
            TimeStamp = CurrentDateTime.ToString("yyyyMMddHHmmss");
            Logger.Timestamp = TimeStamp;
        }


        private void Logger_MessageLogged(LogItem logItem)
        {
            ViewModel.LogItems.Add(logItem);
            Dispatcher.BeginInvoke(new Action(() => { scrollViewer.ScrollToEnd(); }));
        }

    }
}
