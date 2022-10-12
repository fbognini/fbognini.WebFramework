using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.IpRestrictions
{
    public class IpRestrictionsSettings
    {
        public List<string> Whitelist { get; set; }
        public List<string> Blacklist { get; set; }
    }
}
