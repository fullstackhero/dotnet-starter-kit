using System.Reflection;
using FSH.Framework.Core.Messaging.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.Events;
public static class Extensions
{
    public static IServiceCollection RegisterInMemoryEventBus(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IEventPublisher, InMemoryEventPublisher>();

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
