using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace SIQServicePackCoreInstaller
{
    internal class WebsiteInfo
    {
        public Site Site { get; set; }

        public Application Application { get; set; }
        public VirtualDirectory VirtualDir { get; set; }
    }
}
