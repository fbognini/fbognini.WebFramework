using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace fbognini.WebFramework.IpRestrictions
{
    public interface IIpRestrictionsService
    {
        bool IsAllowed(string ip);
        bool IsBlocked(string ip);
    }
}
