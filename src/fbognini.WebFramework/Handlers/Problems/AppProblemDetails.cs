using fbognini.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Handlers.Problems
{
    public class AppProblemDetails : ProblemDetails
    {
        public AppProblemDetails(AppException exception)
        {
            Status = (int)exception.HttpStatusCode;
            Title = exception.Title;
            Detail = exception.Message;
            Type = exception.Type;
        }
    }
}
