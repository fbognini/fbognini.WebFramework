using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public class RequestLoggingSettings
    {
        public RequestLoggingSettings(IEnumerable<RequestAdditionalParameter> additionalParameters = null)
            : this(true, true, additionalParameters)
        {
        }

        public RequestLoggingSettings(bool logrequest, bool logresponse, IEnumerable<RequestAdditionalParameter> additionalParameters = null)
        {
            LogRequest = logrequest;
            LogResponse = logresponse;
            AdditionalParameters = additionalParameters ?? Enumerable.Empty<RequestAdditionalParameter>();
        }

        public bool LogRequest { get; }
        public bool LogResponse { get; }
        public IEnumerable<RequestAdditionalParameter> AdditionalParameters { get; }
    }
}
