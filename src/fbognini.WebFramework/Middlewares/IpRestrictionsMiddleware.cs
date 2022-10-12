using fbognini.WebFramework.IpRestrictions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Middlewares
{
    public class IpRestrictionsMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<IpRestrictionsMiddleware> logger;

        public IpRestrictionsMiddleware(
            RequestDelegate next,
            ILogger<IpRestrictionsMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var service = context.RequestServices.GetRequiredService<IIpRestrictionsService>();
            var ip = context.Connection.RemoteIpAddress.ToString();

            if (service.IsAllowed(ip))
            {
                await next(context);
                return;
            }

            logger.LogWarning("request to {path} has been blocked from {ip}", context.Request.Path, ip);
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        }
    }
}
