using DN.WebApi.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Common;

internal static class Startup
{
    internal static IServiceCollection AddServices(this IServiceCollection services)
    {
        var transientServiceType = typeof(ITransientService);
        var transientServices = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => transientServiceType.IsAssignableFrom(p))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterfaces().FirstOrDefault(),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var transientService in transientServices)
        {
            if (transientServiceType.IsAssignableFrom(transientService.Service))
            {
                services.AddTransient(transientService.Service, transientService.Implementation);
            }
        }

        var scopedServiceType = typeof(IScopedService);
        var scopedServices = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => scopedServiceType.IsAssignableFrom(p))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterfaces().FirstOrDefault(),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var scopedService in scopedServices)
        {
            if (scopedServiceType.IsAssignableFrom(scopedService.Service))
            {
                services.AddScoped(scopedService.Service, scopedService.Implementation);
            }
        }

        return services;
    }
}