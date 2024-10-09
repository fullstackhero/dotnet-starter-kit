using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FSH.Framework.Infrastructure.OpenApi;
/// <summary>
/// Fixed error enum name generation with _0, _1, _2 ...
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
        {
            return;
        }

        var array = new OpenApiArray();
        array.AddRange(Enum.GetNames(context.Type).Select(n => new OpenApiString(n)));
        // NSwag
        schema.Extensions.Add("x-enumNames", array);
        // Openapi-generator
        schema.Extensions.Add("x-enum-varnames", array);
    }
}


// public class EnumSchemaFilter2 : ISchemaFilter
// {
//     public void Apply(OpenApiSchema schema, SchemaFilterContext context)
//     {
//         if (!context.Type.IsEnum) return;
//
//         schema.Enum.Clear();
//         var enumValues = Enum.GetValues(context.Type);
//         foreach (object enumValue in enumValues)
//         {
//             schema.Enum.Add(new OpenApiInteger(Convert.ToInt32(enumValue)));
//         }
//
//
//         var openApiExtension = new OpenApiArray();
//         foreach (var key in Enum.GetNames(context.Type))
//         {
//             openApiExtension.Add(new OpenApiString(key));
//         }
//
//         schema.Extensions.Add("x-enumNames", openApiExtension);
//     }
// }
