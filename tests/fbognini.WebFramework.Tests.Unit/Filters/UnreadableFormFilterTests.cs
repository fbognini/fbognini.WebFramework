using fbognini.WebFramework.Filters;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace fbognini.WebFramework.Tests.Unit.Filters;

public class UnreadableFormFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_RethrowsTheFormFailureInsteadOfRunningTheAction()
    {
        var httpContext = CreateFormContext(new DisconnectedStream());
        await Should.ThrowAsync<ConnectionResetException>(() => httpContext.Request.ReadFormAsync());

        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "Failed to read the request form. The client has disconnected");

        var (context, next, actionRan) = CreateActionContext(httpContext, modelState);

        await Should.ThrowAsync<ConnectionResetException>(() => new UnreadableFormFilter().OnActionExecutionAsync(context, next));
        actionRan().ShouldBeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_DoesNotTouchTheBodyWhenTheModelStateIsValid()
    {
        var httpContext = CreateFormContext(new DisconnectedStream());

        var (context, next, actionRan) = CreateActionContext(httpContext, new ModelStateDictionary());

        await new UnreadableFormFilter().OnActionExecutionAsync(context, next);
        actionRan().ShouldBeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_RunsTheActionWhenTheFormWasReadAndOnlyValidationFailed()
    {
        var httpContext = CreateFormContext(new MemoryStream(Encoding.UTF8.GetBytes("name=")));

        var modelState = new ModelStateDictionary();
        modelState.AddModelError("name", "The name field is required.");

        var (context, next, actionRan) = CreateActionContext(httpContext, modelState);

        await new UnreadableFormFilter().OnActionExecutionAsync(context, next);
        actionRan().ShouldBeTrue();
    }

    private static DefaultHttpContext CreateFormContext(Stream body)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Body = body;

        return httpContext;
    }

    private static (ActionExecutingContext Context, ActionExecutionDelegate Next, Func<bool> ActionRan) CreateActionContext(HttpContext httpContext, ModelStateDictionary modelState)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
        var filters = new List<IFilterMetadata>();
        var controller = new object();
        var context = new ActionExecutingContext(actionContext, filters, new Dictionary<string, object?>(), controller);

        var actionRan = false;
        ActionExecutionDelegate next = () =>
        {
            actionRan = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, filters, controller));
        };

        return (context, next, () => actionRan);
    }

    private sealed class DisconnectedStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => throw new ConnectionResetException("The client has disconnected");
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new ConnectionResetException("The client has disconnected");
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new ConnectionResetException("The client has disconnected");

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
