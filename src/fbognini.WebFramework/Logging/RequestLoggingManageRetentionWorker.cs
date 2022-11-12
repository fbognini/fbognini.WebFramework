using Cronos;
using fbognini.WebFramework.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
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
            logger.LogInformation("Manage retentions of {days} for [{schema}].[{table}]", settings.RetentionOptions.Days, settings.SchemaName, settings.TableName);

            ValidateSettings();

            var total = await DeletePreviousRows(
                settings.TableName,
                settings.SchemaName,
                settings.ColumnName,
                DateTime.Now.AddDays(-settings.RetentionOptions.Days),
                settings.RetentionOptions.BatchSize,
                cancellationToken);

            logger.LogInformation("{rows} rows deleted from [{schema}].[{table}]", total, settings.SchemaName, settings.TableName);
        }

        private async Task<long> DeletePreviousRows(string table, string schema, string column, DateTime date, int batch, CancellationToken cancellationToken)
        {
            var sql = @$"
DECLARE @BatchSize INT = 1, @Total BIGINT = 0
SET rowcount {batch}
WHILE @BatchSize <> 0
BEGIN
	
	DECLARE @sql NVARCHAR(MAX);

	SET @sql = '
		DELETE FROM ' + QUOTENAME('{schema}') + '.' + QUOTENAME('{table}') + '
		WHERE ' + QUOTENAME('{column}') + ' < @retentiondate'

	exec sp_executesql @sql, 
                    N'@retentiondate datetime2(7)', @RetentionDate;

	SET @BatchSize = @@rowcount
	SET @Total = @Total + @BatchSize

END

SELECT @Total
";

            using SqlConnection connection = new(settings.ConnectionString);
            // Create the command and set its properties.
            SqlCommand command = new()
            {
                Connection = connection,
                CommandText = sql,
                CommandType = CommandType.Text
            };

            // Add the input parameter and set its properties.
            SqlParameter parameter = new()
            {
                ParameterName = "@RetentionDate",
                SqlDbType = SqlDbType.DateTime2,
                Direction = ParameterDirection.Input,
                Value = date
            };

            // Add the parameter to the Parameters collection.
            command.Parameters.Add(parameter);

            // Open the connection and execute the reader.
            connection.Open();

            return (long)await command.ExecuteScalarAsync(cancellationToken);
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
