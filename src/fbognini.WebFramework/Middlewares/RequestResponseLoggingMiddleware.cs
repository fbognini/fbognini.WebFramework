﻿using fbognini.Core.Interfaces;
using fbognini.WebFramework.Filters;
using fbognini.WebFramework.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private bool LogRequest { get; set; }
        private bool LogResponse { get; set; }
        private IEnumerable<RequestAdditionalParameter> AdditionalParameters { get; set; }


        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            this.recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            this.next = next; 
        }

        public async Task Invoke(HttpContext context)
        {
            LoadOptions(context);

            var logger = context.RequestServices.GetRequiredService<ILogger<RequestResponseLoggingMiddleware>>();
            var endpoint = context
                .GetEndpoint();

            if (endpoint == null)
            {
                await next(context);
                return;
            }

            var ignoreLogging = endpoint.Metadata.OfType<IgnoreRequestLoggingAttribute>().LastOrDefault();
            if (ignoreLogging != null && ignoreLogging.IgnoreLogging)
            {
                await next(context);
                return;
            }

            var propertys = new Dictionary<string, object>()
            {
                [ApiLoggingProperty] = true,
            };

            var controllerActionDescriptor = endpoint
                .Metadata
                .GetMetadata<ControllerActionDescriptor>();

            if (controllerActionDescriptor != null)
            {
                propertys.Add("Area", controllerActionDescriptor.RouteValues.TryGetValue("area", out string _area) ? _area : null);
                propertys.Add("Controller", controllerActionDescriptor.ControllerName);
                propertys.Add("Action", controllerActionDescriptor.ActionName);
            }

            try
            {
                AddAdditionalParameters(AdditionalParameters);

                var requestDate = DateTime.UtcNow;
                var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();

                string request = await ReadRequest(context, ignoreLogging);

                var originalResponseBody = context.Response.Body;

                await using var responseBody = recyclableMemoryStreamManager.GetStream();
                context.Response.Body = responseBody;

                await next(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);

                var responseDate = DateTime.UtcNow;
                var elapsedMilliseconds = (responseDate - requestDate).Milliseconds;

                string response = await ReadResponse(context, ignoreLogging);

                try
                {
                    AddAdditionalParameters(AdditionalParameters.Where(x => x.Type == RequestAdditionalParameterType.Session), true);

                    // RequestId populated by serilog
                    propertys.Add("Schema", context.Request.Scheme);
                    propertys.Add("Host", context.Request.Host.Value);
                    propertys.Add("Path", context.Request.Path.Value);
                    propertys.Add("Query", context.Request.QueryString.Value);
                    propertys.Add("Method", context.Request.Method);
                    propertys.Add("RequestContentType", context.Request.ContentType);
                    propertys.Add("RequestContentLength", context.Request.ContentLength);
                    propertys.Add("RequestDate", requestDate);
                    propertys.Add("Request", request);
                    propertys.Add("Origin", context.Request.Headers["origin"].ToString());
                    propertys.Add("Ip", context.Connection.RemoteIpAddress.ToString());
                    propertys.Add("UserAgent", context.Request.Headers[HeaderNames.UserAgent].ToString());
                    propertys.Add("UserId", currentUserService.UserId);
                    propertys.Add("ResponseContentType", context.Response.ContentType);
                    propertys.Add("ResponseContentLength", context.Response.ContentLength);
                    propertys.Add("ResponseDate", responseDate);
                    propertys.Add("Response", response);

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
                        logger.LogInformation("HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMilliseconds} ms", context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value, context.Response.StatusCode, elapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unexpeted error during logging web request {Path}{Query}", context.Request.Path.Value, context.Request.QueryString.Value);
                }

                await responseBody.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpeted error during logging web request {Path}{Query}", context.Request.Path.Value, context.Request.QueryString.Value);
                throw;
            }

            void AddAdditionalParameters(IEnumerable<RequestAdditionalParameter> parameters, bool update = false)
            {
                foreach (var parameter in parameters)
                {
                    var value = GetValue(parameter);
                    if (value == null && parameter.SqlColumn.AllowNull == false)
                    {
                        logger.LogWarning("Expected {parameter} in {type} but no value provided", parameter.Parameter, parameter.Type.ToString());
                    }

                    if (update == false || propertys.ContainsKey(parameter.SqlColumn.PropertyName) == false)
                    {
                        propertys.Add(parameter.SqlColumn.PropertyName, value);
                        continue;
                    }
                    
                    if (string.IsNullOrWhiteSpace(value) == false)
                    {
                        propertys[parameter.SqlColumn.PropertyName] = value;
                    }

                    string GetValue(RequestAdditionalParameter parameter) => parameter.Type switch
                    {
                        RequestAdditionalParameterType.Query => context.Request.Query.TryGetValue(parameter.Parameter, out var _value) ? _value : default,
                        RequestAdditionalParameterType.Header => context.Request.Headers.TryGetValue(parameter.Parameter, out var _value) ? _value : default,
                        RequestAdditionalParameterType.Session => parameter.Parameter.Equals("__id__", StringComparison.InvariantCultureIgnoreCase)
                         ? context.Session.Id
                         : context.Session.GetString(parameter.Parameter),
                        RequestAdditionalParameterType.Cookie => context.Request.Cookies.TryGetValue(parameter.Parameter, out var _value) ? _value : default,
                        _ => throw new ArgumentException($"{parameter.Type} is not a valid value", nameof(parameter))
                    };
                }
            }
        }

        private async Task<string> ReadRequest(HttpContext context, IgnoreRequestLoggingAttribute ignoreLogging)
        {
            if (ignoreLogging != null && ignoreLogging.IgnoreRequestLogging)
            {
                return "[[attribute - no log]]";
            }

            if (ignoreLogging == null && LogRequest == false)
            {
                return "[[configuration - no log]]";
            }

            context.Request.EnableBuffering();

            string request = await GetRequest(context);

            context.Request.Body.Position = 0;

            return request;
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

        private async Task<string> ReadResponse(HttpContext context, IgnoreRequestLoggingAttribute ignoreLogging)
        {
            if (ignoreLogging != null && ignoreLogging.IgnoreResponseLogging)
            {
                return "[[attribute - no log]]";
            }

            if (ignoreLogging == null && LogResponse == false)
            {
                return "[[configuration - no log]]";
            }

            var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            return response;
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

        private void LoadOptions(HttpContext context)
        {
            var settings = context.RequestServices.GetRequiredService<IOptionsSnapshot<RequestLoggingSettings>>().Value;
            AdditionalParameters = settings.AdditionalParameters ?? Enumerable.Empty<RequestAdditionalParameter>();
            LogRequest = settings.LogRequest;
            LogResponse = settings.LogResponse;
        }
    }
}
