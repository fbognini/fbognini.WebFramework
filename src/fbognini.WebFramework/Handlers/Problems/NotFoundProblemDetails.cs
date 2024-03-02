using fbognini.Core.Exceptions;
using System;

namespace fbognini.WebFramework.Handlers.Problems
{
    public class NotFoundProblemDetails : AppProblemDetails
    {
        public NotFoundProblemDetails(NotFoundException exception)
            : base(exception)
        {
        }

        public NotFoundProblemDetails(Type type, object key)
            : base(new NotFoundException(type, key))
        {
        }
    }
}
