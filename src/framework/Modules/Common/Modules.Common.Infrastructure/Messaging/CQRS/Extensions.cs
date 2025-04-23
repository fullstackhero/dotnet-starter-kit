using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Infrastructure.Messaging.CQRS.Validation;
using FSH.Modules.Common.Core.Messaging.CQRS;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Framework.Infrastructure.Messaging.CQRS;
public static class Extensions
{
    public static IServiceCollection AddCommandAndQueryDispatchers(this IServiceCollection services)
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