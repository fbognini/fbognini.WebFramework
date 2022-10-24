﻿using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

namespace fbognini.WebFramework.Logging
{
    public static class Startup
    {
        public static LoggerConfiguration WriteWebRequestToSqlServer(this LoggerConfiguration logger, string connectionstring, string tableName, string schemaName, IEnumerable<RequestAdditionalParameter> parameters = null)
        {
            var additionalColumns = new List<SqlColumn>()
                {
                    new SqlColumn("Schema", System.Data.SqlDbType.NVarChar, false, 10),
                    new SqlColumn("Host", System.Data.SqlDbType.NVarChar, false, 200),
                    new SqlColumn("Path", System.Data.SqlDbType.NVarChar, false, 200),
                    new SqlColumn("Area", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("Controller", System.Data.SqlDbType.NVarChar, false, 50),
                    new SqlColumn("Action", System.Data.SqlDbType.NVarChar, false, 50),
                    new SqlColumn("Query", System.Data.SqlDbType.NVarChar, false, 400),
                    new SqlColumn("Method", System.Data.SqlDbType.NVarChar, false, 10),
                    new SqlColumn("RequestContentType", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("RequestDate", System.Data.SqlDbType.DateTime2, false),
                    new SqlColumn("Request", System.Data.SqlDbType.NVarChar, false, -1),
                    new SqlColumn("ResponseContentType", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("ResponseDate", System.Data.SqlDbType.DateTime2, false),
                    new SqlColumn("Response", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("ElapsedMilliseconds", System.Data.SqlDbType.Float, false),
                    new SqlColumn("StatusCode", System.Data.SqlDbType.Int, false),
                    new SqlColumn("Ip", System.Data.SqlDbType.NVarChar, false, 20),
                    new SqlColumn("Origin", System.Data.SqlDbType.NVarChar, false, 400),
                    new SqlColumn("UserAgent", System.Data.SqlDbType.NVarChar, false, 1000),
                    new SqlColumn("UserId", System.Data.SqlDbType.NVarChar, true, 100)
                };

            return logger.WriteRequestToSqlServer(additionalColumns, connectionstring, tableName, schemaName, parameters);
        }

        public static LoggerConfiguration WriteMvcRequestToSqlServer(this LoggerConfiguration logger, string connectionstring, string tableName, string schemaName, IEnumerable<RequestAdditionalParameter> parameters = null)
        {
            var additionalColumns = new List<SqlColumn>()
                {
                    new SqlColumn("Schema", System.Data.SqlDbType.NVarChar, false, 10),
                    new SqlColumn("Host", System.Data.SqlDbType.NVarChar, false, 200),
                    new SqlColumn("Path", System.Data.SqlDbType.NVarChar, false, 200),
                    new SqlColumn("Area", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("Controller", System.Data.SqlDbType.NVarChar, false, 50),
                    new SqlColumn("Action", System.Data.SqlDbType.NVarChar, false, 50),
                    new SqlColumn("Query", System.Data.SqlDbType.NVarChar, false, 400),
                    new SqlColumn("Method", System.Data.SqlDbType.NVarChar, false, 10),
                    new SqlColumn("RequestContentType", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("RequestDate", System.Data.SqlDbType.DateTime2, false),
                    new SqlColumn("Request", System.Data.SqlDbType.NVarChar, false, -1),
                    new SqlColumn("ResponseContentType", System.Data.SqlDbType.NVarChar, true, 50),
                    new SqlColumn("ResponseDate", System.Data.SqlDbType.DateTime2, false),
                    new SqlColumn("Response", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("Model", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("ViewData", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("TempData", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("InvalidModelState", System.Data.SqlDbType.NVarChar, true, -1),
                    new SqlColumn("ElapsedMilliseconds", System.Data.SqlDbType.Float, false),
                    new SqlColumn("StatusCode", System.Data.SqlDbType.Int, false),
                    new SqlColumn("Ip", System.Data.SqlDbType.NVarChar, false, 20),
                    new SqlColumn("Origin", System.Data.SqlDbType.NVarChar, false, 400),
                    new SqlColumn("UserAgent", System.Data.SqlDbType.NVarChar, false, 1000),
                    new SqlColumn("UserId", System.Data.SqlDbType.NVarChar, true, 100)
                };

            return logger.WriteRequestToSqlServer(additionalColumns, connectionstring, tableName, schemaName, parameters);
        }

        private static LoggerConfiguration WriteRequestToSqlServer(this LoggerConfiguration logger, List<SqlColumn> additionalColumns, string connectionstring, string tableName, string schemaName, IEnumerable<RequestAdditionalParameter> parameters = null)
        {
            if (parameters != null)
            {
                additionalColumns.AddRange(parameters.Select(x => x.SqlColumn));
            }

            var sinkOptions = new MSSqlServerSinkOptions { AutoCreateSqlTable = true, TableName = tableName, SchemaName = schemaName };

            var columnOptions = new ColumnOptions()
            {
                AdditionalColumns = additionalColumns
            };

            columnOptions.Store.Remove(StandardColumn.Message);
            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.Store.Remove(StandardColumn.Level);
            columnOptions.Store.Remove(StandardColumn.Exception);
            columnOptions.Store.Remove(StandardColumn.Properties);

            logger
                .WriteTo
                    .Logger(lc => lc.Filter.ByIncludingOnly(Matching.FromSource(typeof(RequestResponseLoggingMiddleware).FullName))
                                    .Filter.ByIncludingOnly(Matching.WithProperty(RequestResponseLoggingMiddleware.ApiLoggingProperty))
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

        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app, RequestLoggingSettings settings)
        {
            app.UseMiddleware<RequestResponseLoggingMiddleware>(settings);
            return app;
        }
    }
}