using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace FSH.Framework.Web.Modules;

public static class ModuleLoader
{
    private static readonly List<IModule> _modules = new();
    private static readonly object _lock = new();
    private static bool _modulesLoaded;

    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        lock (_lock)
        {
            if (_modulesLoaded)
            {
                return builder;
            }

            builder.Services.AddValidatorsFromAssemblies(assemblies);

            var source = assemblies is { Length: > 0 }
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies();

            var moduleRegistrations = source
                .SelectMany(a => a.GetCustomAttributes<FshModuleAttribute>())
                .Where(r => typeof(IModule).IsAssignableFrom(r.ModuleType))
                .DistinctBy(r => r.ModuleType)
                .OrderBy(r => r.Order)
                .ThenBy(r => r.ModuleType.Name)
                .Select(r => r.ModuleType);

            foreach (var moduleType in moduleRegistrations)
            {
                if (Activator.CreateInstance(moduleType) is not IModule module)
                {
                    throw new InvalidOperationException($"Unable to create module {moduleType.Name}.");
                }

                module.ConfigureServices(builder);
                _modules.Add(module);
            }

            _modulesLoaded = true;
        }

        return builder;
    }

    public static IApplicationBuilder UseModuleMiddlewares(this IApplicationBuilder app)
    {
        foreach (var m in _modules)
            m.ConfigureMiddleware(app);

        return app;
    }

    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        foreach (var m in _modules)
            m.MapEndpoints(endpoints);

        return endpoints;
    }
}
