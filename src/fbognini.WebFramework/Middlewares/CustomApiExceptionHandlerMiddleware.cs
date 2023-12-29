using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using fbognini.Core.Exceptions;
using Microsoft.Extensions.Hosting;
using fbognini.WebFramework.Api;
using System.IO;
using Microsoft.IdentityModel.Tokens;
using fbognini.Core.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using fbognini.WebFramework.Validation;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.Json.Serialization;

namespace fbognini.WebFramework.Middlewares
{
    public static class CustomApiExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomApiExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomApiExceptionHandlerMiddleware>();
        }
    }

    public class CustomApiExceptionHandlerMiddleware
    {
        public static readonly List<Type> HandledException = new()
        {
            typeof(AppException),
            typeof(ValidationException),
            typeof(SecurityTokenExpiredException),
            typeof(UnauthorizedAccessException),
        };

        private readonly RequestDelegate next;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<CustomApiExceptionHandlerMiddleware> logger;

        public CustomApiExceptionHandlerMiddleware(
            RequestDelegate next,
            IWebHostEnvironment env,
            ILogger<CustomApiExceptionHandlerMiddleware> logger)
        {
            this.next = next;
            this.env = env;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            Dictionary<string, string[]>? validations = null;
            string? message = null;
            object? additionalData = null;
            HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

            try
            {
                await next(context);
            }
            catch (AppException exception)
            {
                httpStatusCode = exception.HttpStatusCode;
                additionalData = exception.AdditionalData;
                message = exception.Message;

                await WriteToResponseAsync();
            }
            catch (ValidationException exception)
            {
                httpStatusCode = HttpStatusCode.BadRequest;
                validations = exception.Failures;

                await WriteToResponseAsync();
            }
            catch (SecurityTokenExpiredException exception)
            {
                SetUnAuthorizeResponse(exception);
                await WriteToResponseAsync();
            }
            catch (UnauthorizedAccessException exception)
            {
                SetUnAuthorizeResponse(exception);
                await WriteToResponseAsync();
            }
            catch (Exception exception)
            {
                DefaultExceptionLogging.Log(logger, context, exception);

                if (env.IsDevelopment())
                {
                    SetExceptionMessage(exception);
                }

                await WriteToResponseAsync();
            }

            async Task WriteToResponseAsync()
            {
                if (context.Response.HasStarted)
                {
                    throw new InvalidOperationException("The response has already started, the http status code middleware will not be executed.");
                }

                var result = new ApiResult(false, httpStatusCode, message, validations, additionalData);
                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                context.Response.StatusCode = (int)httpStatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }

            void SetUnAuthorizeResponse(Exception exception)
            {
                httpStatusCode = HttpStatusCode.Unauthorized;

                if (env.IsDevelopment())
                {
                    SetExceptionMessage(exception);
                }
                else
                {
                    message = exception.Message;
                }

            }

            void SetExceptionMessage(Exception exception)
            {
                var dic = new Dictionary<string, string?>
                {
                    ["Exception"] = exception.Message,
                    ["StackTrace"] = exception.StackTrace
                };

                if (exception.InnerException != null)
                {
                    dic.Add("InnerException.Exception", exception.InnerException.Message);
                    dic.Add("InnerException.StackTrace", exception.InnerException.StackTrace);
                }

                if (exception is SecurityTokenExpiredException tokenException)
                {
                    dic.Add("Expires", tokenException.Expires.ToString());
                }

                message = JsonSerializer.Serialize(dic);
            }
        }
    }
}
