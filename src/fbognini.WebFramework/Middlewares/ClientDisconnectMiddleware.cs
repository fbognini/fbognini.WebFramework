using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Middlewares;

public class ClientDisconnectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClientDisconnectMiddleware> _logger;

    public ClientDisconnectMiddleware(RequestDelegate next, ILogger<ClientDisconnectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (context.WasAbortedByClient(ex))
        {
            _logger.LogDebug("Request {Method} {Path} was aborted by the client", context.Request.Method, context.Request.Path.Value);
        }
    }
}

public static class ClientDisconnectApplicationBuilderExtensions
{
    public static IApplicationBuilder UseClientDisconnectHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ClientDisconnectMiddleware>();
    }
}