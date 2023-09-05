using fbognini.WebFramework.Filters;
using fbognini.WebFramework.Handlers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

#if NET7_0
namespace fbognini.WebFramework.Handlers
{
    public static class MvcatrExtensions
    {
        public static void LoadController(this IMvcRequest request, Controller controller)
        {
            request.Controller = controller;
        }

    }
}
#endif
