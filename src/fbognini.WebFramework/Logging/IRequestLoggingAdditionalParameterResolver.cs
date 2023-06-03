using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public interface IRequestLoggingAdditionalParameterResolver
    {
        Task<object?> Resolve(string key);
    }
}
