using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.Jobs;
using SIQServicePackCoreInstaller.Model.Utility;

namespace SIQServicePackCoreInstaller.Model.Factories  {

    public class ServiceUpdateJobFactory : IUpdateJobFactory {

        private readonly string _servicePackLocation;

        public ServiceUpdateJobFactory(string servicePackLocation) {
            this._servicePackLocation = servicePackLocation;
        }

        public string Name => "Service";

        public IEnumerable<IUpdateJob> getJobs() {

            string[] jsonFiles = Directory.GetFiles(_servicePackLocation, "*service.json", SearchOption.AllDirectories);

            if (jsonFiles.Length == 0) {
                return new List<IUpdateJob>();
            }
            Logger.logToFile("Found the following service config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            var jobs = new List<IUpdateJob>();
            ServiceController[] services = ServiceController.GetServices();
            foreach (var jsonFile in jsonFiles)
            {
                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));

                var serviceName = jsonObject["serviceName"].Value<string>();
                Logger.logToFile("Searching for service with name: " + serviceName);
                if (services.All(x => !Regex.Match(x.ServiceName, serviceName).Success))
                {
                    //// TODO: remove - this is for dev testing only
                    //jobs.AddRange(new XmlUpdateJobFactory(new FileInfo(jsonFile).Directory.FullName, jsonFile).getJobs());
                    Logger.log("WARN: Unable to find installed service matching name: " + serviceName + ".  Continuing.");
                    continue;
                }

                var serviceControllers = services.Where(x => Regex.Match(x.ServiceName, serviceName).Success).ToList();
                foreach (var serviceController in serviceControllers) {

                    DirectoryInfo servicePatchDirectory = new FileInfo(jsonFile).Directory;
                    var auxiallaryJobs =
                        new XmlUpdateJobFactory(
                            new FileInfo(RegistryUtility.getServicePath(serviceController.ServiceName)).Directory
                                .FullName, jsonFile).getJobs();

                    jobs.Add(new ServiceUpdateJob(serviceController, servicePatchDirectory, auxiallaryJobs));
                }
            }

            return jobs;
        }
        
    }
}
