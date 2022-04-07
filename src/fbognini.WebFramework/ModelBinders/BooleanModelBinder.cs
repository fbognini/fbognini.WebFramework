using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace fbognini.WebFramework.ModelBinders
{
    public class BooleanModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            // Try to fetch the value of the argument by name
            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var boolStr = valueProviderResult.FirstValue;
            // Here you define your custom parsing logic, i.e. using "de-DE" culture

            if (boolStr == string.Empty)
            {
                //bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            if (boolStr == "on" || boolStr.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                bindingContext.Result = ModelBindingResult.Success(true);

            return Task.CompletedTask;
        }
    }

}
