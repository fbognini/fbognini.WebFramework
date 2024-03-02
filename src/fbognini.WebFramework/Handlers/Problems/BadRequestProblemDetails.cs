using fbognini.Core.Exceptions;

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
