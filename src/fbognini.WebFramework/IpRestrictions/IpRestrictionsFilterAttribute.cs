using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fbognini.WebFramework.IpRestrictions
{
    public class IpRestrictionsFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var context = actionContext.HttpContext;
            var logger = context.RequestServices.GetRequiredService<ILogger<IpRestrictionsFilterAttribute>>();

            var block = IpRestrictionsHelper.ShouldBlockRequest(context, logger);
            if (!block)
            {
                return;
            }

            actionContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
