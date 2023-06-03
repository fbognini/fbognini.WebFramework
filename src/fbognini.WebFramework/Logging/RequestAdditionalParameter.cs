using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Builder;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public enum RequestAdditionalParameterType
    {
        Query,
        Header,
        Session,
        Cookie,
        Custom,
    }

    public class RequestAdditionalParameter
    {
        public RequestAdditionalParameter()
        {

        }

        public RequestAdditionalParameter(string parameter, SqlColumn sqlColumn, RequestAdditionalParameterType type = RequestAdditionalParameterType.Query)
        {
            Parameter = parameter;
            SqlColumn = sqlColumn;
            Type = type;
        }

        public string Parameter { get; set; }
        public SqlColumn SqlColumn { get; set; }
        public RequestAdditionalParameterType Type { get; set; }
    }
}
