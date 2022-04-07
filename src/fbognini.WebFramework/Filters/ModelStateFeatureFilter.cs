using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Filters
{
    public class ModelStateFeature
    {
        public ModelStateDictionary ModelState { get; set; }

        public ModelStateFeature(ModelStateDictionary state)
        {
            this.ModelState = state;
        }
    }

    public class ModelStateFeatureFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var state = context.ModelState;
            context.HttpContext.Features.Set<ModelStateFeature>(new ModelStateFeature(state));
            await next();
        }
    }
}
