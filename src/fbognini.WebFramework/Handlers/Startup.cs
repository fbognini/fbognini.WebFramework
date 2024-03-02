using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace fbognini.WebFramework.Handlers
{
    public static class Startup
    {
        public static IServiceCollection AddMediatRExceptionHandler(this IServiceCollection services)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
            services.AddTransient(typeof(IRequestExceptionHandler<,,>), typeof(GenericExceptionHandler<,,>));
            return services;
        }
    }
}