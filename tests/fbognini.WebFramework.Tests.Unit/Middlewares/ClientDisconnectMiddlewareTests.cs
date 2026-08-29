using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace fbognini.WebFramework.Tests.Unit.Middlewares;

public class ClientDisconnectMiddlewareTests
{
    [Fact]
    public async Task Invoke_SwallowsTheCancellationWhenTheClientIsGone()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cancellationTokenSource.Token };
        cancellationTokenSource.Cancel();

        var sut = new ClientDisconnectMiddleware(_ => throw new OperationCanceledException(), NullLogger<ClientDisconnectMiddleware>.Instance);

        await Should.NotThrowAsync(() => sut.Invoke(context));
    }

    [Fact]
    public async Task Invoke_RethrowsACancellationThatTheClientDidNotCause()
    {
        var context = new DefaultHttpContext();

        var sut = new ClientDisconnectMiddleware(_ => throw new TaskCanceledException("Timeout", new TimeoutException()), NullLogger<ClientDisconnectMiddleware>.Instance);

        await Should.ThrowAsync<TaskCanceledException>(() => sut.Invoke(context));
    }

    [Fact]
    public async Task Invoke_RethrowsAnyOtherExceptionEvenOnAnAbortedRequest()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cancellationTokenSource.Token };
        cancellationTokenSource.Cancel();

        var sut = new ClientDisconnectMiddleware(_ => throw new InvalidOperationException(), NullLogger<ClientDisconnectMiddleware>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => sut.Invoke(context));
    }

    [Fact]
    public async Task Invoke_ForwardsARequestThatCompletes()
    {
        var called = false;
        var context = new DefaultHttpContext();

        var sut = new ClientDisconnectMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<ClientDisconnectMiddleware>.Instance);

        await sut.Invoke(context);

        called.ShouldBeTrue();
    }
}
