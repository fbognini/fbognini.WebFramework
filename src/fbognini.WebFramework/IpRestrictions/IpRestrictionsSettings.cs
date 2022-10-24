using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.IpRestrictions
{
    public class IpRestrictionsSettings
    {
        public string WhitelistText { get; set; }
        public List<string> Whitelist { get; set; }
        public List<string> WhitelistIps => MergeList(WhitelistText, Whitelist);

        public string BlacklistText { get; set; }
        public List<string> Blacklist { get; set; }
        public List<string> BlacklistIps => MergeList(BlacklistText, Blacklist);

        private static List<string> MergeList(string list, List<string> lists)
        {
            var all = new List<string>();
            if (!string.IsNullOrWhiteSpace(list))
            {
                all.AddRange(list.Split(new char[] { ',', ';', ' ' }));
            }

            if (lists != null)
            {
                all.AddRange(lists);
            }

            return all;
        }
    }
}
