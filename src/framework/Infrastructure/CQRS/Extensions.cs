using System.Reflection;
using FSH.Framework.Core.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.CQRS;
public static class Extensions
{
    public static IServiceCollection RegisterCommandAndQueryHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Scan for handlers in provided assemblies
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
