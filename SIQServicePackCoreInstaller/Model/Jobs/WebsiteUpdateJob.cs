using System;
using System.Threading;
using Microsoft.Web.Administration;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.DataTypes;

namespace SIQServicePackCoreInstaller.Model.Jobs {

    public class WebsiteUpdateJob : IUpdateJob {

        private readonly WebsiteUpdateJobInfo _jobInfo;

        public WebsiteUpdateJob(WebsiteUpdateJobInfo jobInfo) {
            this._jobInfo = jobInfo;
        }

        public string Name => _jobInfo.Site.Name;

        public void performUpdate() { 

            Site site = _jobInfo.Site;
            Logger.log("Applying updates for website: " + site.Name);

            bool websiteStopped = stopWebsite(site);

            if (websiteStopped) {
                updateWebApps();
                startWebsite(site);
            }
            else {
                Logger.log($"WARN: website \"{site.Name}\" has not stopped in a timely manner. This website will not be updated.");
            }
        }

        private void updateWebApps() {
            try {
                foreach (string applicationName in _jobInfo.ApplicationsDictionary.Keys) {
                    var directoryUpdateJobInfo = _jobInfo.ApplicationsDictionary[applicationName];
                    updateWebApp(applicationName, directoryUpdateJobInfo);
                }
            }
            catch (Exception e) {
                Logger.log($"ERROR: Problem updating web apps {Name}, ExceptionMsg: {e.Message}");
            }
        }

        private bool stopWebsite(Site site) {

            if (site.State != ObjectState.Stopped) {

                Logger.log("Stopping website " + site.Name + " ...");
                site.Stop();
                Thread.Sleep(3000);
                if (site.State != ObjectState.Stopped) {
                    return false;
                }
            }

            return true;
        }

        private void startWebsite(Site site) {

            Logger.log("Starting website " + site.Name + " ...");
            site.Start();
            Thread.Sleep(3000);
            if (site.State != ObjectState.Started) {
                Logger.log("WARN: Service \"" + site.Name + "\" has been updated, but has not started in a timely manner. This may indicate a problem with the website. Continuing.");
            }
            else {
                Logger.log("Completed updating website: " + site.Name);
            }
        }

        private void updateWebApp(string applicationName, DirectoryUpdateJobInfo directoryUpdateJobInfo) {

            try {
                doUpdateWebApp(directoryUpdateJobInfo);
            }
            catch (Exception e) {
                Logger.log("ERROR applying service pack for website application \"" + applicationName +
                           "\" : " + e.Message);
            }
        }

        private void doUpdateWebApp(DirectoryUpdateJobInfo directoryUpdateJobInfo) {

            new DirectoryUpdateJob(directoryUpdateJobInfo).performUpdate();
        }

        public override string ToString()
        {
            return $"WebsiteUpdateJob {Name}";
        }
    }
}