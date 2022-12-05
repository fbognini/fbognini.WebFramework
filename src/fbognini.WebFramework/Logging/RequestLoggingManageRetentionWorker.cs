using Cronos;
using fbognini.WebFramework.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    internal class RequestLoggingManageRetentionWorker : BackgroundService
    {
        private readonly RequestLoggingSettings settings;
        private readonly CronosPeriodicTimer timer;
        private readonly ILogger<RequestLoggingManageRetentionWorker> logger;
        public RequestLoggingManageRetentionWorker(IOptions<RequestLoggingSettings> options, ILogger<RequestLoggingManageRetentionWorker> logger)
        {
            if (options.Value.RetentionOptions is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            settings = options.Value;

            timer = new CronosPeriodicTimer(settings.RetentionOptions.CronExpression, CronFormat.Standard);

            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (settings.RetentionOptions.Days < 1)
            {
                return;
            }

            ValidateSettings();

            if (settings.RetentionOptions.RunOnStartup)
            {
                await DoWork(stoppingToken);
            }

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                await DoWork(stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Manage retentions of {days} for [{schema}].[{table}]", settings.RetentionOptions.Days, settings.SchemaName, settings.TableName);

                ValidateSettings();

                var total = await DeletePreviousRows(
                    settings.TableName,
                    settings.SchemaName,
                    settings.ColumnName,
                    DateTime.Now.AddDays(-settings.RetentionOptions.Days),
                    settings.RetentionOptions.BatchSize,
                    cancellationToken);

                logger.LogInformation("Successfully deleted {rows} rows deleted from [{schema}].[{table}]", total, settings.SchemaName, settings.TableName);
            }
            catch (SqlException ex)
            {
                logger.LogError(ex, "An SqlException occours during DeletePreviousRows from [{schema}].[{table}]", settings.SchemaName, settings.TableName);
            }
        }

        private async Task<long> DeletePreviousRows(string table, string schema, string column, DateTime date, int batch, CancellationToken cancellationToken)
        {
            var sql = @$"
DECLARE @Total INT = 0
DECLARE @sql NVARCHAR(MAX);

SET @sql = '
	DELETE FROM ' + QUOTENAME('{schema}') + '.' + QUOTENAME('{table}') + '
	WHERE ' + QUOTENAME('{column}') + ' < @retentiondate'

exec sp_executesql @sql, 
                N'@retentiondate datetime2(7)', @RetentionDate;

SET @Total = @@rowcount

SELECT @Total
";

            long total = 0, deleted;

            using SqlConnection connection = new(settings.ConnectionString);
            connection.Open();

            do
            {

                SqlCommand command = new()
                {
                    Connection = connection,
                    CommandText = sql,
                    CommandType = CommandType.Text,
                    CommandTimeout = 120,
                };
                command.Parameters.Add(new SqlParameter()
                {
                    ParameterName = "@RetentionDate",
                    SqlDbType = SqlDbType.DateTime2,
                    Direction = ParameterDirection.Input,
                    Value = date
                });

                var watch = new Stopwatch();
                watch.Start();

                deleted = (long)await command.ExecuteScalarAsync(cancellationToken);

                watch.Stop();

                total += deleted;

                logger.LogInformation("{rows} rows deleted from [{schema}].[{table}] in {seconds} seconds", total, settings.SchemaName, settings.TableName, watch.Elapsed.TotalSeconds);

            } while (deleted == batch);


            return total;
        }

        private void ValidateSettings()
        {
            if (settings.RetentionOptions.Days < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.RetentionOptions.Days), settings.RetentionOptions.Days, "Should be greater than zero.");
            }

            if (settings.RetentionOptions.BatchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.RetentionOptions.BatchSize), settings.RetentionOptions.BatchSize, "Should be greater than zero.");
            }
        }
    }
}
