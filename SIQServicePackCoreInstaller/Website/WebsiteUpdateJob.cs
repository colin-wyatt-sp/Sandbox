using System;
using System.IO;
using System.Threading;
using Microsoft.Web.Administration;

namespace SIQServicePackCoreInstaller {

    public class WebsiteUpdateJob : IUpdateJob {

        private WebsiteUpdateJobInfo jobInfo;

        public WebsiteUpdateJob(WebsiteUpdateJobInfo jobInfo) {
            this.jobInfo = jobInfo;
        }

        public void PerformUpdate() { 

            Site site = jobInfo.Site;
            try {
                DoPerformUpdate(site);
            }
            catch (Exception e)
            {
                Logger.Log("ERROR applying service pack for website \"" + site.Name + "\" : " + e.Message);
            }
        }

        private void DoPerformUpdate(Site site) {

            Logger.Log("Applying updates for website: " + site.Name);

            bool websiteStopped = StopWebsite(site);

            if (websiteStopped) {
                foreach (string applicationName in jobInfo.ApplicationsDictionary.Keys) {
                    var directoryUpdateJobInfo = jobInfo.ApplicationsDictionary[applicationName];
                    UpdateWebApp(applicationName, directoryUpdateJobInfo);
                }

                StartWebsite(site);
            }
            else {
                Logger.Log($"WARN: website \"{site.Name}\" has not stopped in a timely manner. This website will not be updated.");
            }
        }

        private bool StopWebsite(Site site)
        {

            if (site.State != ObjectState.Stopped)
            {
                Logger.Log("Stopping website " + site.Name + " ...");
                site.Stop();
                Thread.Sleep(3000);
                if (site.State != ObjectState.Stopped)
                {
                    return false;
                }
            }

            return true;
        }

        private void StartWebsite(Site site)
        {
            Logger.Log("Starting website " + site.Name + " ...");
            site.Start();
            Thread.Sleep(3000);
            if (site.State != ObjectState.Started)
            {
                Logger.Log("WARN: Service \"" + site.Name + "\" has been updated, but has not started in a timely manner. This may indicate a problem with the website. Continuing.");
            }
            else
            {
                Logger.Log("Completed updating website: " + site.Name);
            }
        }

        private void UpdateWebApp(string applicationName, DirectoryUpdateJobInfo directoryUpdateJobInfo)
        {
            try {
                DoUpdateWebApp(directoryUpdateJobInfo);
            }
            catch (Exception e)
            {
                Logger.Log("ERROR applying service pack for website application \"" + applicationName +
                           "\" : " + e.Message);
            }
        }

        private void DoUpdateWebApp(DirectoryUpdateJobInfo directoryUpdateJobInfo) {

            Logger.Log("Getting website path...");
            var websiteFolder = directoryUpdateJobInfo.LocationToUpdate;
            var serviceDirectoryBackupPath = websiteFolder + "_BAK-" + Logger.Timestamp;
            var servicePackFolder = directoryUpdateJobInfo.DirectoryWithFileUpdates;

            //Logger.Log("Stop any website processes still running...");
            //processUtility.TryKillRogueProcesses(serviceDirectory);

            Logger.Log("Backing up website folder \"" + websiteFolder + "\" to \"" +
                       new DirectoryInfo(serviceDirectoryBackupPath).Name + "\"");

            var backupDirectoryInfo = Directory.CreateDirectory(serviceDirectoryBackupPath);
            FileUtility.Copy(websiteFolder, backupDirectoryInfo.FullName, null, overwrite: false);

            Logger.Log("Copying service pack files from \"" + servicePackFolder + "\" to \"" +
                       websiteFolder + "\"");
            FileUtility.Copy(servicePackFolder, websiteFolder, new[] {"website.json"}, overwrite: true);
        }
    }
}