using System.Collections.Generic;

namespace fbognini.WebFramework.Logging
{
    public class SqlOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = "TimeStamp";
        public RetentionOptions? Retention { get; set; }
    }
}
