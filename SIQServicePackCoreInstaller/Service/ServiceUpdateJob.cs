using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;

namespace SIQServicePackCoreInstaller {
    public class ServiceUpdateJob : IUpdateJob {

        private ServiceController serviceController;
        private DirectoryInfo servicePatchDirectory;
        private ProcessUtility processUtility;
        private readonly IEnumerable<IUpdateJob> _auxiallaryJobs;

        public ServiceUpdateJob(ServiceController serviceController, DirectoryInfo servicePatchDirectory,
            ProcessUtility processUtility, IEnumerable<IUpdateJob> auxiallaryJobs)
        {
            this.serviceController = serviceController;
            this.servicePatchDirectory = servicePatchDirectory;
            this.processUtility = processUtility;
            _auxiallaryJobs = auxiallaryJobs;
        }

        public void PerformUpdate() {

            try
            {
                Logger.Log("Applying patch for service: " + serviceController.DisplayName);
                PerformApply(serviceController, servicePatchDirectory);
            }
            catch (Exception e)
            {
                Logger.Log("ERROR applying service pack for service \"" + serviceController.DisplayName + "\" : " + e.Message);
            }
        }

        private void PerformApply(ServiceController serviceController, DirectoryInfo servicePackFolder)
        {

            if (serviceController.Status != ServiceControllerStatus.Stopped)
            {
                Logger.Log("Stopping service " + serviceController.DisplayName + " ...");
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 3, 0));
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    Logger.Log("WARN: Service \"" + serviceController.DisplayName +
                        "\" has not stopped in a timely manner. This service will not be patched; skipping.");
                    return;
                }
                else
                {
                    Thread.Sleep(1500); // give the servie a few extra milliseconds to allow it's processes to completely stop.
                }
            }

            Logger.Log("Getting service path...");
            var serviceDirectory = new FileInfo(RegistryUtility.GetServicePath(serviceController.ServiceName)).Directory;

            Logger.Log("Stop any service processes still running...");
            processUtility.TryKillRogueProcesses(serviceDirectory);

            var serviceDirectoryBackupPath = serviceDirectory.FullName + "_BAK-" + Logger.Timestamp;

            Logger.Log("Backing up service folder \"" + serviceDirectory + "\" to \"" + new DirectoryInfo(serviceDirectoryBackupPath).Name + "\"");
            var backupDirectoryInfo = Directory.CreateDirectory(serviceDirectoryBackupPath);
            FileUtility.Copy(serviceDirectory.FullName, backupDirectoryInfo.FullName, null, overwrite: false);

            Logger.Log("Copying service pack files from \"" + servicePackFolder.FullName + "\" to \"" + serviceDirectory.FullName + "\"");
            FileUtility.Copy(servicePackFolder.FullName, serviceDirectory.FullName, new[] { "service.json" }, overwrite: true);

            try {
                foreach (var auxiallaryJob in _auxiallaryJobs) {
                    auxiallaryJob.PerformUpdate();
                }
            }
            catch (Exception e) {
                Logger.Log($"ERROR: executing auxiallary jobs for service {serviceController.DisplayName} : {e.Message}");
            }

            Logger.Log("Starting service " + serviceController.DisplayName + " ...");
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 3, 0));
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                Logger.Log("WARN: Service \"" + serviceController.DisplayName + "\" has been patched, but has not started in a timely manner. This may indicate a problem with the service. Continuing.");
            }
            else
            {
                Logger.Log("Completed patching service: " + serviceController.DisplayName);
            }
        }

    }
}