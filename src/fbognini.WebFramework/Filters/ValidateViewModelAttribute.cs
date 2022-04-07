using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PBO.Internal.Attributes
{
    public class ValidateViewModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {

                var controller = context.Controller as Controller;

                context.Result = new ViewResult
                {
                    ViewName = controller.ControllerContext.ActionDescriptor.ActionName,
                    ViewData = controller.ViewData,
                    TempData = controller.TempData,
                };
                //context.Result = new BadRequestObjectResult(context.ModelState); // it returns 400 with the error
            }
        }
    }
}
