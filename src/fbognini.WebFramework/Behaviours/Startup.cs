using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace fbognini.WebFramework.Behaviours
{
    public static class Startup
    {
        [Obsolete("Please use AddRequestLogger() in AddMediatR()", error: true)]
        public static IServiceCollection AddRequestLogger(this IServiceCollection services) => services.AddTransient(typeof(IRequestPreProcessor<>), typeof(RequestLogger<>));
        public static MediatRServiceConfiguration AddRequestLogger(this MediatRServiceConfiguration configuration) => configuration.AddOpenRequestPreProcessor(typeof(RequestLogger<>));

        [Obsolete("Please use AddRequestPerformanceBehaviour() in AddMediatR()", error: true)]
        public static IServiceCollection AddRequestPerformanceBehaviour(this IServiceCollection services) => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPerformanceBehaviour<,>));
        public static MediatRServiceConfiguration AddRequestPerformanceBehaviour(this MediatRServiceConfiguration configuration) => configuration.AddOpenBehavior(typeof(RequestPerformanceBehaviour<,>));

        [Obsolete("Please use AddRequestValidationBehavior() in AddMediatR()", error: true)]
        public static IServiceCollection AddRequestValidationBehavior(this IServiceCollection services) => services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));
        public static MediatRServiceConfiguration AddRequestValidationBehavior(this MediatRServiceConfiguration configuration) => configuration.AddOpenBehavior(typeof(RequestValidationBehavior<,>));

        [Obsolete("Please use AddIHttpRequestValidationBehavior() in AddMediatR()", error: true)]
        public static IServiceCollection AddIHttpRequestValidationBehavior(this IServiceCollection services)
        {
            SetValidatorOptions();
            return services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IHttpRequestValidationBehavior<,>));
        }

        public static MediatRServiceConfiguration AddIHttpRequestValidationBehavior(this MediatRServiceConfiguration configuration)
        {
            SetValidatorOptions();
            return configuration.AddOpenBehavior(typeof(IHttpRequestValidationBehavior<,>));
        }

        private static void SetValidatorOptions()
        {
            ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
            ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;
            ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
            {
                if (member != null)
                {
                    return member.Name;
                }

                return null;
            };
        }
    }
}
