using fbognini.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Handlers.Problems
{
    public class NotFoundProblemDetails : AppProblemDetails
    {
        public NotFoundProblemDetails(NotFoundException exception)
            : base(exception)
        {
        }
    }
}
