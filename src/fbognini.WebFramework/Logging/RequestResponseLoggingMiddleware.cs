using FastIDs.TypeId.Serialization.SystemTextJson;
using fbognini.Core.Interfaces;
using fbognini.WebFramework.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace fbognini.WebFramework.Logging
{
    internal class RequestResponseLoggingMiddleware
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        }.ConfigureForTypeId();

        public const string ApiLoggingProperty = "ApiLogging";

        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;
        private readonly RequestDelegate next;
        private bool LogRequest { get; set; }
        private bool LogResponse { get; set; }
        private IEnumerable<RequestAdditionalParameter> AdditionalParameters { get; set; } = Enumerable.Empty<RequestAdditionalParameter>();


        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
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

            var hub = endpoint.Metadata.OfType<Microsoft.AspNetCore.SignalR.HubMetadata>().LastOrDefault();
            if (hub != null)
            {
                await next(context);
                return;
            }

            if (context.Request.Method.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase)
                || context.Request.Method.Equals("HEAD", StringComparison.InvariantCultureIgnoreCase))
            {
                await next(context);
                return;
            }

            var propertys = new Dictionary<string, object?>()
            {
                [ApiLoggingProperty] = true,
            };

            var (area, controller, action) = GetRouteValues(endpoint);

            propertys.Add("Area", area);
            propertys.Add("Controller", controller);
            propertys.Add("Action", action);

            var requestDate = DateTime.UtcNow;

            try
            {
                AddAdditionalParameters(AdditionalParameters);

                string request = await ReadRequest(context, ignoreLogging);

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
                propertys.Add("Ip", context.Connection.RemoteIpAddress?.ToString());
                propertys.Add("UserAgent", context.Request.Headers[HeaderNames.UserAgent].ToString());

                using (logger.BeginScope(propertys))
                {
                    logger.LogDebug("HTTP {Method} {Path}{Query} requested", context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpeted error during logging web request {Path}{Query}", context.Request.Path.Value, context.Request.QueryString.Value);
                throw;
            }

            var originalResponseBody = context.Response.Body;

            await using var responseBody = recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            Exception? exception = null;

            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                exception = ex;

                logger.LogWarning(ex, "Unexpected exception catched and rethrowned by logging middleware during pipeline execution");
                throw;
            }
            finally
            {
                var responseDate = DateTime.UtcNow;
                var elapsedMilliseconds = (responseDate - requestDate).Milliseconds;

                try
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    string response = await ReadResponse(context, ignoreLogging);

                    var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
                    propertys.Add("UserId", currentUserService.UserId);

                    AddAdditionalParameters(AdditionalParameters, true);

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
                        logger.LogInformation(exception, "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMilliseconds} ms", context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value, context.Response.StatusCode, elapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unexpeted error during logging web response {Path}{Query}", context.Request.Path.Value, context.Request.QueryString.Value);
                }

                await responseBody.CopyToAsync(originalResponseBody);
            }

            void AddAdditionalParameters(IEnumerable<RequestAdditionalParameter> parameters, bool update = false)
            {
                foreach (var parameter in parameters)
                {
                    var value = GetValue(parameter);

                    if (update == false || propertys.ContainsKey(parameter.PropertyName) == false)
                    {
                        propertys.Add(parameter.PropertyName, value);
                        continue;
                    }

                    if (value != null || value != default)
                    {
                        propertys[parameter.PropertyName] = value!;
                    }

                    object? GetValue(RequestAdditionalParameter parameter) => parameter.Type switch
                    {
                        RequestAdditionalParameterType.Query => context.Request.Query.TryGetValue(parameter.Parameter, out var _value) ? _value : default,
                        RequestAdditionalParameterType.Header => context.Request.Headers.TryGetValue(parameter.Parameter, out var _value) ? _value : default,
                        RequestAdditionalParameterType.Session => parameter.Parameter.Equals("__id__", StringComparison.InvariantCultureIgnoreCase)
                         ? context.Session.Id
                         : context.Session.GetString(parameter.Parameter),
                        RequestAdditionalParameterType.Cookie => context.Request.Cookies.TryGetValue(parameter.Parameter, out var _value) ? _value : default,
                        RequestAdditionalParameterType.Custom => context.RequestServices.GetRequiredService<IRequestLoggingAdditionalParameterResolver>().Resolve(parameter.Parameter).GetAwaiter().GetResult(),
                        _ => throw new ArgumentException($"{parameter.Type} is not a valid value", nameof(parameter))
                    };
                }
            }
        }

        private async Task<string> ReadRequest(HttpContext context, IgnoreRequestLoggingAttribute? ignoreLogging)
        {
            if (ignoreLogging != null && ignoreLogging.IgnoreRequestLogging)
            {
                return "[[ATTR - NOLOG]]";
            }

            if (ignoreLogging == null && LogRequest == false)
            {
                return "[[CONF - NOLOG]]";
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

        private async Task<string> ReadResponse(HttpContext context, IgnoreRequestLoggingAttribute? ignoreLogging)
        {
            if (ignoreLogging != null && ignoreLogging.IgnoreResponseLogging)
            {
                return "[[ATTR - NOLOG]]";
            }

            if (ignoreLogging == null && LogResponse == false)
            {
                return "[[CONF - NOLOG]]";
            }

            var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            return response;
        }

        private static (string? Model, string? ViewData, string? TempData, string? RedirectTo) GetModel(HttpContext context)
        {
            var key = typeof(Microsoft.AspNetCore.Mvc.IUrlHelper);
            if (context.Items.TryGetValue(key, out var helper) == false || helper == null)
            {
                return (null, null, null, null);
            }

            var property = helper.GetType().GetProperty("ActionContext");
            if (property == null)
            {
                return (null, null, null, null);
            }

            var renderContext = property.GetValue(helper);
            if (renderContext is ViewContext viewcontext)
            {
                var model = SerializeModel(viewcontext.ViewData.Model);
                var viewdata = Serialize(viewcontext.ViewData);
                var tempdata = Serialize(viewcontext.TempData);

                return (model, viewdata, tempdata, null);
            }

            if (renderContext is PageContext pageContext)
            {
                var model = SerializeModel(pageContext.ViewData.Model);
                var viewdata = Serialize(pageContext.ViewData);

                return (model, viewdata, null, null);
            }

            return (null, null, null, context.Response.Headers[HeaderNames.Location].ToString());
        }


        private static (string? Area, string? Controller, string? Action) GetRouteValues(Endpoint endpoint)
        {
            var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (controllerActionDescriptor != null)
            {
                var area = controllerActionDescriptor.RouteValues.TryGetValue("area", out string? _area) ? _area : null;
                return (area, controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
            }

            var pageActionDescriptor = endpoint.Metadata.GetMetadata<PageActionDescriptor>();
            if (pageActionDescriptor != null)
            {
                var page = pageActionDescriptor.RouteValues.TryGetValue("page", out string? _page) ? _page : null;
                return (pageActionDescriptor.AreaName, null, page);
            }

            return (null, null, null);
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
                    x.Key,
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

        private static string? SerializeModel(object? model)
        {
            if (model is null)
            {
                return null;
            }

            if (model is not PageModel)
            {
                return Serialize(model);
            }

            // get properties of only derivied class
            var dictionary = model.GetType().GetProperties(
                BindingFlags.DeclaredOnly |
                BindingFlags.Public |
                BindingFlags.Instance).ToDictionary(x => x.Name, x => x.GetValue(model));

            return Serialize(dictionary);
        }

        private static string Serialize(object model)
        {
            return JsonSerializer.Serialize(model, _jsonSerializerOptions);
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
