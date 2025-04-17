using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Framework.Infrastructure.Modules;
public static class Extensions
{
    public static IServiceCollection AddFrameworkModules(
        this IServiceCollection services,
        IConfiguration config,
        params Assembly[] assemblies)
    {
        var moduleTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IFrameworkModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var type in moduleTypes)
        {
            var module = (IFrameworkModule)Activator.CreateInstance(type)!;
            module.AddModuleServices(services, config);
            services.AddSingleton(module); // Store for endpoint mapping later
        }

        return services;
    }

    public static IApplicationBuilder MapFrameworkModuleEndpoints(this IApplicationBuilder app)
    {
        var modules = app.ApplicationServices.GetServices<IFrameworkModule>();
        var endpoints = app as IEndpointRouteBuilder;

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints!);
        }

        return app;
    }
}