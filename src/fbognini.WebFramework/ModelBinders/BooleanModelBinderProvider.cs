using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace fbognini.WebFramework.ModelBinders
{
    public class BooleanModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(Boolean) ||
                context.Metadata.ModelType == typeof(Boolean?))
            {
                return new BooleanModelBinder();
            }

            return null;
        }
    }
}
