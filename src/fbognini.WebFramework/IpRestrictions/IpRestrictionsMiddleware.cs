using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace fbognini.WebFramework.IpRestrictions
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
            var block = IpRestrictionsHelper.ShouldBlockRequest(context, logger);
            if (!block)
            {
                await next(context);
                return;
            }
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        }
    }
}
