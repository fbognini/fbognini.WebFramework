using Azure;
using fbognini.Core.Exceptions;
using fbognini.WebFramework.Api;
using fbognini.WebFramework.Handlers;
using fbognini.WebFramework.Handlers.Problems;
using FluentValidation;
using LinqKit;
using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#if NET7_0
namespace fbognini.WebFramework.Handlers
{
    public class GenericExceptionHandler<TRequest, TResponse, TException> : RequestExceptionHandler<TRequest, IResult>
        where TRequest : IHttpRequest
        where TResponse : IResult
        where TException : Exception
    {
        private readonly ILogger<GenericExceptionHandler<TRequest, TResponse, TException>> logger;

        public GenericExceptionHandler(ILogger<GenericExceptionHandler<TRequest, TResponse, TException>> logger)
        {
            this.logger = logger;
        }

        protected override void Handle(TRequest request, Exception exception, RequestExceptionHandlerState<IResult> state)
        {
            var result = HandleException(request, exception);

            state.SetHandled(result);
        }

        protected virtual IResult HandleException(TRequest request, Exception exception)
        {
            var propertys = new Dictionary<string, object>()
            {
                ["Request"] = JsonSerializer.Serialize(request)
            };

            using (logger.BeginScope(propertys))
            {
                if (exception is AppException appException)
                {
                    return HandleAppException(appException);
                }

                return HandleSystemException(exception);
            }
        }

        private IResult HandleAppException(AppException exception)
        {
            logger.LogError(exception, "That's strange. An AppException occourred during {RequestName}. Flow shouldn't be managed by exceptions", typeof(TRequest).Name);

            var response = new AppProblemDetails(exception);
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (exception.AdditionalData != null)
            {
                JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(exception.AdditionalData, options)).ForEach((pair) =>
                {
                    response.Extensions.Add(pair.Key, pair.Value);
                });
            }

            return Results.Json<ProblemDetails>(response, options, statusCode: response.Status!.Value);
        }

        private IResult HandleSystemException(Exception exception)
        {
            logger.LogError(exception, "Ops. An unexpected exception occourred during {RequestName}", typeof(TRequest).Name);

            var response = new ProblemDetails()
            {
                Status = 500,
                Title = "InternalServerError",
                Detail = exception.Message
            };
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            return Results.Json<ProblemDetails>(response, options, statusCode: 500);
        }
    }
}
#endif