using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace fbognini.WebFramework.Npm
{
    public static class Startup
    {
        public static IServiceCollection AddNpmWatch(this IServiceCollection services, IConfiguration configuration, string path = "Styles:wwwroot/css")
        {
            services.AddHostedService(sp => new NpmWatchHostedService(
                enabled: sp.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                logger: sp.GetRequiredService<ILogger<NpmWatchHostedService>>(),
                path));

            return services;
        }
    }
}
