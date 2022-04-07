using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;
using System.Linq;

namespace fbognini.WebFramework.Rules
{
    public class RedirectToLowercaseRule : IRule
    {
        private readonly int _statusCode;

        public RedirectToLowercaseRule(int statusCode)
        {
            _statusCode = statusCode;
        }

        public void ApplyRule(RewriteContext context)
        {
            var req = context.HttpContext.Request;

            if (!req.Scheme.Any(char.IsUpper)
                && !req.Host.Value.Any(char.IsUpper)
                && !req.PathBase.Value.Any(char.IsUpper)
                && !req.Path.Value.Any(char.IsUpper))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            var newUrl = UriHelper.BuildAbsolute(req.Scheme.ToLowerInvariant(), new HostString(req.Host.Value.ToLowerInvariant()), req.PathBase.Value.ToLowerInvariant(), req.Path.Value.ToLowerInvariant(), req.QueryString);

            var response = context.HttpContext.Response;
            response.StatusCode = _statusCode;
            response.Headers[HeaderNames.Location] = newUrl;
            context.Result = RuleResult.EndResponse;
            context.Logger.RedirectedToLowercase();
        }
    }
}
