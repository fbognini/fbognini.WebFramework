using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace fbognini.WebFramework.Endpoints;

#if NET7_0_OR_GREATER
public interface IEndpoints
{
    public static abstract void DefineEndpoints(IEndpointRouteBuilder app);

    public static abstract void AddServices(IServiceCollection services, IConfiguration configuration);
}
#endif