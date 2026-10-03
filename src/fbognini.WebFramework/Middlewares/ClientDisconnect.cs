using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;

namespace fbognini.WebFramework.Middlewares
{
    public static class ClientDisconnect
    {
        // The exception type alone does not tell noise from failure: HttpClient throws TaskCanceledException both when the caller goes away and when its own timeout expires, while EF Core, SqlClient and Kestrel throw a plain OperationCanceledException. What discriminates is whether the request token was cancelled.
        // ConnectionResetException is the exception: the server throws it only when the client is gone while the request body is read, and IIS cancels the request token later, on the thread pool, so the token cannot be required.
        public static bool WasAbortedByClient(this HttpContext context, Exception exception)
        {
            return exception is ConnectionResetException
                || exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested;
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
