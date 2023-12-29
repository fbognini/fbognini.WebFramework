using fbognini.WebFramework.Behaviours;
using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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