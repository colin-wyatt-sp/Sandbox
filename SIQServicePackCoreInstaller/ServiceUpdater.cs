using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SIQServicePackCoreInstaller
{
    public class ServiceUpdater
    {
        private readonly string servicePackLocation;
        private readonly ProcessUtility processUtility;
        private Logger logger;
        private string timeStamp;

        public ServiceUpdater(string servicePackLocation, ProcessUtility processUtility, string timeStamp, Logger logger) {
            this.servicePackLocation = servicePackLocation;
            this.processUtility = processUtility;
            this.timeStamp = timeStamp;
            this.logger = logger;
        }

        public void Update() {
            string[] jsonFiles =
                Directory.GetFiles(servicePackLocation, "*service.json", SearchOption.AllDirectories);

            if (jsonFiles == null || jsonFiles.Length == 0)
            {
                logger.Log("WARN: " + "Did not find any \"service.json\" files. No services will be patched.");
                return;
            }
            logger.Log("Found the following service config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            ServiceController[] services = ServiceController.GetServices();
            foreach (var jsonFile in jsonFiles)
            {
                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
                var serviceName = jsonObject["serviceName"].Value<string>();
                logger.Log("Searching for service with name: " + serviceName);
                if (services.All(x => x.ServiceName != serviceName))
                {
                    logger.Log("Unable to find installed service matching name: " + serviceName + ".  Continuing.");
                    continue;
                }

                var serviceController = services.First(x => x.ServiceName == serviceName);
                DirectoryInfo servicePatchDirectory = new FileInfo(jsonFile).Directory;

                TryApply(serviceController, servicePatchDirectory);
            }
        }

        private void TryApply(ServiceController serviceController, DirectoryInfo servicePackFolder)
        {
            try
            {
                logger.Log("Applying patch for service: " + serviceController.DisplayName);
                PerformApply(serviceController, servicePackFolder);
            }
            catch (Exception e)
            {
                logger.Log("ERROR applying service pack for service \"" + serviceController.DisplayName + "\" : " + e.Message);
            }
        }

        private void PerformApply(ServiceController serviceController, DirectoryInfo servicePackFolder)
        {

            if (serviceController.Status != ServiceControllerStatus.Stopped)
            {
                logger.Log("Stopping service " + serviceController.DisplayName + " ...");
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 3, 0));
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    logger.Log("WARN: Service \"" + serviceController.DisplayName +
                        "\" has not stopped in a timely manner. This service will not be patched; skipping.");
                    return;
                }
                else
                {
                    Thread.Sleep(500); // give the servie a few extra milliseconds to allow it's processes to completely stop.
                }
            }

            logger.Log("Getting service path...");
            var serviceDirectory = new FileInfo(GetImagePath(serviceController.ServiceName)).Directory;

            logger.Log("Stop any service processes still running...");
            processUtility.TryKillRogueProcesses(serviceDirectory);

            var serviceDirectoryBackupPath = serviceDirectory.FullName + "_BAK-" + timeStamp;

            logger.Log("Backing up service folder \"" + serviceDirectory + "\" to \"" + new DirectoryInfo(serviceDirectoryBackupPath).Name + "\"");
            var backupDirectoryInfo = Directory.CreateDirectory(serviceDirectoryBackupPath);
            FileUtility.Copy(serviceDirectory.FullName, backupDirectoryInfo.FullName, null, overwrite: false);

            logger.Log("Copying service pack files from \"" + servicePackFolder.FullName + "\" to \"" + serviceDirectory.FullName + "\"");
            FileUtility.Copy(servicePackFolder.FullName, serviceDirectory.FullName, new[] { "service.json" }, overwrite: true);

            logger.Log("Starting service " + serviceController.DisplayName + " ...");
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 3, 0));
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                logger.Log("WARN: Service \"" + serviceController.DisplayName + "\" has been patched, but has not started in a timely manner. This may indicate a problem with the service. Continuing.");
            }
            else
            {
                logger.Log("Completed patching service: " + serviceController.DisplayName);
            }
        }


        private string GetImagePath(string serviceName)
        {

            string registryPath = @"SYSTEM\CurrentControlSet\Services\" + serviceName;
            RegistryKey keyHKLM = Registry.LocalMachine;

            RegistryKey key;
            key = keyHKLM.OpenSubKey(registryPath);

            string value = key.GetValue("ImagePath").ToString();
            key.Close();

            return Environment.ExpandEnvironmentVariables(value).Replace("\\\"", "").Replace("\"", "");
        }
    }
}
