using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace fbognini.WebFramework.ModelBinders
{
    public class DateTimeModelBinder : IModelBinder
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

            var dateStr = valueProviderResult.FirstValue;
            // Here you define your custom parsing logic, i.e. using "de-DE" culture

            if (dateStr == string.Empty)
            {
                //bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            DateTime date;
            if (
                !(DateTime.TryParseExact(dateStr, "dd/MM/yyyy", new CultureInfo("it-IT"), DateTimeStyles.None, out date)
                    || DateTime.TryParseExact(dateStr, "dd/MM/yyyy HH:mm:ss", new CultureInfo("it-IT"), DateTimeStyles.None, out date)))
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "DateTime should be in format 'dd/MM/yyyy HH:mm:ss'");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(date);
            return Task.CompletedTask;
        }
    }

}