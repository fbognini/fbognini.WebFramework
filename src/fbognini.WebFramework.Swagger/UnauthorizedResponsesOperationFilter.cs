using fbognini.WebFramework.Filters;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace fbognini.WebFramework.Swagger
{
    public class UnauthorizedResponsesOperationFilter : IOperationFilter
    {
        private readonly bool includeUnauthorizedAndForbiddenResponses;
        private readonly OpenApiSecurityScheme scheme;

        public UnauthorizedResponsesOperationFilter(bool includeUnauthorizedAndForbiddenResponses, OpenApiSecurityScheme scheme)
        {
            this.includeUnauthorizedAndForbiddenResponses = includeUnauthorizedAndForbiddenResponses;
            this.scheme = scheme;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var filters = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var attributes = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            var hasAnonymousFilter = filters.Any(p => p.Filter is AllowAnonymousFilter);
            var hasAnonymousAttribute = attributes.Any(p => p is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute);
            if (hasAnonymousFilter || hasAnonymousAttribute) return;

            var hasAuthorizeFilter = filters.Any(p => p.Filter is AuthorizeFilter);
            var hasAuthorizeAttributes = attributes.Any(p => p is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
            if (!(hasAuthorizeFilter || hasAuthorizeAttributes)) return;

            if (includeUnauthorizedAndForbiddenResponses)
            {
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
            }

            operation.Security = new List<OpenApiSecurityRequirement>()
            {
                new OpenApiSecurityRequirement()
                {
                    {
                        scheme
                        , new List<string>()
                    }
                }
            };
        }
    }

}
