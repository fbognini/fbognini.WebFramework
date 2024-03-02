using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace fbognini.WebFramework.Npm
{
    public static class Startup
    {
        public static IServiceCollection AddNpmWatch(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHostedService(sp => new NpmWatchHostedService(
                enabled: sp.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                logger: sp.GetRequiredService<ILogger<NpmWatchHostedService>>()));

            return services;
        }
    }
}
