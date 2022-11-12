using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public class RetentionOptions
    {
        public int Days { get; set; } = -1;
        public int BatchSize { get; set; } = 1000;
        public bool RunOnStartup { get; set; } = false;
        public string CronExpression { get; set; } = "0 * * * *"; // every hour
    }

    public class RequestLoggingSettings
    {
        public string ConnectionString { get; set; }
        public string TableName { get; set; } = "Requests";
        public string SchemaName { get; set; } = "dbo";
        public string ColumnName { get; set; } = "TimeStamp";
        public bool LogRequest { get; set; } = true;
        public bool LogResponse { get; set; } = true;
        public RetentionOptions RetentionOptions { get; set; } = new();
        public IList<RequestAdditionalParameter> AdditionalParameters { get; set; }
    }
}
