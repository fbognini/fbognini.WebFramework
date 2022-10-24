using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public class RequestLoggingSettings
    {
        public RequestLoggingSettings(bool saveResponse, IEnumerable<RequestAdditionalParameter> additionalParameters = null)
        {
            SaveResponse = saveResponse;
            AdditionalParameters = additionalParameters ?? Enumerable.Empty<RequestAdditionalParameter>();
        }

        public bool SaveResponse { get; }
        public IEnumerable<RequestAdditionalParameter> AdditionalParameters { get; }
    }
}
