using FSH.Modules.Common.Infrastructure.Modules;
using System.Reflection;

namespace FSH.PlayGround.Api.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration config)
    {
        var moduleTypes = typeof(ModuleExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            Console.WriteLine($"[AddModule] Registering module: {type.FullName}");
            var module = (IModule)Activator.CreateInstance(type)!;
            module.AddModule(services, config);
        }

        return services;
    }

    public static WebApplication ConfigureModules(this WebApplication app)
    {
        var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            Console.WriteLine($"[ConfigureModule] Configuring module: {type.FullName}");
            var module = (IModule)Activator.CreateInstance(type)!;
            module.ConfigureModule(app);
        }

        return app;
    }
}
