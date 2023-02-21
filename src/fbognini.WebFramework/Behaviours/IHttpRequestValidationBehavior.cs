using fbognini.WebFramework.Handlers;
using fbognini.WebFramework.Handlers.Problems;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Behaviours
{
    internal class IHttpRequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, IResult>
        where TRequest : IHttpRequest
        where TResponse : IResult
    {
        private readonly IEnumerable<IValidator<TRequest>> validators;

        public IHttpRequestValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            this.validators = validators;
        }

        public Task<IResult> Handle(TRequest request, RequestHandlerDelegate<IResult> next, CancellationToken cancellationToken)
        {
            var context = new ValidationContext<object>(request);

            var validations = validators
                .Select(v => v.ValidateAsync(context))
                .ToArray();

            Task.WaitAll(validations, cancellationToken);

            var failures = validations
                .Select(x => x.Result)
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count == 0)
            {
                return next();
            }

            var problem = new DetailedValidationProblemDetails(failures.First());
            return Task.FromResult(Results.Problem(problem));
        }
    }
}
