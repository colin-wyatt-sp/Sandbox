using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace SIQServicePackCoreInstaller
{
    public class WebsiteUpdateJobInfo
    {
        public Site Site { get; set; }

        public IDictionary<string, DirectoryUpdateJobInfo> ApplicationsDictionary { get; set; }

    }
}
