using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.DataTypes;
using SIQServicePackCoreInstaller.Model.Utility;

namespace SIQServicePackCoreInstaller.Model.Jobs {
    public class ServiceUpdateJob : IUpdateJob {

        private readonly ServiceController _serviceController;
        private readonly DirectoryInfo _servicePackFolder;
        private readonly IEnumerable<IUpdateJob> _auxiallaryJobs;

        public ServiceUpdateJob(ServiceController serviceController, DirectoryInfo servicePackFolder,
            IEnumerable<IUpdateJob> auxiallaryJobs)
        {
            this._serviceController = serviceController;
            this._servicePackFolder = servicePackFolder;
            _auxiallaryJobs = auxiallaryJobs;
        }

        public string Name => _serviceController.ServiceName;

        public void performUpdate() {

            bool isStopped = stopService(_serviceController);
            if (!isStopped) {
                Logger.log("WARN: Service \"" + _serviceController.DisplayName +
                           "\" has not stopped in a timely manner. This service will not be patched; skipping.");
                return;
            }

            try {
                updateServiceFiles();
                runAuxialliaryJobs();
            }
            catch (Exception e) {
                Logger.log($"ERROR: There was a problem updating service {Name} files. ExceptionMsg: {e.Message}");
            }

            startService(_serviceController);
        }

        private void runAuxialliaryJobs() {
            try {
                foreach (var auxiallaryJob in _auxiallaryJobs) {
                    auxiallaryJob.performUpdate();
                }
            }
            catch (Exception e) {
                Logger.log($"ERROR: executing auxiallary jobs for service {_serviceController.DisplayName} : {e.Message}");
            }
        }

        private void updateServiceFiles() {
            var job = new DirectoryUpdateJob(new DirectoryUpdateJobInfo {
                Name = _serviceController.ServiceName,
                LocationToUpdate =
                    new FileInfo(RegistryUtility.getServicePath(_serviceController.ServiceName)).Directory.FullName,
                DirectoryWithFileUpdates = _servicePackFolder.FullName,
                FileExcludeList = new[] {"service.json", "app.config.update" }
            });
            job.performUpdate();
        }

        private static void startService(ServiceController serviceController) {

            Logger.log($"Starting service {serviceController.DisplayName} ...");
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 3, 0));
            if (serviceController.Status != ServiceControllerStatus.Running) {
                Logger.log("WARN: Service \"" + serviceController.DisplayName +
                           "\" has been patched, but has not started in a timely manner. This may indicate a problem with the service. Continuing.");
            }
            else {
                Logger.logToFile($"Finished updating service: {serviceController.DisplayName}");
            }
        }

        private static bool stopService(ServiceController serviceController) {
            if (serviceController.Status != ServiceControllerStatus.Stopped) {
                Logger.log("Stopping service " + serviceController.DisplayName + " ...");
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 3, 0));
                if (serviceController.Status != ServiceControllerStatus.Stopped) {
                    return false;
                }
                else {
                    Thread.Sleep(1200); // give the servie a few extra milliseconds to allow it's processes to completely stop.
                }
            }

            return true;
        }

        public override string ToString()
        {
            return $"ServiceUpdateJob {Name}";
        }
    }
}