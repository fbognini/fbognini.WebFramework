using Microsoft.AspNetCore.Http;
using System;
using System.Threading;

namespace fbognini.WebFramework.Middlewares
{
    public static class ClientDisconnect
    {
        // The exception type alone does not tell noise from failure: HttpClient throws TaskCanceledException both when the caller goes away and when its own timeout expires, while EF Core, SqlClient and Kestrel throw a plain OperationCanceledException. What discriminates is whether the request token was cancelled.
        public static bool WasAbortedByClient(this HttpContext context, Exception exception)
        {
            return exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested;
        }

        public static bool WasAbortedBy(this Exception exception, CancellationToken cancellationToken)
        {
            return exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
        }

        // A TaskCanceledException wrapping a TimeoutException is an HttpClient timeout, not an abandoned request: the outbound call did not answer in time and stays an error.
        public static bool IsHttpTimeout(this Exception exception)
        {
            return exception is OperationCanceledException && exception.InnerException is TimeoutException;
        }
    }
}
