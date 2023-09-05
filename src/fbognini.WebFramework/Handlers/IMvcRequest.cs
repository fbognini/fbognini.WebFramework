using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fbognini.WebFramework.Handlers
{
    public interface IMvcRequest : IRequest<IActionResult>
    {
        Controller? Controller { get; set; }
    }
}
