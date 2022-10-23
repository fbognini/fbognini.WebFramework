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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace fbognini.WebFramework.Middlewares
{
    internal class RequestResponseLoggingMiddleware
    {
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;
        private readonly RequestDelegate next;
        private readonly IEnumerable<RequestAdditionalParameter> AdditionalParameters;

        public RequestResponseLoggingMiddleware(RequestDelegate next, IEnumerable<RequestAdditionalParameter> additionalParameters = null)
        {
            recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            this.next = next;
            this.AdditionalParameters = additionalParameters ?? new List<RequestAdditionalParameter>();
        }

        public async Task Invoke(HttpContext context)
        {
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

            var controller = controllerActionDescriptor.ControllerName;
            var action = controllerActionDescriptor.ActionName;

            if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
            {
                await next(context);
                return;
            }

            var propertys = new Dictionary<string, object>();


            var logger = context.RequestServices.GetRequiredService<ILogger<RequestResponseLoggingMiddleware>>();

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

            propertys.Add("Schema", context.Request.Scheme);
            propertys.Add("Host", context.Request.Host.Value);
            propertys.Add("Path", context.Request.Path.Value);
            propertys.Add("Controller", controller);
            propertys.Add("Action", action);
            propertys.Add("Query", context.Request.QueryString.Value);
            propertys.Add("Method", context.Request.Method);
            propertys.Add("ContentType", context.Request.ContentType);
            propertys.Add("RequestDate", requestDate);
            propertys.Add("Request", request);
            propertys.Add("Origin", context.Request.Headers["origin"].ToString());
            propertys.Add("Ip", context.Connection.RemoteIpAddress.ToString());
            propertys.Add("UserAgent", context.Request.Headers[HeaderNames.UserAgent].ToString());
            propertys.Add("UserId", currentUserService.UserId);
            propertys.Add("ResponseDate", responseDate);
            propertys.Add("Response", response);
            propertys.Add("ElapsedMilliseconds", elapsedMilliseconds);
            propertys.Add("StatusCode", context.Response.StatusCode);

            using (logger.BeginScope(propertys))
            {
                logger.LogInformation("HTTP {method} {path}{querystring} responded {statuscode} in {elapsed} ms", context.Request.Method, context.Request.Path.Value, context.Request.QueryString.Value, context.Response.StatusCode, elapsedMilliseconds);
            }

            await responseBody.CopyToAsync(originalResponseBody);
        }

        private async Task<string> GetRequest(HttpContext context)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = false,
            };

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
                return JsonSerializer.Serialize(dict.AllKeys.ToDictionary(k => k, k => dict[k]), options);
            }

            return JsonSerializer.Serialize(context.Request.Form.ToDictionary(k => k.Key, k => k.Value.First()), options);
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
    }
}
