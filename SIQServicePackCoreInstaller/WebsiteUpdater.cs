using System;
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
    public class WebsiteUpdater {
        private readonly string servicePackLocation;
        private readonly ProcessUtility processUtility;
        private Logger logger;
        private string timeStamp;

        public WebsiteUpdater(string servicePackLocation, ProcessUtility processUtility, string timeStamp,
            Logger logger) {
            this.servicePackLocation = servicePackLocation;
            this.processUtility = processUtility;
            this.timeStamp = timeStamp;
            this.logger = logger;
        }

        public void Update() {

            string[] jsonFiles =
                Directory.GetFiles(servicePackLocation, "*website.json", SearchOption.AllDirectories);

            if (jsonFiles == null || jsonFiles.Length == 0)
            {
                logger.Log("WARN: " + "Did not find any \"website.json\" files. No websites will be patched.");
                return;
            }
            logger.Log("Found the following website config files: " + string.Join(", ", jsonFiles.Select(x => new FileInfo(x).Directory.Name)));

            
            foreach (var jsonFile in jsonFiles)
            {
                JObject jsonObject = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
                var websiteName = jsonObject["websiteName"].Value<string>();
                logger.Log("Searching for website with name: " + websiteName);
                WebsiteInfo websiteInfo = FindWebsite(websiteName);
                if (websiteInfo == null) {
                    logger.Log("Unable to find installed website matching name: " + websiteName + ".  Continuing.");
                    continue;
                }

                DirectoryInfo servicePackFolder = new FileInfo(jsonFile).Directory;
                TryApply(websiteInfo, servicePackFolder);
            }
        }

        private WebsiteInfo FindWebsite(string websiteName) {

            using (ServerManager server = new ServerManager()) {

                SiteCollection sites = server.Sites;

                foreach (var site in sites) {
                    logger.LogToFile("Site: " + site.Name);
                    foreach (Application app in site.Applications) {
                        logger.LogToFile("  App:  poolName: " + app.ApplicationPoolName + ", path: " + app.Path);

                        var appName = app.Path.Trim('/');
                        if (appName != websiteName) continue;

                        foreach (VirtualDirectory virtualDir in app.VirtualDirectories)
                        {
                            logger.LogToFile("     VirtualDir: physPath: " + virtualDir.PhysicalPath);
                            if (virtualDir.PhysicalPath.EndsWith(websiteName)) {
                                return new WebsiteInfo {
                                    Site = site,
                                    Application = app,
                                    VirtualDir = virtualDir
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void TryApply(WebsiteInfo websiteInfo, DirectoryInfo servicePackFolder) {

            try
            {
                logger.Log("Applying updates for website: " + websiteInfo.Application.Path);
                PerformApply(websiteInfo, servicePackFolder);
            }
            catch (Exception e)
            {
                logger.Log("ERROR applying service pack for service \"" + websiteInfo.Application.Path + "\" : " + e.Message);
            }
        }

        private void PerformApply(WebsiteInfo websiteInfo, DirectoryInfo servicePackFolder) {

            var site = websiteInfo.Site;
            if (site.State != ObjectState.Stopped)
            {
                logger.Log("Stopping website " + site.Name + " ...");
                site.Stop();
                Thread.Sleep(3000);
                if (site.State != ObjectState.Stopped)
                {
                    logger.Log("WARN: website \"" + site.Name +
                        "\" has not stopped in a timely manner. This website will not be patched; skipping.");
                    return;
                }
            }

            logger.Log("Getting website path...");
            var websiteFolder = websiteInfo.VirtualDir.PhysicalPath;
            var serviceDirectoryBackupPath = websiteFolder + "_BAK-" + timeStamp;

            //logger.Log("Stop any website processes still running...");
            //processUtility.TryKillRogueProcesses(serviceDirectory);

            logger.Log("Backing up website folder \"" + websiteFolder + "\" to \"" + new DirectoryInfo(serviceDirectoryBackupPath).Name + "\"");

            var backupDirectoryInfo = Directory.CreateDirectory(serviceDirectoryBackupPath);
            FileUtility.Copy(websiteFolder, backupDirectoryInfo.FullName, null, overwrite: false);

            logger.Log("Copying service pack files from \"" + servicePackFolder.FullName + "\" to \"" + websiteFolder + "\"");
            FileUtility.Copy(servicePackFolder.FullName, websiteFolder, new[] { "website.json" }, overwrite: true);

            logger.Log("Starting website " + site.Name + " ...");
            site.Start();
            Thread.Sleep(3000);
            if (site.State != ObjectState.Started)
            {
                logger.Log("WARN: Service \"" + site.Name + "\" has been updated, but has not started in a timely manner. This may indicate a problem with the website. Continuing.");
            }
            else
            {
                logger.Log("Completed updating website: " + site.Name);
            }
        }
    }
}
