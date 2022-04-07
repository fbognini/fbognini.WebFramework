using fbognini.Core.Data.Pagination;
using fbognini.WebFramework.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Net;

namespace fbognini.WebFramework.Filters
{
    public class FromMultiSourceAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource => CompositeBindingSource.Create(
            new[] { BindingSource.Path, BindingSource.Query },
            nameof(FromMultiSourceAttribute));
    }

}
