using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace fbognini.WebFramework.Filters
{
    public class FromMultiSourceAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource => CompositeBindingSource.Create(
            new[] { BindingSource.Path, BindingSource.Query },
            nameof(FromMultiSourceAttribute));
    }

}
