using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public class SqlOptions
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string ColumnName { get; set; } = "TimeStamp";
    }

    public class RetentionOptions
    {
        public int Days { get; set; } = -1;
        public int BatchSize { get; set; } = 1000;
        public bool RunOnStartup { get; set; } = false;
        public string CronExpression { get; set; } = "0 * * * *"; // every hour
    }

    public class RequestLoggingSettings
    {
        public bool LogRequest { get; set; } = true;
        public bool LogResponse { get; set; } = true;
        public SqlOptions? SqlOptions { get; set; }
        public RetentionOptions? RetentionOptions { get; set; }
        public List<RequestAdditionalParameter> AdditionalParameters { get; set; } = new();
    }
}
