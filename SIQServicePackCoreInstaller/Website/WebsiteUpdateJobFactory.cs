using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SIQServicePackCoreInstaller
{
    public class WebsiteUpdateJobFactory : IUpdateJobFactory {

        private readonly string servicePackLocation;
        private readonly ProcessUtility processUtility;

        public WebsiteUpdateJobFactory(string servicePackLocation, ProcessUtility processUtility) {
            this.servicePackLocation = servicePackLocation;
            this.processUtility = processUtility;
        }

        public string Name => "Website";

        public IEnumerable<IUpdateJob> GetJobs() {

            string[] jsonFiles = Directory.GetFiles(servicePackLocation, "*website.json", SearchOption.AllDirectories);

            if (jsonFiles.Length == 0) {
                return new List<IUpdateJob>();
            }

            Logger.Log("Found the following website config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            List<WebsiteUpdateJobInfo> websiteJobs = new List<WebsiteUpdateJobInfo>();
            ServerManager server = new ServerManager();
            try {
                SiteCollection sites = server.Sites;
                foreach (var jsonFile in jsonFiles) {
                    JObject jsonObject = (JObject) JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
                    var websiteName = jsonObject["websiteName"].Value<string>();
                    Logger.Log("Searching for website with name: " + websiteName);
                    Site site;
                    string websiteLocation = GetWebAppLocation(websiteName, sites, out site);
                    if (string.IsNullOrWhiteSpace(websiteLocation)) {
                        Logger.Log("Unable to find installed website matching name: " + websiteName + ".  Continuing.");
                        continue;
                    }

                    var job = websiteJobs.FirstOrDefault(x => x.Site == site);
                    if (job == null) {
                        job = new WebsiteUpdateJobInfo {
                            Site = site,
                            ApplicationsDictionary = new Dictionary<string, DirectoryUpdateJobInfo>()
                        };
                        websiteJobs.Add(job);
                    }

                    if (!job.ApplicationsDictionary.ContainsKey(websiteName)) {
                        job.ApplicationsDictionary[websiteName] = new DirectoryUpdateJobInfo {
                            LocationToUpdate = websiteLocation,
                            DirectoryWithFileUpdates = new FileInfo(jsonFile).Directory.FullName
                        };
                    }
                    else {
                        Logger.Log(
                            "ERROR: Logic error in ServicePack installer. Two apps with same name in one website detected.");
                    }
                }
            }
            finally {
                server.Dispose();
            }
            return websiteJobs.Select(x => new UpdateWebsiteJob(x));
        }

        private string GetWebAppLocation(string websiteName, SiteCollection sites, out Site containingSite) {

            foreach (var site in sites) {
                Logger.LogToFile("Site: " + site.Name);
                foreach (Application app in site.Applications) {
                    Logger.LogToFile("  App:  poolName: " + app.ApplicationPoolName + ", path: " + app.Path);

                    var appName = app.Path.Trim('/');
                    if (appName != websiteName) continue;

                    foreach (VirtualDirectory virtualDir in app.VirtualDirectories)
                    {
                        Logger.LogToFile("     VirtualDir: physPath: " + virtualDir.PhysicalPath);
                        if (virtualDir.PhysicalPath.EndsWith(websiteName)) {
                            containingSite = site;
                            return virtualDir.PhysicalPath;
                        }
                    }
                }
            }
            containingSite = null;
            return null;
        }

       
    }
}
