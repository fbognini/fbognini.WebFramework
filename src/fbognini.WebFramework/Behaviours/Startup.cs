using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Behaviours
{
    public static class Startup
    {
        public static IServiceCollection AddRequestLogger(this IServiceCollection services) => services.AddTransient(typeof(IRequestPreProcessor<>), typeof(RequestLogger<>));
        
        public static IServiceCollection AddRequestPerformanceBehaviour(this IServiceCollection services) => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPerformanceBehaviour<,>));
        public static IServiceCollection AddRequestValidationBehavior(this IServiceCollection services) => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));
        public static IServiceCollection AddIHttpRequestValidationBehavior(this IServiceCollection services) 
        {
            ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;
            ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
            {
                if (member != null)
                {
                    return member.Name;
                }

                return null;
            };
            return services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IHttpRequestValidationBehavior<,>));
        }
    }
}
