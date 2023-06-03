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
    internal class RequestLoggingSqlManageRetentionWorker : BackgroundService
    {
        private readonly CronosPeriodicTimer? timer;
        private readonly RequestLoggingSettings settings;
        private readonly ILogger<RequestLoggingSqlManageRetentionWorker> logger;
        public RequestLoggingSqlManageRetentionWorker(IOptions<RequestLoggingSettings> options, ILogger<RequestLoggingSqlManageRetentionWorker> logger)
        {
            settings = options.Value;
            timer = settings.RetentionOptions is not null
                ? new CronosPeriodicTimer(settings.RetentionOptions.CronExpression, CronFormat.Standard)
                : null;

            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (timer == null || settings.SqlOptions == null || settings.RetentionOptions == null)
            {
                return;
            }

            ValidateSettings();

            if (settings.RetentionOptions!.RunOnStartup)
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
                logger.LogInformation("Manage retentions of {days} for [{schema}].[{table}]", settings.RetentionOptions!.Days, settings.SqlOptions!.SchemaName, settings.SqlOptions!.TableName);

                ValidateSettings();

                var total = await DeletePreviousRows(
                    settings.SqlOptions!.TableName,
                    settings.SqlOptions!.SchemaName,
                    settings.SqlOptions!.ColumnName,
                    DateTime.Now.AddDays(-settings.RetentionOptions.Days),
                    settings.RetentionOptions.BatchSize,
                    cancellationToken);

                logger.LogInformation("Successfully deleted {rows} rows deleted from [{schema}].[{table}]", total, settings.SqlOptions!.SchemaName, settings.SqlOptions!.TableName);
            }
            catch (SqlException ex)
            {
                logger.LogError(ex, "An SqlException occours during DeletePreviousRows from [{schema}].[{table}]", settings.SqlOptions!.SchemaName, settings.SqlOptions!.TableName);
            }
        }

        private async Task<long> DeletePreviousRows(string table, string schema, string column, DateTime date, int batch, CancellationToken cancellationToken)
        {
            var sql = @$"
DECLARE @Total BIGINT = 0
DECLARE @sql NVARCHAR(MAX);

SET @sql = '
	DELETE TOP({batch}) FROM ' + QUOTENAME('{schema}') + '.' + QUOTENAME('{table}') + '
	WHERE ' + QUOTENAME('{column}') + ' < @retentiondate'

exec sp_executesql @sql, 
                N'@retentiondate datetime2(7)', @RetentionDate;

SET @Total = @@rowcount

SELECT @Total
";

            long total = 0, deleted;

            using SqlConnection connection = new(settings.SqlOptions!.ConnectionString);
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

                logger.LogInformation("{rows} rows deleted from [{schema}].[{table}] in {seconds} seconds", total, settings.SqlOptions!.SchemaName, settings.SqlOptions!.TableName, watch.Elapsed.TotalSeconds);

            } while (deleted != 0);


            return total;
        }

        private void ValidateSettings()
        {
            if (settings.RetentionOptions!.Days < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.RetentionOptions.Days), settings.RetentionOptions.Days, "Should be greater than zero.");
            }

            if (settings.RetentionOptions!.BatchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.RetentionOptions.BatchSize), settings.RetentionOptions.BatchSize, "Should be greater than zero.");
            }
        }
    }
}
