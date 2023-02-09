using fbognini.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Handlers.Problems
{
    public class BadRequestProblemDetails : AppProblemDetails
    {
        public BadRequestProblemDetails(BadRequestException exception)
            : base(exception)
        {
        }
    }
}
