using FastIDs.TypeId.Serialization.SystemTextJson;
using fbognini.Core.Exceptions;
using fbognini.WebFramework.JsonConverters;
using LinqKit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Handlers.Problems
{
    public static class ResultExtensions
    {
        public static IResult Exception(this IResultExtensions resultExtensions, Exception _)
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);

            return Results.Problem(new ProblemDetails()
            {
                Status = 500
            });
        }

        public static IResult ExplicitException(this IResultExtensions resultExtensions, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);

            var details = new ProblemDetails()
            {
                Status = 500
            };
#if NET8_0_OR_GREATER
            details.Extensions = new Dictionary<string, object?>();
#endif

            details.Extensions.Add("exception", GetDictionaryFromException(exception));

            return Results.Problem(details);
        }


        public static IResult AppException(this IResultExtensions resultExtensions, AppException exception)
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);

            var details = new ProblemDetails()
            {
                Status = (int)exception.HttpStatusCode,
                Title = exception.Title,
                Detail = exception.Message,
                Type = exception.Type
            };

#if NET8_0_OR_GREATER
            details.Extensions ??= new Dictionary<string, object?>();
#endif

            details.AddExtensions(exception.Extensions);

            var dataAsDictionary = new Dictionary<string, object?>();
            dataAsDictionary.AddExtensions(exception.Data);

            if (exception.AdditionalData != null)
            {
                dataAsDictionary.AddExtensions(exception.AdditionalData);
            }

            details.Extensions.Add("additional", dataAsDictionary);

            return Results.Problem(details);
        }

        private static Dictionary<string, object?> GetDictionaryFromException(Exception exception)
        {
            var dict = new Dictionary<string, object?>()
            {
                ["message"] = exception.Message,
                ["type"] = exception.GetType().FullName
            };

            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                dict.Add("stackTrace", exception.StackTrace);
            }

            if (exception.InnerException != null)
            {
                dict.Add("inner", GetDictionaryFromException(exception.InnerException));
            }

            dict.AddExtensions(exception.Data);

            return dict;
        }

        private static void AddExtensions(this ProblemDetails details, Dictionary<string, object?>? dataAdDictionary)
        {
            details.Extensions.AddExtensions(dataAdDictionary);
        }

        private static void AddExtensions(this IDictionary<string, object?> original, object obj)
        {
            ArgumentNullException.ThrowIfNull(original);

            if (obj is null)
            {
                return;
            }

            var dataAdDictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(obj, JsonSerializerHelper.WebOptions));

            original.AddExtensions(dataAdDictionary);
        }

        private static void AddExtensions(this IDictionary<string, object?> original, Dictionary<string, object?>? dataAdDictionary)
        {
            ArgumentNullException.ThrowIfNull(original);

            if (dataAdDictionary is null)
            {
                return;
            }

            dataAdDictionary.ForEach((pair) =>
            {
                original.Add(pair.Key, pair.Value);
            });
        }
    }
}
