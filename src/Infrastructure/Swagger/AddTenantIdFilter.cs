using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace DN.WebApi.Infrastructure.Swagger;

public class AddTenantIdFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        if (context.MethodInfo.GetCustomAttribute(typeof(SwaggerHeaderAttribute)) is SwaggerHeaderAttribute attribute)
        {
            var existingParam = operation.Parameters.FirstOrDefault(p =>
                p.In == ParameterLocation.Header && p.Name == attribute.HeaderName);

            // remove description from [FromHeader] argument attribute
            if (existingParam != null)
            {
                operation.Parameters.Remove(existingParam);
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = attribute.HeaderName,
                In = ParameterLocation.Header,
                Description = attribute.Description,
                Required = attribute.IsRequired,
                Schema = string.IsNullOrEmpty(attribute.DefaultValue)
                    ? null
                    : new OpenApiSchema
                    {
                        Type = "String",
                        Default = new OpenApiString(attribute.DefaultValue)
                    }
            });
        }
    }
}