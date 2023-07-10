using fbognini.WebFramework.IpRestrictions;
using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

            Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(todayFile));

            return services;
        }

        public static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services, Action<RequestLoggingSettings> options)
        {
            return services
                .AddRequestLogging()
                .Configure(options);
        }

        public static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services, IConfiguration configuration, Action<RequestLoggingSettings> options = null)
        {
            return services.AddRequestLogging(configuration.GetSection(nameof(RequestLoggingSettings)), options);
        }

        public static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services, IConfigurationSection section, Action<RequestLoggingSettings> options = null)
        {
            var optionsBuilder =
                services.AddRequestLogging()
                        .Bind(section);

            if (options != null)
            {
                optionsBuilder.Configure(options);
            }

            return optionsBuilder;
        }

        private static OptionsBuilder<RequestLoggingSettings> AddRequestLogging(this IServiceCollection services) 
        {
            services.AddHostedService<RequestLoggingSqlManageRetentionWorker>();
            return services.AddOptions<RequestLoggingSettings>();
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


        public static LoggerConfiguration WriteWebRequestToSqlServer(this LoggerConfiguration logger, IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<IOptions<RequestLoggingSettings>>().Value;

            var sqlOptions = settings.SqlOptions;
            if (sqlOptions == null)
            {
                throw new ArgumentNullException(nameof(settings.SqlOptions));
            }

            if (string.IsNullOrWhiteSpace(sqlOptions.ConnectionString))
            {
                throw new ArgumentNullException(nameof(sqlOptions.ConnectionString));
            }

            if (string.IsNullOrWhiteSpace(sqlOptions.TableName))
            {
                throw new ArgumentNullException(nameof(sqlOptions.TableName));
            }

            sqlOptions.AdditionalColumns ??= new();

            var notValidColumn = sqlOptions.AdditionalColumns.Keys.FirstOrDefault(x => settings.AdditionalParameters.All(ap => x != ap.PropertyName));
            if (notValidColumn is not null)
            {
                throw new ArgumentException($"You need to specify {notValidColumn} as additional parameter before using it as additional column");
            }

            return logger.WriteWebRequestToSqlServer(sqlOptions.ConnectionString, sqlOptions.TableName, sqlOptions.SchemaName, sqlOptions.AdditionalColumns.Values);
        }

        public static LoggerConfiguration WriteWebRequestToSqlServer(this LoggerConfiguration logger, string connectionstring, string tableName, string? schemaName, IEnumerable<SqlColumn>? additionalColumns = null)
        {
            var requestColumns = new List<SqlColumn>()
                {
                    new SqlColumn("RequestId", System.Data.SqlDbType.NVarChar, false, 50),
                    new SqlColumn("Schema", System.Data.SqlDbType.NVarChar, false, 10),
                    new SqlColumn("Host", System.Data.SqlDbType.NVarChar, false, 200),
                    new SqlColumn("Path", System.Data.SqlDbType.NVarChar, false, 200),
                    new SqlColumn("Area", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("Controller", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("Action", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("Query", System.Data.SqlDbType.NVarChar, false),
                    new SqlColumn("Method", System.Data.SqlDbType.NVarChar, false, 10),
                    new SqlColumn("RequestContentType", System.Data.SqlDbType.NVarChar, true, 200),
                    new SqlColumn("RequestContentLength", System.Data.SqlDbType.BigInt, true),
                    new SqlColumn("RequestDate", System.Data.SqlDbType.DateTime2, false),
                    new SqlColumn("Request", System.Data.SqlDbType.NVarChar, false, -1),
                    new SqlColumn("ResponseContentType", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("ResponseContentLength", System.Data.SqlDbType.BigInt, true),
                    new SqlColumn("ResponseDate", System.Data.SqlDbType.DateTime2, false),
                    new SqlColumn("Response", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("Model", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("ViewData", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("TempData", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("InvalidModelState", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("RedirectTo", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("ElapsedMilliseconds", System.Data.SqlDbType.Float, false),
                    new SqlColumn("StatusCode", System.Data.SqlDbType.Int, false),
                    new SqlColumn("Ip", System.Data.SqlDbType.NVarChar, false, 20),
                    new SqlColumn("Origin", System.Data.SqlDbType.NVarChar, false, 400),
                    new SqlColumn("UserAgent", System.Data.SqlDbType.NVarChar, false, 1000),
                    new SqlColumn("UserId", System.Data.SqlDbType.NVarChar, true, 100),
                    
                };

            if (additionalColumns != null)
            {
                requestColumns.AddRange(additionalColumns);
            }

            var sinkOptions = new MSSqlServerSinkOptions
            {
                AutoCreateSqlTable = true,
                TableName = tableName,
                SchemaName = schemaName
            };
            var columnOptions = new ColumnOptions()
            {
                AdditionalColumns = requestColumns,
                DisableTriggers = true
            };

            columnOptions.Store.Remove(StandardColumn.Message);
            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.Store.Remove(StandardColumn.Level);
            columnOptions.Store.Remove(StandardColumn.Properties);

            logger
                .WriteTo
                    .Logger(lc => lc.Filter.ByIncludingOnly(Matching.FromSource(typeof(RequestResponseLoggingMiddleware).FullName))
                                    .Filter.ByIncludingOnly(Matching.WithProperty(RequestResponseLoggingMiddleware.ApiLoggingProperty))
                                    .Filter.ByIncludingOnly(Matching.WithProperty("Response"))
                        .WriteTo
                            .MSSqlServer(
                                connectionstring,
                                sinkOptions,
                                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                                columnOptions: columnOptions
                            )
                        );

            return logger;
        }

        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            return app;
        }
    }
}
