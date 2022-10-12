using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace fbognini.WebFramework.IpRestrictions
{
    public static class Startup
    {
        public static IServiceCollection AddIpRestrictions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<IpRestrictionsSettings>(configuration.GetSection(nameof(IpRestrictionsSettings)));
            services.AddScoped<IIpRestrictionsService, IpRestrictionsService>();

            return services;
        }

        public static IApplicationBuilder UseIpRestrictionsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpRestrictionsMiddleware>();
        }
    }
}
