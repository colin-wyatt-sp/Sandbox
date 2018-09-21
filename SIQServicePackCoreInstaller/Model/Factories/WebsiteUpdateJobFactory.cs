using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.DataTypes;
using SIQServicePackCoreInstaller.Model.Jobs;

namespace SIQServicePackCoreInstaller.Model.Factories {

    public class WebsiteUpdateJobFactory : IUpdateJobFactory {

        private readonly string _servicePackLocation;

        public WebsiteUpdateJobFactory(string servicePackLocation) {
            this._servicePackLocation = servicePackLocation;
        }

        public string Name => "Website";

        public IEnumerable<IUpdateJob> getJobs() {

            string[] jsonFiles = Directory.GetFiles(_servicePackLocation, "*website.json", SearchOption.AllDirectories);

            if (jsonFiles.Length == 0) {
                return new List<IUpdateJob>();
            }

            Logger.logToFile("Found the following website config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            List<WebsiteUpdateJobInfo> websiteJobs = new List<WebsiteUpdateJobInfo>();
            ServerManager server = new ServerManager();
            try {
                SiteCollection sites = server.Sites;
                foreach (var jsonFile in jsonFiles) {
                    JObject jsonObject = (JObject) JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
                    var websiteName = jsonObject["websiteName"].Value<string>();
                    Logger.logToFile("Searching for website with name: " + websiteName);
                    Site site;
                    string websiteLocation = getWebAppLocation(websiteName, sites, out site);
                    if (string.IsNullOrWhiteSpace(websiteLocation)) {
                        Logger.log("INFO: Unable to find installed website matching name: " + websiteName + ".  Continuing.");
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
                            DirectoryWithFileUpdates = new FileInfo(jsonFile).Directory.FullName,
                            FileExcludeList = new[] { "website.json" },
                            Name = "WebApp " + websiteName
                        };
                    }
                    else {
                        Logger.log(
                            "ERROR: Logic error in ServicePack installer. Two apps with same name in one website detected.");
                    }
                }
            }
            finally {
                server.Dispose();
            }
            return websiteJobs.Select(x => new WebsiteUpdateJob(x));
        }

        private string getWebAppLocation(string websiteName, SiteCollection sites, out Site containingSite) {

            foreach (var site in sites) {
                Logger.logToFile("Site: " + site.Name);
                foreach (Application app in site.Applications) {
                    Logger.logToFile("  App:  poolName: " + app.ApplicationPoolName + ", path: " + app.Path);

                    var appName = app.Path.Trim('/');
                    if (appName != websiteName) continue;

                    foreach (VirtualDirectory virtualDir in app.VirtualDirectories)
                    {
                        Logger.logToFile("     VirtualDir: physPath: " + virtualDir.PhysicalPath);
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
