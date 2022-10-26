using fbognini.Core.Interfaces;
using fbognini.WebFramework.Filters;
using fbognini.WebFramework.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Context;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace fbognini.WebFramework.Middlewares
{
    internal class RequestResponseLoggingMiddleware
    {
        public const string ApiLoggingProperty = "ApiLogging";

        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;
        private readonly RequestDelegate next;
        private readonly bool LogResponse;
        private readonly IEnumerable<RequestAdditionalParameter> AdditionalParameters;


        public RequestResponseLoggingMiddleware(RequestDelegate next, RequestLoggingSettings settings)
        {
            this.recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            this.next = next; 
            this.AdditionalParameters = settings.AdditionalParameters;
            this.LogResponse = settings.SaveResponse;
        }

        public async Task Invoke(HttpContext context)
        {

            var logger = context.RequestServices.GetRequiredService<ILogger<RequestResponseLoggingMiddleware>>();
            var endpoint = context
                .GetEndpoint();

            if (endpoint == null)
            {
                await next(context);
                return;
            }

            var controllerActionDescriptor = endpoint
                .Metadata
                .GetMetadata<ControllerActionDescriptor>();

            if (controllerActionDescriptor == null || string.IsNullOrWhiteSpace(controllerActionDescriptor.ControllerName) || string.IsNullOrWhiteSpace(controllerActionDescriptor.ActionName))
            {
                await next(context);
                return;
            }

            var controller = controllerActionDescriptor.ControllerName;
            var action = controllerActionDescriptor.ActionName;
            var area = controllerActionDescriptor.RouteValues.ContainsKey("area")
                ? controllerActionDescriptor.RouteValues["area"]
                : null;

            var propertys = new Dictionary<string, object>()
            {
                [ApiLoggingProperty] = true
            };

            try
            {
                foreach (var parameter in AdditionalParameters)
                {
                    var value = GetValue();
                    if (value == null && !parameter.SqlColumn.AllowNull)
                    {
                        await next(context);
                        return;
                    }

                    propertys.Add(parameter.Parameter, value);

                    string GetValue()
                    {
                        if (parameter.Type == RequestAdditionalParameterType.Query)
                        {
                            return context.Request.Query.ContainsKey(parameter.Parameter) ? context.Request.Query[parameter.Parameter].ToString() : default;
                        }

                        return context.Request.Headers.ContainsKey(parameter.Parameter) ? context.Request.Headers[parameter.Parameter].ToString() : default;
                    }
                }

                var requestDate = DateTime.UtcNow;
                var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();

                context.Request.EnableBuffering();

                string request = await GetRequest(context);

                context.Request.Body.Position = 0;

                var originalResponseBody = context.Response.Body;

                await using var responseBody = recyclableMemoryStreamManager.GetStream();
                context.Response.Body = responseBody;

                await next(context);

                var responseDate = DateTime.UtcNow;
                var elapsedMilliseconds = (responseDate - requestDate).Milliseconds;

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                try
                {
                    propertys.Add("Schema", context.Request.Scheme);
                    propertys.Add("Host", context.Request.Host.Value);
                    propertys.Add("Path", context.Request.Path.Value);
                    propertys.Add("Area", area);
                    propertys.Add("Controller", controller);
                    propertys.Add("Action", action);
                    propertys.Add("Query", context.Request.QueryString.Value);
                    propertys.Add("Method", context.Request.Method);
                    propertys.Add("RequestContentType", context.Request.ContentType);
                    propertys.Add("RequestDate", requestDate);
                    propertys.Add("Request", request);
                    propertys.Add("Origin", context.Request.Headers["origin"].ToString());
                    propertys.Add("Ip", context.Connection.RemoteIpAddress.ToString());
                    propertys.Add("UserAgent", context.Request.Headers[HeaderNames.UserAgent].ToString());
                    propertys.Add("UserId", currentUserService.UserId);
                    propertys.Add("ResponseContentType", context.Response.ContentType);
                    propertys.Add("ResponseDate", responseDate);
                    if (LogResponse)
                    {
                        propertys.Add("Response", response);
                    }
                    var (model, viewdata, tempdata, redirect) = GetModel(context);
                    propertys.Add("Model", model);
                    propertys.Add("ViewData", viewdata);
                    propertys.Add("TempData", tempdata);
                    propertys.Add("InvalidModelState", GetInvalidModelState(context));
                    propertys.Add("RedirectTo", redirect);
                    propertys.Add("ElapsedMilliseconds", elapsedMilliseconds);
                    propertys.Add("StatusCode", context.Response.StatusCode);

                    using (logger.BeginScope(propertys))
                    {
                        logger.LogInformation("HTTP {method} {path}{querystring} responded {statuscode} in {elapsed} ms", context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value, context.Response.StatusCode, elapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unexpeted error during logging web request {path}{query}", context.Request.Path.Value, context.Request.QueryString.Value);
                }

                await responseBody.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpeted error during logging web request {path}{query}", context.Request.Path.Value, context.Request.QueryString.Value);
                throw;
            }
        }

        private async Task<string> GetRequest(HttpContext context)
        {
            await using var requestStream = recyclableMemoryStreamManager.GetStream();

            if (!context.Request.HasFormContentType)
            {
                await context.Request.Body.CopyToAsync(requestStream);
                return ReadStreamInChunks(requestStream);
            }

            if (context.Request.Form.Count == 0 && !context.Request.Form.Files.Any())
            {
                await context.Request.Body.CopyToAsync(requestStream);
                var request = ReadStreamInChunks(requestStream);

                var dict = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(request));
                return Serialize(dict.AllKeys.ToDictionary(k => k, k => dict[k]));
            }

            return Serialize(context.Request.Form.ToDictionary(k => k.Key, k => k.Value.First()));
        }

        private static (string Model, string ViewData, string TempData, string RedirectTo) GetModel(HttpContext context)
        {
            var key = typeof(Microsoft.AspNetCore.Mvc.IUrlHelper);
            if (context.Items.TryGetValue(key, out var helper) == false)
            {
                return (null, null, null, null);
            }

            var property = helper.GetType().GetProperty("ActionContext");
            if (property == null)
            {
                return (null, null, null, null);
            }

            var viewcontext = property.GetValue(helper) as Microsoft.AspNetCore.Mvc.Rendering.ViewContext;
            if (viewcontext == null)
            {
                return (null, null, null, context.Response.Headers[HeaderNames.Location].ToString());
            }

            var model = viewcontext.ViewData.Model != null ? Serialize(viewcontext.ViewData.Model) : null;
            var viewdata = Serialize(viewcontext.ViewData);
            var tempdata = Serialize(viewcontext.TempData);

            return (model, viewdata, tempdata, null);
        }

        private static string GetInvalidModelState(HttpContext context)
        {
            var feature = context.Features.Get<ModelStateFeature>();
            if (feature == null || feature.ModelState == null || feature.ModelState.IsValid)
            {
                return null;
            }

            var errors = feature.ModelState
                .Where(v => v.Value.Errors.Count > 0)
                .Select(x => new
                {
                    Key = x.Key,
                    Errors = x.Value.Errors.ToList()
                });

            return Serialize(errors);
        }

        private static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;

            stream.Seek(0, SeekOrigin.Begin);

            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);

            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;

            do
            {
                readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);

            return textWriter.ToString();
        }
    
        private static string Serialize(object model)
        {
            return JsonSerializer.Serialize(model, new JsonSerializerOptions()
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            });
        }
    }
}
