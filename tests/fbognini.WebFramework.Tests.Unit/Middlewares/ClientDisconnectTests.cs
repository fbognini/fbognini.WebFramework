using fbognini.WebFramework.Middlewares;
using Microsoft.AspNetCore.Http;

namespace fbognini.WebFramework.Tests.Unit.Middlewares;

public class ClientDisconnectTests
{
    [Fact]
    public void WasAbortedByClient_IsTrueOnlyWhenTheRequestTokenIsCancelled()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cancellationTokenSource.Token };

        context.WasAbortedByClient(new OperationCanceledException()).ShouldBeFalse();

        cancellationTokenSource.Cancel();

        context.WasAbortedByClient(new OperationCanceledException()).ShouldBeTrue();
    }

    // A TaskCanceledException raised by an HttpClient timeout reaches the handlers with the request token untouched: it is an error, not an abandoned request.
    [Fact]
    public void WasAbortedByClient_IsFalseForAnHttpTimeout()
    {
        var context = new DefaultHttpContext();

        context.WasAbortedByClient(new TaskCanceledException("Timeout", new TimeoutException())).ShouldBeFalse();
    }

    [Fact]
    public void WasAbortedByClient_IsFalseForAnyOtherException()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cancellationTokenSource.Token };
        cancellationTokenSource.Cancel();

        context.WasAbortedByClient(new InvalidOperationException()).ShouldBeFalse();
    }

    [Fact]
    public void IsHttpTimeout_DistinguishesTheTimeoutFromTheAbandonedRequest()
    {
        new TaskCanceledException("Timeout", new TimeoutException()).IsHttpTimeout().ShouldBeTrue();
        new TaskCanceledException().IsHttpTimeout().ShouldBeFalse();
        new OperationCanceledException().IsHttpTimeout().ShouldBeFalse();
    }

    [Fact]
    public void WasAbortedBy_ChecksTheGivenToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        new OperationCanceledException().WasAbortedBy(cancellationTokenSource.Token).ShouldBeFalse();

        cancellationTokenSource.Cancel();

        new OperationCanceledException().WasAbortedBy(cancellationTokenSource.Token).ShouldBeTrue();
        new InvalidOperationException().WasAbortedBy(cancellationTokenSource.Token).ShouldBeFalse();
    }
}
