using System.Reflection;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FSH.WebApi.Infrastructure.OpenApi;

public class SwaggerHeaderAttributeProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        if (context.MethodInfo?.GetCustomAttribute(typeof(SwaggerHeaderAttribute)) is SwaggerHeaderAttribute attribute)
        {
            var parameters = context.OperationDescription.Operation.Parameters;

            var existingParam = parameters.FirstOrDefault(p =>
                p.Kind == OpenApiParameterKind.Header && p.Name == attribute.HeaderName);
            if (existingParam is not null)
            {
                parameters.Remove(existingParam);
            }

            parameters.Add(new OpenApiParameter
            {
                Name = attribute.HeaderName,
                Kind = OpenApiParameterKind.Header,
                Description = attribute.Description,
                IsRequired = attribute.IsRequired,
                Schema = new NJsonSchema.JsonSchema
                {
                    Type = NJsonSchema.JsonObjectType.String,
                    Default = attribute.DefaultValue
                }
            });
        }

        return true;
    }
}