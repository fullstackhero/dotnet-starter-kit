using FSH.Framework.Web.Endpoints.Abstractions;
using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace FSH.Framework.Web.Endpoints;
public static class Extensions
{
    public static WebApplication MapEndpointsFromAssembly(this WebApplication app, Assembly assembly)
    {
        var endpointDefinitions = assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEndpointDefinition))
                       && !t.IsAbstract
                       && !t.IsInterface)
            .Select(Activator.CreateInstance)
            .Cast<IEndpointDefinition>();

        foreach (var endpointDefinition in endpointDefinitions)
        {
            endpointDefinition.DefineEndpoints(app);
        }

        return app;
    }
}