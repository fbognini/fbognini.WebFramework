using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Filters;

// When the form cannot be read, because the client disconnected mid-body or the body is malformed, MVC records the failure in ModelState, skips binding every parameter, query string included, and still runs the action.
// Reading the form again returns the same failed task, so the original exception surfaces and the action never runs on unbound parameters.
public class UnreadableFormFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.HasFormContentType && !context.ModelState.IsValid)
        {
            await request.ReadFormAsync(context.HttpContext.RequestAborted);
        }

        await next();
    }
}
