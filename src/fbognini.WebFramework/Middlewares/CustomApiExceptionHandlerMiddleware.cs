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
using System.Text.Json;
using fbognini.WebFramework.Validation;
using System.Text.Json.Serialization;

namespace fbognini.WebFramework.Middlewares
{
    public static class CustomApiExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomApiExceptionHandler(this IApplicationBuilder builder, Action<CustomApiExceptionHandlerOptions>? configure = null)
        {
            var options = new CustomApiExceptionHandlerOptions();
            configure?.Invoke(options);

            return builder.UseMiddleware<CustomApiExceptionHandlerMiddleware>(options);
        }
    }

    public class CustomApiExceptionHandlerOptions
    {
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
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

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CustomApiExceptionHandlerMiddleware> _logger;
        private readonly CustomApiExceptionHandlerOptions _options;

        public CustomApiExceptionHandlerMiddleware(
            RequestDelegate next,
            IWebHostEnvironment env,
            ILogger<CustomApiExceptionHandlerMiddleware> logger,
            CustomApiExceptionHandlerOptions options)
        {
            _next = next;
            _env = env;
            _logger = logger;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            Dictionary<string, string[]>? validations = null;
            string? message = null;
            object? additionalData = null;
            HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

            try
            {
                await _next(context);
            }
            catch (Exception exception) when (context.WasAbortedByClient(exception))
            {
                _logger.LogDebug("Request {Method} {Path}{Query} was aborted by the client", context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value);
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
                DefaultExceptionLogging.Log(_logger, context, exception);

                if (_env.IsDevelopment())
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
                var json = JsonSerializer.Serialize(result, _options.JsonSerializerOptions);

                context.Response.StatusCode = (int)httpStatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }

            void SetUnAuthorizeResponse(Exception exception)
            {
                httpStatusCode = HttpStatusCode.Unauthorized;

                if (_env.IsDevelopment())
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
