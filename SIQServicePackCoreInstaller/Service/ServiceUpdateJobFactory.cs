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
using System.Text.RegularExpressions;
using SIQServicePackCoreInstaller.Xml;

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
                return new List<IUpdateJob>();
            }
            Logger.Log("Found the following service config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            var jobs = new List<IUpdateJob>();
            ServiceController[] services = ServiceController.GetServices();
            foreach (var jsonFile in jsonFiles)
            {
                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));

                var serviceName = jsonObject["serviceName"].Value<string>();
                Logger.Log("Searching for service with name: " + serviceName);
                if (services.All(x => !Regex.Match(x.ServiceName, serviceName).Success))
                {
                    // TODO: remove - this is for dev testing only
                    jobs.AddRange(new XmlUpdateJobFactory(new FileInfo(jsonFile).Directory.FullName, jsonFile).GetJobs());
                    Logger.Log("Unable to find installed service matching name: " + serviceName + ".  Continuing.");
                    continue;
                }

                var serviceControllers = services.Where(x => Regex.Match(x.ServiceName, serviceName).Success).ToList();
                foreach (var serviceController in serviceControllers) {

                    DirectoryInfo servicePatchDirectory = new FileInfo(jsonFile).Directory;
                    var auxiallaryJobs =
                        new XmlUpdateJobFactory(
                            new FileInfo(RegistryUtility.GetServicePath(serviceController.ServiceName)).Directory
                                .FullName, jsonFile).GetJobs();

                    jobs.Add(new ServiceUpdateJob(serviceController, servicePatchDirectory, processUtility, auxiallaryJobs));
                }
            }

            return jobs;
        }
        
    }
}
