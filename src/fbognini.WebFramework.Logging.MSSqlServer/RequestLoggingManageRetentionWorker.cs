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
        private readonly IOptions<RequestLoggingSettings> _requestLoggingOptions;
        private readonly ILogger<RequestLoggingSqlManageRetentionWorker> _logger;

        public RequestLoggingSqlManageRetentionWorker(IOptions<RequestLoggingSettings> requestLoggingOptions, ILogger<RequestLoggingSqlManageRetentionWorker> logger)
        {
            _requestLoggingOptions = requestLoggingOptions;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sqlOptions = _requestLoggingOptions.Value.SqlOptions;
            if (sqlOptions?.Retention is null)
            {
                _logger.LogInformation("No retention, logs table should be deleted manually");
                return;
            }

            ValidateSettings(sqlOptions);

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
            var sqlOptions = _requestLoggingOptions.Value.SqlOptions;

            try
            {
                _logger.LogInformation("Manage retentions of {Days} days for [{SchemaName}].[{TableName}]", sqlOptions!.Retention!.Days, sqlOptions!.SchemaName, sqlOptions!.TableName);

                ValidateSettings(sqlOptions);

                var total = await DeletePreviousRows(
                    sqlOptions!.TableName,
                    sqlOptions!.SchemaName,
                    sqlOptions!.ColumnName,
                    DateTime.Now.AddDays(-sqlOptions!.Retention.Days),
                    sqlOptions!.Retention.BatchSize,
                    cancellationToken);

                _logger.LogInformation("Successfully deleted {rows} rows deleted from [{SchemaName}].[{TableName}]", total, sqlOptions!.SchemaName, sqlOptions!.TableName);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "An SqlException occours during DeletePreviousRows from [{SchemaName}].[{TableName}]", sqlOptions!.SchemaName, sqlOptions!.TableName);
            }
        }

        private async Task<long> DeletePreviousRows(string table, string schema, string column, DateTime date, int batch, CancellationToken cancellationToken)
        {
            var sqlOptions = _requestLoggingOptions.Value.SqlOptions;

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

                _logger.LogInformation("{NoOfDeletedRows} rows deleted from [{SchemaName}].[{TableName}] in {seconds} seconds ({NoOfDeletedRowsInTotal} in total)", deleted, sqlOptions!.SchemaName, sqlOptions!.TableName, watch.Elapsed.TotalSeconds, total);

            } while (deleted != 0);


            return total;
        }

        private static void ValidateSettings(SqlOptions sqlOptions)
        {
            ArgumentNullException.ThrowIfNull(sqlOptions, nameof(sqlOptions));

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
