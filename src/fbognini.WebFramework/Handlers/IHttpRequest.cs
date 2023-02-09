using MediatR;
using Microsoft.AspNetCore.Http;

namespace fbognini.WebFramework.Handlers
{
    public interface IHttpRequest: IRequest<IResult>
    {
    }
}
