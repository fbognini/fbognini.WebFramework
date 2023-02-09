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

#if NET7_0
namespace fbognini.WebFramework.Handlers
{
    public static class Startup
    {
        public static IServiceCollection AddMediatRExceptionHandler(this IServiceCollection services)
        {
            services.AddMediatRExceptionHandler(typeof(GenericExceptionHandler<,,>));
            return services;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="type">typeof(GenericExceptionHandler<,,>)</param>
        /// <returns></returns>
        public static IServiceCollection AddMediatRExceptionHandler(this IServiceCollection services, Type type)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
            services.AddScoped(typeof(IRequestExceptionHandler<,,>), type);

            return services;
        }
    }
}
#endif