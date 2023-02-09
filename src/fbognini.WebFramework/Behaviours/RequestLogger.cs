using fbognini.Core.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Behaviours
{
    public class RequestLogger<TRequest> : IRequestPreProcessor<TRequest>
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

        public async Task Process(TRequest request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Request: {Name} {@UserId} {@Request}",
                typeof(TRequest).Name, currentUserService.UserName, request);
        }
    }
}
