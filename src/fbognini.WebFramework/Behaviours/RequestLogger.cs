using fbognini.Core.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Behaviours
{   
    internal class RequestLogger<TRequest> : IRequestPreProcessor<TRequest> 
        where TRequest : notnull
    {
        private readonly ILogger logger;
        private readonly ICurrentUserService currentUserService;

        public RequestLogger(
            ILogger<TRequest> logger,
            ICurrentUserService currentUserService)
        {
            this.logger = logger;
            this.currentUserService = currentUserService;
        }

        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Request: {Name} {@UserId} {@Request}",
                typeof(TRequest).Name, currentUserService.UserName, request);

            return Task.CompletedTask;
        }
    }
}
