using fbognini.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace fbognini.WebFramework.Middlewares
{
    public static class DefaultExceptionLogging
    {
        public static void Log(ILogger logger, HttpContext context, Exception exception)
        {
            if (exception is ISilentException)
            {
                logger.LogInformation(exception, "A silent error occours. See previous logs");
            }
            else
            {
                logger.LogError(exception, "Unexpected exception {ExceptionMessage} during request {Request}", exception.Message, context.Request.GetEncodedUrl());
            }
        }
    }
}
