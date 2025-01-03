using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fbognini.WebFramework.Logging
{
    public static class Startup
    {


        public static LoggerConfiguration WriteWebRequestToSqlServer(this LoggerConfiguration logger, IServiceProvider serviceProvider, Dictionary<string, SqlColumn>? additionalColumns = null)
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

            additionalColumns ??= new();

            var notValidColumn = additionalColumns.Keys.FirstOrDefault(x => settings.AdditionalParameters.All(ap => x != ap.PropertyName));
            if (notValidColumn is not null)
            {
                throw new ArgumentException($"You need to specify {notValidColumn} as additional parameter before using it as additional column");
            }

            return logger.WriteWebRequestToSqlServer(sqlOptions.ConnectionString, sqlOptions.TableName, sqlOptions.SchemaName, additionalColumns.Values);
        }

        public static LoggerConfiguration WriteWebRequestToSqlServer(this LoggerConfiguration logger, string connectionString, string tableName, string? schemaName, IEnumerable<SqlColumn>? additionalColumns = null)
        {
            var requestColumns = new List<SqlColumn>()
            {
                new ("RequestId", System.Data.SqlDbType.NVarChar, false, 50),
                new ("Schema", System.Data.SqlDbType.NVarChar, false, 10),
                new ("Host", System.Data.SqlDbType.NVarChar, false, 200),
                new ("Path", System.Data.SqlDbType.NVarChar, false, 200),
                new ("Area", System.Data.SqlDbType.NVarChar, true, 50),
                new ("Controller", System.Data.SqlDbType.NVarChar, true, 50),
                new ("Action", System.Data.SqlDbType.NVarChar, true, 50),
                new ("Query", System.Data.SqlDbType.NVarChar, false),
                new ("Method", System.Data.SqlDbType.NVarChar, false, 10),
                new ("RequestContentType", System.Data.SqlDbType.NVarChar, true, 200),
                new ("RequestContentLength", System.Data.SqlDbType.BigInt, true),
                new ("RequestDate", System.Data.SqlDbType.DateTime2, false),
                new ("Request", System.Data.SqlDbType.NVarChar, false, -1),
                new ("ResponseContentType", System.Data.SqlDbType.NVarChar, true, 50),
                new ("ResponseContentLength", System.Data.SqlDbType.BigInt, true),
                new ("ResponseDate", System.Data.SqlDbType.DateTime2, false),
                new ("Response", System.Data.SqlDbType.NVarChar, true, -1),
                new ("Model", System.Data.SqlDbType.NVarChar, true, -1),
                new ("ViewData", System.Data.SqlDbType.NVarChar, true, -1),
                new ("TempData", System.Data.SqlDbType.NVarChar, true, -1),
                new ("InvalidModelState", System.Data.SqlDbType.NVarChar, true, -1),
                new ("RedirectTo", System.Data.SqlDbType.NVarChar, true, -1),
                new ("ElapsedMilliseconds", System.Data.SqlDbType.Float, false),
                new ("StatusCode", System.Data.SqlDbType.Int, false),
                new ("Ip", System.Data.SqlDbType.NVarChar, false, 20),
                new ("Origin", System.Data.SqlDbType.NVarChar, false, 400),
                new ("UserAgent", System.Data.SqlDbType.NVarChar, false, 1000),
                new ("UserId", System.Data.SqlDbType.NVarChar, true, 100)
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
                    .Logger(lc => lc.Filter.ByIncludingOnly(Matching.FromSource(typeof(RequestResponseLoggingMiddleware).FullName!))
                                    .Filter.ByIncludingOnly(Matching.WithProperty(RequestResponseLoggingMiddleware.ApiLoggingProperty))
                                    .Filter.ByIncludingOnly(Matching.WithProperty("Response"))
                        .WriteTo
                            .MSSqlServer(
                                connectionString,
                                sinkOptions,
                                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                                columnOptions: columnOptions
                            )
                        );

            return logger;
        }


        public static OptionsBuilder<RequestLoggingSettings> WithMSSqlServer(this OptionsBuilder<RequestLoggingSettings> optionsBuilder)
        {
            optionsBuilder.Services.AddHostedService<RequestLoggingSqlManageRetentionWorker>();
            return optionsBuilder;
        }
    }
}
