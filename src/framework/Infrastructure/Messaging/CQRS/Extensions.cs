using System.Reflection;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Infrastructure.Messaging.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.CQRS;
public static class Extensions
{
    public static IServiceCollection RegisterCommandAndQueryDispatchers(this IServiceCollection services)
    {
        // Register dispatchers
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Register decorators
        services.Decorate<ICommandDispatcher, CommandValidation>();
        services.Decorate<IQueryDispatcher, QueryValidation>();

        return services;
    }
    public static IServiceCollection RegisterCommandAndQueryHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Deduplicate Assemblies
        var distinctAssemblies = assemblies.Distinct().ToArray();

        // Scan for handlers in provided assemblies
        services.Scan(scan => scan
            .FromAssemblies(distinctAssemblies)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(distinctAssemblies)
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
