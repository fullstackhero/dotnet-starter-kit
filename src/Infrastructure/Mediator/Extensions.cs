using FSH.Framework.Infrastructure.Mediator.Validation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.Framework.Infrastructure.Mediator;
public static class Extensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IMediator, Mediator>();
        RegisterHandlers(services, assemblies);
        services.Decorate<IMediator, RequestValidation>();

        return services;
    }

    internal static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        // Deduplicate Assemblies
        var distinctAssemblies = assemblies.Distinct().ToArray();

        // Scan for handlers in provided assemblies
        services.Scan(scan => scan
            .FromAssemblies(distinctAssemblies)
            .AddClasses(c => c.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}