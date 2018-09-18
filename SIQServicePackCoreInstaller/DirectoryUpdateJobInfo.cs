using Microsoft.Web.Administration;

namespace SIQServicePackCoreInstaller {
    public class DirectoryUpdateJobInfo {

        public string Name { get; set; }

        public string LocationToUpdate { get; set; }

        public string DirectoryWithFileUpdates { get; set; }

    }
}