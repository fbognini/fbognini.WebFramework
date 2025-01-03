using System.Collections.Generic;

namespace fbognini.WebFramework.Logging
{

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
