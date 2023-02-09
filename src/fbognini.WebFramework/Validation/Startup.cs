using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.ComponentModel;
using System.Linq;

namespace fbognini.WebFramework.Validation
{
    public static class Startup
    {
        public static IServiceCollection AddDisplayNameAsFluentValidationResolver(this IServiceCollection services)
        {
            ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
            {
                if (member == null)
                    return null;

                var attribute = member.GetCustomAttributes(typeof(DisplayNameAttribute), false).SingleOrDefault();
                if (attribute == null)
                {
                    return member.Name;
                }

                var localizerFactory = services.BuildServiceProvider().GetService<IStringLocalizerFactory>();
                if (localizerFactory == null)
                    return ((DisplayNameAttribute)attribute).DisplayName;

                var localizer = localizerFactory.Create(type);
                return localizer[((DisplayNameAttribute)attribute).DisplayName];
            };

            return services;
        }
    }
}
