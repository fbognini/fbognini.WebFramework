using fbognini.WebFramework.IpRestrictions;
using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
