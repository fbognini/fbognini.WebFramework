using fbognini.Core.Exceptions;
using fbognini.WebFramework.Handlers.Problems;
using fbognini.WebFramework.JsonConverters;
using FluentValidation;
using LinqKit;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly IWebHostEnvironment env;
        private readonly ILogger<GenericExceptionHandler<TRequest, TResponse, TException>> logger;

        public GenericExceptionHandler(IWebHostEnvironment env, ILogger<GenericExceptionHandler<TRequest, TResponse, TException>> logger)
        {
            this.env = env;
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
                ["Request"] = JsonSerializer.Serialize(request, JsonSerializerHelper.LogOptions)
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

            return Results.Extensions.AppException(exception);
        }

        private IResult HandleSystemException(Exception exception)
        {
            logger.LogError(exception, "Ops. An unexpected exception occourred during {RequestName}", typeof(TRequest).Name);

            if (env.IsDevelopment())
            {
                return Results.Extensions.ExplicitException(exception);
            }

            return Results.Extensions.Exception(exception);
        }
    }
}