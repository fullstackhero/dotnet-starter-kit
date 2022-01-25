using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FSH.WebApi.Infrastructure.OpenApi;

internal static class ObjectExtensions
{
    public static T? TryGetPropertyValue<T>(this object? obj, string propertyName, T? defaultValue = default) =>
        obj?.GetType().GetRuntimeProperty(propertyName) is PropertyInfo propertyInfo
            ? (T?)propertyInfo.GetValue(obj)
            : defaultValue;
}

/// <summary>
/// The default NSwag AspNetCoreOperationProcessor doesn't take .RequireAuthorization() calls into account
/// Unless the AllowAnonymous attribute is defined, this processor will always add the security scheme
/// when it's not already there, so effectively adding "Global Auth".
/// </summary>
public class SwaggerGlobalAuthProcessor : IOperationProcessor
{
    private readonly string _name;

    public SwaggerGlobalAuthProcessor()
        : this(JwtBearerDefaults.AuthenticationScheme)
    {
    }

    public SwaggerGlobalAuthProcessor(string name)
    {
        _name = name;
    }

    public bool Process(OperationProcessorContext context)
    {
        IList<object>? list = ((AspNetCoreOperationProcessorContext)context).ApiDescription?.ActionDescriptor?.TryGetPropertyValue<IList<object>>("EndpointMetadata");
        if (list is not null)
        {
            if (list.OfType<AllowAnonymousAttribute>().Any())
            {
                return true;
            }

            if (context.OperationDescription.Operation.Security?.Any() != true)
            {
                (context.OperationDescription.Operation.Security ??= new List<OpenApiSecurityRequirement>()).Add(new OpenApiSecurityRequirement
                {
                    {
                        _name,
                        Array.Empty<string>()
                    }
                });
            }
        }

        return true;
    }
}