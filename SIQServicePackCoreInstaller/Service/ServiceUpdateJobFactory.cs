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
    public class ServiceUpdateJobFactory : IUpdateJobFactory
    {
        private readonly string servicePackLocation;
        private readonly ProcessUtility processUtility;

        public ServiceUpdateJobFactory(string servicePackLocation, ProcessUtility processUtility) {
            this.servicePackLocation = servicePackLocation;
            this.processUtility = processUtility;
        }

        public string Name => "Service";

        public IEnumerable<IUpdateJob> GetJobs() {

            string[] jsonFiles = Directory.GetFiles(servicePackLocation, "*service.json", SearchOption.AllDirectories);

            if (jsonFiles.Length == 0) {
                yield break;
            }
            Logger.Log("Found the following service config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            ServiceController[] services = ServiceController.GetServices();
            foreach (var jsonFile in jsonFiles)
            {
                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
                var serviceName = jsonObject["serviceName"].Value<string>();
                Logger.Log("Searching for service with name: " + serviceName);
                if (services.All(x => x.ServiceName != serviceName))
                {
                    Logger.Log("Unable to find installed service matching name: " + serviceName + ".  Continuing.");
                    continue;
                }

                var serviceController = services.First(x => x.ServiceName == serviceName);
                DirectoryInfo servicePatchDirectory = new FileInfo(jsonFile).Directory;

                 yield return new UpdateServiceJob(serviceController, servicePatchDirectory, processUtility);
            }
        }
        
    }
}
