using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace fbognini.WebFramework.Logging
{
    public static class Startup
    {
        public static IServiceCollection AddSerilogSelfLogging(this IServiceCollection services)
        {
            string todayFilePath = $@"logs/self/{DateTime.Today:yyyyMMdd}.log";
            Directory.CreateDirectory(Path.GetDirectoryName(todayFilePath)!);

            var todayFile = File.Exists(todayFilePath)
                ? new StreamWriter(todayFilePath, true)
                : File.CreateText(todayFilePath);


            var serilogDubugEnableMethod = Assembly.Load("Serilog")
                .GetType("Serilog.Debugging.SelfLog")?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Enable" &&
                                     m.GetParameters().Length == 1 &&
                                     m.GetParameters()[0].ParameterType == typeof(TextWriter))
                ?? throw new InvalidOperationException("Serilog is not loaded");

            serilogDubugEnableMethod.Invoke(null, new object[] { TextWriter.Synchronized(todayFile) });

            return services;
        }

        public static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services, Action<RequestLoggingSettings> options)
        {
            return services
                .AddOptions<RequestLoggingSettings>()
                .Configure(options);
        }

        public static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services, IConfiguration configuration, Action<RequestLoggingSettings>? options = null)
        {
            return services.AddRequestLogging(configuration.GetSection(nameof(RequestLoggingSettings)), options);
        }

        public static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services, IConfigurationSection section, Action<RequestLoggingSettings>? options = null)
        {
            var optionsBuilder =
                services.AddOptions<RequestLoggingSettings>()
                        .Bind(section);

            if (options != null)
            {
                optionsBuilder.Configure(options);
            }

            return optionsBuilder;
        }

        public static OptionsBuilder<RequestLoggingSettings> WithAdditionalParameterResolver<TRequestLoggingAdditionalParameterResolver>(this OptionsBuilder<RequestLoggingSettings> optionsBuilder)
            where TRequestLoggingAdditionalParameterResolver : class, IRequestLoggingAdditionalParameterResolver
        {
            optionsBuilder.Services.AddTransient<IRequestLoggingAdditionalParameterResolver, TRequestLoggingAdditionalParameterResolver>();
            return optionsBuilder;
        }

        public static OptionsBuilder<RequestLoggingSettings> IgnoreMvcResponses(this OptionsBuilder<RequestLoggingSettings> optionsBuilder)
        {
            optionsBuilder
                .Configure<IHttpContextAccessor>(
                    (options, http) => {

                        if (http == null || http.HttpContext == null || http.HttpContext.Request == null)
                        {
                            return;
                        }

                        var context = http.HttpContext;

                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            options.LogResponse = true;
                        }
                        else
                        {
                            if (context.Request.RouteValues.ContainsKey("controller"))
                            {
                                options.LogResponse = false;
                            }
                        }
                    });

            return optionsBuilder;
        }


        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            return app;
        }
    }
}
