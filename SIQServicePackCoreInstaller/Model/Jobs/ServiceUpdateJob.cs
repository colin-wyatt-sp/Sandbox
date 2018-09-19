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
        private readonly DirectoryInfo _servicePatchDirectory;
        private readonly IEnumerable<IUpdateJob> _auxiallaryJobs;

        public ServiceUpdateJob(ServiceController serviceController, DirectoryInfo servicePatchDirectory,
            IEnumerable<IUpdateJob> auxiallaryJobs)
        {
            this._serviceController = serviceController;
            this._servicePatchDirectory = servicePatchDirectory;
            _auxiallaryJobs = auxiallaryJobs;
        }

        public void performUpdate() {

            try
            {
                Logger.log("Applying patch for service: " + _serviceController.DisplayName);
                performApply(_serviceController, _servicePatchDirectory);
            }
            catch (Exception e)
            {
                Logger.log("ERROR applying service pack for service \"" + _serviceController.DisplayName + "\" : " + e.Message);
            }
        }

        private void performApply(ServiceController serviceController, DirectoryInfo servicePackFolder) {

            bool isStopped = stopService(serviceController);
            if (!isStopped) {
                Logger.log("WARN: Service \"" + serviceController.DisplayName +
                           "\" has not stopped in a timely manner. This service will not be patched; skipping.");
                return;
            }

            var job = new DirectoryUpdateJob(new DirectoryUpdateJobInfo {
                Name = "Service " + serviceController.ServiceName,
                LocationToUpdate = new FileInfo(RegistryUtility.getServicePath(serviceController.ServiceName)).Directory.FullName,
                DirectoryWithFileUpdates = servicePackFolder.FullName,
                FileExcludeList = new[] { "service.json" }
            });
            job.performUpdate();

            try {
                foreach (var auxiallaryJob in _auxiallaryJobs) {
                    auxiallaryJob.performUpdate();
                }
            }
            catch (Exception e) {
                Logger.log($"ERROR: executing auxiallary jobs for service {serviceController.DisplayName} : {e.Message}");
            }

            startService(serviceController);
        }

        private static void startService(ServiceController serviceController) {
            Logger.log("Starting service " + serviceController.DisplayName + " ...");
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 3, 0));
            if (serviceController.Status != ServiceControllerStatus.Running) {
                Logger.log("WARN: Service \"" + serviceController.DisplayName +
                           "\" has been patched, but has not started in a timely manner. This may indicate a problem with the service. Continuing.");
            }
            else {
                Logger.log("Completed patching service: " + serviceController.DisplayName);
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
    }
}