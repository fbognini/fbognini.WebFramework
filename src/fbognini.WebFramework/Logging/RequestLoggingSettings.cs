using Serilog.Sinks.MSSqlServer;
using System.Collections.Generic;

namespace fbognini.WebFramework.Logging
{
    public class SqlOptions
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string ColumnName { get; set; } = "TimeStamp";
        public RetentionOptions? Retention { get; set; }
        public Dictionary<string, SqlColumn> AdditionalColumns { get; set; }
    }

    public class RetentionOptions
    {
        public int Days { get; set; } = -1;
        public int BatchSize { get; set; } = 1000;
        public int Timeout { get; set; } = 200;
        public bool RunOnStartup { get; set; } = false;
        public string CronExpression { get; set; } = "0 * * * *"; // every hour
    }

    public class RequestLoggingSettings
    {
        public bool LogRequest { get; set; } = true;
        public bool LogResponse { get; set; } = true;
        public List<RequestAdditionalParameter> AdditionalParameters { get; set; } = new();

        public SqlOptions? SqlOptions { get; set; }
    }
}
