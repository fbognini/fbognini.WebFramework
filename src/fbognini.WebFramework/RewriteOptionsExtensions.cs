using fbognini.WebFramework.Rules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Logging;
using System;

namespace fbognini.WebFramework
{
    internal static class MiddlewareLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _redirectedToLowercase = LoggerMessage.Define(LogLevel.Information, new EventId(1, "RedirectedToLowercase"), "Request redirected to lowercase");

        public static void RedirectedToLowercase(this ILogger logger)
        {
            _redirectedToLowercase(logger, null);
        }
    }

    public static class RewriteOptionsExtensions
    {
        public static RewriteOptions AddRedirectToLowercase(this RewriteOptions options, int statusCode)
        {
            options.Add(new RedirectToLowercaseRule(statusCode));
            return options;
        }

        public static RewriteOptions AddRedirectToLowercase(this RewriteOptions options)
        {
            return AddRedirectToLowercase(options, StatusCodes.Status307TemporaryRedirect);
        }

        public static RewriteOptions AddRedirectToLowercasePermanent(this RewriteOptions options)
        {
            return AddRedirectToLowercase(options, StatusCodes.Status308PermanentRedirect);
        }
    }
}
