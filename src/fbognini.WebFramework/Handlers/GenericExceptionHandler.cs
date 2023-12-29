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

namespace fbognini.WebFramework.Handlers
{
    public class GenericExceptionHandler<TRequest, TResponse, TException> : IRequestExceptionHandler<TRequest, IResult, TException>
        where TRequest : IHttpRequest
        where TResponse : IResult
        where TException : Exception
    {
        private readonly ILogger<GenericExceptionHandler<TRequest, TResponse, TException>> logger;

        public GenericExceptionHandler(ILogger<GenericExceptionHandler<TRequest, TResponse, TException>> logger)
        {
            this.logger = logger;
        }

        public Task Handle(TRequest request, TException exception, RequestExceptionHandlerState<IResult> state, CancellationToken cancellationToken)
        {
            var result = HandleException(request, exception); 
            
            state.SetHandled(result);

            return Task.CompletedTask;
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

            if (exception is NotFoundException notFoundException)
            {
                return Results.NotFound(new NotFoundProblemDetails(notFoundException));
            }

            var response = new AppProblemDetails(exception);
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (exception.AdditionalData != null)
            {
                JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(exception.AdditionalData, options)).ForEach((pair) =>
                {
                    response.Extensions.Add(pair.Key, pair.Value);
                });
            }

#if NET6_0
            return Results.Json(response, options, statusCode: response.Status!.Value);
#else
            return Results.Json<ProblemDetails>(response, options, statusCode: response.Status!.Value);
#endif

        }

        private IResult HandleSystemException(Exception exception)
        {
            logger.LogError(exception, "Ops. An unexpected exception occourred during {RequestName}", typeof(TRequest).Name);

            var response = new ProblemDetails()
            {
                Status = 500,
                Title = "InternalServerError"
            };
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

#if NET6_0
            return Results.Json(response, options, statusCode: 500);
#else
            return Results.Json<ProblemDetails>(response, options, statusCode: 500);
#endif

        }
    }
}