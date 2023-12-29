using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using fbognini.Core.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http.Extensions;

namespace fbognini.WebFramework.Middlewares
{
    public static class CustomExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
        }
    }

    public static class HttpContextExtensions
    {
        private static readonly RouteData EmptyRouteData = new RouteData();

        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        public static Task WriteModelAsync<TModel>(this HttpContext context, TModel model)
        {
            var result = new ObjectResult(model)
            {
                DeclaredType = typeof(TModel)
            };

            return context.ExecuteResultAsync(result);
        }

        public static Task ExecuteResultAsync<TResult>(this HttpContext context, TResult result)
            where TResult : IActionResult
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var executor = context.RequestServices.GetRequiredService<IActionResultExecutor<TResult>>();

            var routeData = context.GetRouteData() ?? EmptyRouteData;
            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            return executor.ExecuteAsync(actionContext, result);
        }
    }

    public class CustomExceptionHandlerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<CustomExceptionHandlerMiddleware> logger;

        public CustomExceptionHandlerMiddleware(RequestDelegate next,
            IWebHostEnvironment env,
            ILogger<CustomExceptionHandlerMiddleware> logger)
        {
            this.next = next;
            this.env = env;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is not TimeoutException timeoutException)
            {
                logger.LogInformation("Task has been cancelled during request {Request}", context.Request.GetEncodedUrl());
            }
            catch (NotFoundException exception)
            {
                var notFoundView = new ViewResult()
                {
                    ViewName = "ErrorNotFound",
                };

                context.Response.StatusCode = (int)exception.HttpStatusCode;
                await context.ExecuteResultAsync(notFoundView);
            }
            catch (Exception exception)
            {
                DefaultExceptionLogging.Log(logger, context, exception);

                var exceptionView = new ViewResult()
                {
                    ViewName = "ErrorException",
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                };

                exceptionView.ViewData.Add("ExceptionPath", context.Request.Path);
                exceptionView.ViewData.Add("ExceptionMessage", exception.Message);
                exceptionView.ViewData.Add("StackTrace", exception.StackTrace);

                await context.ExecuteResultAsync(exceptionView);
            }
        }
    }
}
