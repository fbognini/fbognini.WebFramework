using Cronos;
using fbognini.WebFramework.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    internal class RequestLoggingSqlManageRetentionWorker : BackgroundService
    {
        private readonly SqlOptions? sqlOptions;

        private readonly ILogger<RequestLoggingSqlManageRetentionWorker> logger;

        public RequestLoggingSqlManageRetentionWorker(IOptions<RequestLoggingSettings> options, ILogger<RequestLoggingSqlManageRetentionWorker> logger)
        {
            sqlOptions = options.Value.SqlOptions;

            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (sqlOptions?.Retention is null)
            {
                return;
            }

            ValidateSettings();

            if (sqlOptions!.Retention!.RunOnStartup)
            {
                await DoWork(stoppingToken);
            }

            var timer = new CronosPeriodicTimer(sqlOptions!.Retention.CronExpression, CronFormat.Standard);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                await DoWork(stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Manage retentions of {days} for [{schema}].[{table}]", sqlOptions!.Retention!.Days, sqlOptions!.SchemaName, sqlOptions!.TableName);

                ValidateSettings();

                var total = await DeletePreviousRows(
                    sqlOptions!.TableName,
                    sqlOptions!.SchemaName,
                    sqlOptions!.ColumnName,
                    DateTime.Now.AddDays(-sqlOptions!.Retention.Days),
                    sqlOptions!.Retention.BatchSize,
                    cancellationToken);

                logger.LogInformation("Successfully deleted {rows} rows deleted from [{schema}].[{table}]", total, sqlOptions!.SchemaName, sqlOptions!.TableName);
            }
            catch (SqlException ex)
            {
                logger.LogError(ex, "An SqlException occours during DeletePreviousRows from [{schema}].[{table}]", sqlOptions!.SchemaName, sqlOptions!.TableName);
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

            using SqlConnection connection = new(sqlOptions!.ConnectionString);
            connection.Open();

            do
            {

                SqlCommand command = new()
                {
                    Connection = connection,
                    CommandText = sql,
                    CommandType = CommandType.Text,
                    CommandTimeout = sqlOptions!.Retention!.Timeout,
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

                logger.LogInformation("{rows} rows deleted from [{schema}].[{table}] in {seconds} seconds", total, sqlOptions!.SchemaName, sqlOptions!.TableName, watch.Elapsed.TotalSeconds);

            } while (deleted != 0);


            return total;
        }

        private void ValidateSettings()
        {
            if (sqlOptions!.Retention!.Days < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(RetentionOptions.Days), sqlOptions!.Retention.Days, "Should be greater than zero.");
            }

            if (sqlOptions!.Retention!.BatchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(RetentionOptions.BatchSize), sqlOptions!.Retention.BatchSize, "Should be greater than zero.");
            }

            if (sqlOptions!.Retention!.Timeout < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(RetentionOptions.Timeout), sqlOptions!.Retention.Timeout, "Should be greater than zero.");
            }
        }
    }
}
