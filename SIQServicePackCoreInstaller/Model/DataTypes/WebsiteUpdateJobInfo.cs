using System.Collections.Generic;
using Microsoft.Web.Administration;

namespace SIQServicePackCoreInstaller.Model.DataTypes {

    public class WebsiteUpdateJobInfo {

        public Site Site { get; set; }

        public IDictionary<string, DirectoryUpdateJobInfo> ApplicationsDictionary { get; set; }

    }
}
