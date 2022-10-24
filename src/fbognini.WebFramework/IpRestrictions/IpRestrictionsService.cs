using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace fbognini.WebFramework.IpRestrictions
{
    public class IpRestrictionsService : IIpRestrictionsService
    {
        private readonly IpRestrictionsSettings settings;

        public IpRestrictionsService(IOptions<IpRestrictionsSettings> options)
        {
            this.settings = options.Value;
        }

        public bool IsAllowed(string ip) => !IsBlocked(ip);
        public bool IsBlocked(string ip)
        {
            return (!IsValidIp(ip, settings.WhitelistIps) || IsValidIp(ip, settings.BlacklistIps));
        }

        private static bool IsValidIp(string ip, List<string> ips)
        {
            if (ips == null)
                return false;

            if (ips.Contains("*"))
                return true;

            return ips.Contains(ip);
        }
    }
}
