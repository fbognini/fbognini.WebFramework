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
        private string? propertyName;

        public RequestAdditionalParameter()
        {

        }

        public RequestAdditionalParameter(string parameter, RequestAdditionalParameterType type = RequestAdditionalParameterType.Query)
        {
            Parameter = parameter;
            Type = type;
        }

        public string Parameter { get; set; }
        public string PropertyName
        {
            get => propertyName ?? Parameter;
            set => propertyName = value;
        }
        public RequestAdditionalParameterType Type { get; set; }
    }
}
