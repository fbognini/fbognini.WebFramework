using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

#if NET7_0_OR_GREATER
namespace fbognini.WebFramework.Handlers
{
    public static class MinimalatrExtensions
    {
        private const string ContentType = "application/json";
        public static RouteHandlerBuilder MediateGet<TRequest>(this IEndpointRouteBuilder app, string? template = null) where TRequest : IHttpRequest
            => app.MapGet(template ?? string.Empty, async (IMediator mediator, [AsParameters] TRequest request) => await mediator.Send(request));

        public static RouteHandlerBuilder MediatePost<TRequest>(this IEndpointRouteBuilder app, string? template = null) where TRequest : IHttpRequest
            => app.MapPost(template ?? string.Empty, async (IMediator mediator, [AsParameters] TRequest request) => await mediator.Send(request))
                .Accepts<TRequest>(ContentType);

        public static RouteHandlerBuilder MediatePostBody<TRequest>(this IEndpointRouteBuilder app, string? template = null) where TRequest : IHttpRequest
            => app.MapPost(template ?? string.Empty, async (IMediator mediator, [FromBody] TRequest request) => await mediator.Send(request))
                .Accepts<TRequest>(ContentType);

        public static RouteHandlerBuilder MediatePut<TRequest>(this IEndpointRouteBuilder app, string? template = null) where TRequest : IHttpRequest
            => app.MapPut(template ?? string.Empty, async (IMediator mediator, [AsParameters] TRequest request) => await mediator.Send(request))
                .Accepts<TRequest>(ContentType);

        public static RouteHandlerBuilder MediateDelete<TRequest>(this IEndpointRouteBuilder app, string? template = null) where TRequest : IHttpRequest
			=> app.MapDelete(template ?? string.Empty, async (IMediator mediator, [AsParameters] TRequest request) => await mediator.Send(request))
				.Accepts<TRequest>(ContentType);

	}
}
#endif
