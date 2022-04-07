using fbognini.WebFramework.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace fbognini.WebFramework.Middlewares
{
    public class AdditionalParameter
    {
        public AdditionalParameter(string queryParameter, bool isMandatory = false)
        {
            QueryParameter = queryParameter;
            IsMandatory = isMandatory;
        }

        public string QueryParameter { get; set; }
        public bool IsMandatory { get; set; }

        public string Value { get; private set; }
        public void SetValue(string value)
        {
            Value = value;
        }
    }

    public class RequestResponseLoggingMiddleware
    {
        private readonly string connectionString;
        private readonly string schema;
        private readonly List<AdditionalParameter> additionalParameters;
        private readonly RequestDelegate next;
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;

        public RequestResponseLoggingMiddleware(RequestDelegate next, string connectionString, string schema, List<AdditionalParameter> additionalParameters = null)
        {
            this.next = next;
            recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            this.connectionString = connectionString;
            this.schema = schema;
            this.additionalParameters = additionalParameters ?? new List<AdditionalParameter>();
        }

        public async Task Invoke(HttpContext context)
        {
            int id = await LogRequest(context);
            if (id == -1)
            {
                await next(context);
                return;
            }

            context.Request.Body.Position = 0;

            var original = context.Response.Body;

            await using var responseBody = recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            await next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var status = context.Response.StatusCode;
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            SqlCommand command = new SqlCommand(
                $"{schema}.UpdateWebRequest", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@id", id);
            var contentType = context.Response.ContentType ?? string.Empty;
            if (contentType.Contains("application/json"))
            {
                command.Parameters.AddWithValue("@responseBody", text);
            }
            else
            {
                var ModelState = context.Features.Get<ModelStateFeature>()?.ModelState;
                if (ModelState != null && !ModelState.IsValid)
                {
                    status = (int)HttpStatusCode.BadRequest;

                    var errors = ModelState.Values.Where(v => v.Errors.Count > 0)
                           .SelectMany(v => v.Errors)
                           .ToList();

                    command.Parameters.AddWithValue("@responseBody", JsonConvert.SerializeObject(errors));
                }
            }
            command.Parameters.AddWithValue("@statusCode", status);

            command.ExecuteNonQuery();
            connection.Close();

            await responseBody.CopyToAsync(original);
        }

        private async Task<int> LogRequest(HttpContext context)
        {
            if (!context.Request.RouteValues.ContainsKey("controller")) // resource as image 
                return -1;

            context.Request.EnableBuffering();

            await using var requestStream = recyclableMemoryStreamManager.GetStream();

            var queryString = context.Request.QueryString.Value;

            foreach (var additionalParameter in additionalParameters)
            {
                var value = context.Request.Query.ContainsKey(additionalParameter.QueryParameter) ? context.Request.Query[additionalParameter.QueryParameter].ToString() : default(string);
                if (value == null && additionalParameter.IsMandatory)
                    return -1;

                additionalParameter.SetValue(value);
            }

            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            var userAgent = context.Request.Headers[HeaderNames.UserAgent].ToString();

            string controller = context.Request.RouteValues["controller"].ToString();
            string action = context.Request.RouteValues["action"].ToString();

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            SqlCommand command = new SqlCommand(
                    $"{schema}.InsertWebRequest", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };


            var contentType = context.Request.ContentType ?? string.Empty;

            string request;
            if (!context.Request.HasFormContentType)
            {
                await context.Request.Body.CopyToAsync(requestStream);
                request = ReadStreamInChunks(requestStream);
            }
            else if (context.Request.Form.Count() == 0 && !context.Request.Form.Files.Any())
            {
                await context.Request.Body.CopyToAsync(requestStream);
                request = ReadStreamInChunks(requestStream);

                var dict = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(request));
                request = JsonConvert.SerializeObject(
                    dict.AllKeys.ToDictionary(k => k, k => dict[k])
                );
            }
            else
            {
                request = JsonConvert.SerializeObject(context.Request.Form.ToDictionary(k => k.Key, k => k.Value.First()));
            }

            command.Parameters.AddWithValue("@schema", context.Request.Scheme);
            command.Parameters.AddWithValue("@host", context.Request.Host.Value);
            command.Parameters.AddWithValue("@path", context.Request.Path.Value);
            command.Parameters.AddWithValue("@controller", controller);
            command.Parameters.AddWithValue("@action", action);
            command.Parameters.AddWithValue("@query", queryString);
            command.Parameters.AddWithValue("@method", context.Request.Method);
            command.Parameters.AddWithValue("@contentType", contentType);
            command.Parameters.AddWithValue("@request", request);
            command.Parameters.AddWithValue("@ip", context.Connection.RemoteIpAddress.ToString());
            command.Parameters.AddWithValue("@authorization", authorization ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@user", context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User?.Identity?.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@userAgent", userAgent);
            foreach (var additionalParameter in additionalParameters)
            {
                command.Parameters.AddWithValue("@" + additionalParameter.QueryParameter, additionalParameter.Value ?? (object)DBNull.Value);
            }

            int id = -1;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    id = (int)reader[0];
                    break;
                }
            }

            connection.Close();
            return id;
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
