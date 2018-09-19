using System.Collections.Generic;

namespace SIQServicePackCoreInstaller.Model.DataTypes {

    public class DirectoryUpdateJobInfo {

        public string Name { get; set; }

        public string LocationToUpdate { get; set; }

        public string DirectoryWithFileUpdates { get; set; }

        public IEnumerable<string> FileExcludeList { get; set; }

    }
}