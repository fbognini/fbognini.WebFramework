using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.IpRestrictions
{
    internal static class IpRestrictionsHelper
    {
        public static bool ShouldBlockRequest(HttpContext context, ILogger logger)
        {
            var service = context.RequestServices.GetRequiredService<IIpRestrictionsService>();
            var ip = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(ip) || service.IsAllowed(ip))
            {
                return false;
            }

            logger.LogWarning("Request to {Path} has been blocked from {Ip}", context.Request.Path, ip);

            return true;
        }
    }
}
