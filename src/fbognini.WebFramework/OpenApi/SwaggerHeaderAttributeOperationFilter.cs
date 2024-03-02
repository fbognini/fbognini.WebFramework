using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace fbognini.WebFramework.OpenApi;

internal class SwaggerHeaderAttributeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo?.GetCustomAttribute(typeof(SwaggerHeaderAttribute)) is not SwaggerHeaderAttribute attribute)
        {
            return;
        }

        var parameters = operation.Parameters;

        var existingParam = parameters.FirstOrDefault(p =>
            p.In == ParameterLocation.Header && p.Name == attribute.HeaderName);
        if (existingParam is not null)
        {
            parameters.Remove(existingParam);
        }

        var parameter = new OpenApiParameter
        {
            Name = attribute.HeaderName,
            In = ParameterLocation.Header,
            Description = attribute.Description,
            Required = attribute.IsRequired,
            Schema = new OpenApiSchema()
            {
                Type = "string"
            }
        };

        if (!string.IsNullOrWhiteSpace(attribute.DefaultValue))
        {
            parameter.Schema.Default = new OpenApiString(attribute.DefaultValue);
        }

        parameters.Add(parameter);
    }
}