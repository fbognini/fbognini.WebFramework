using fbognini.WebFramework.IpRestrictions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Filters
{
    public class IpRestrictionsFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var context = actionContext.HttpContext;

            var service = context.RequestServices.GetRequiredService<IIpRestrictionsService>();

            var ip = context.Connection.RemoteIpAddress.ToString();

            if (service.IsAllowed(ip))
            {
                return;
            }

            var logger = context.RequestServices.GetRequiredService<ILogger<IpRestrictionsFilterAttribute>>();

            logger.LogWarning("Request to {Path} has been blocked from {Ip}", context.Request.Path, ip);
            actionContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
