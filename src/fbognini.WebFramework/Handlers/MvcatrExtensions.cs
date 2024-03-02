using Microsoft.AspNetCore.Mvc;

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
