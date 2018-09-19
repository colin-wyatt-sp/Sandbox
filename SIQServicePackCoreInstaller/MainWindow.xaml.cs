using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.Factories;
using SIQServicePackCoreInstaller.Properties;

namespace SIQServicePackCoreInstaller {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private DateTime _currentDateTime;
        private string _timeStamp;
        private string _thisExeFolderPath;
        private readonly ServicePackInstallerViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ServicePackInstallerViewModel();
            DataContext = _viewModel;
            Logger.MessageLogged += loggerMessageLogged;

            if (Settings.Default.runFromCurrentDir) {
                _thisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                _viewModel.ServicePackLocation = _thisExeFolderPath;
                applyButtonClick(null, null);
            }
        }

        private void buttonClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {

                _thisExeFolderPath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                dialog.SelectedPath = _thisExeFolderPath;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK
                    || result == System.Windows.Forms.DialogResult.Yes) {

                    _viewModel.ServicePackLocation = dialog.SelectedPath;
                    ApplyButton.IsEnabled = true;
                }
            }
        }

        private void applyButtonClick(object sender, RoutedEventArgs e) {

            _viewModel.LogItems.Clear();
            Task.Factory.StartNew(tryApplyAll);
        }

        private void tryApplyAll() {

            try {
                initializeLogger();

                var updateJobFactories = getUpdateFactories();

                Logger.log("Starting ServicePack install");
                foreach (var factory in updateJobFactories) {

                    var jobs = factory.getJobs().ToList();
                    if (jobs.Any()) {

                        Logger.log($"Beginning {factory.Name} updates");
                        foreach (var updateJob in jobs) {
                            doJobUpdate(updateJob);
                        }
                        Logger.log($"Finished updating {factory.Name}s");
                    }
                    else {
                        Logger.log($"No update jobs created for {factory.Name} update type");
                    }
                }
            }
            catch (Exception e) {
                Logger.log("ERROR: " + e.Message);
            }
        }

        private static void doJobUpdate(IUpdateJob updateJob) {

            try {
                Logger.log($"Updating {updateJob.Name}");
                updateJob.performUpdate();
            }
            catch (Exception e) {
                Logger.log($"ERROR: There was a problem updating {updateJob}, ExceptionMsg: {e.Message}");
            }
        }

        private IEnumerable<IUpdateJobFactory> getUpdateFactories() {

            yield return new ServiceUpdateJobFactory(_viewModel.ServicePackLocation);
            yield return new WebsiteUpdateJobFactory(_viewModel.ServicePackLocation);
            yield return new ClientUpdateJobFactory(_viewModel.ServicePackLocation);
        }

        private void initializeLogger() {

            _currentDateTime = DateTime.Now;
            _timeStamp = _currentDateTime.ToString("yyyyMMddHHmmss");
            Logger.Timestamp = _timeStamp;
        }


        private void loggerMessageLogged(LogItem logItem)
        {
            _viewModel.LogItems.Add(logItem);
            Dispatcher.BeginInvoke(new Action(() => { scrollViewer.ScrollToEnd(); }));
        }

    }
}
