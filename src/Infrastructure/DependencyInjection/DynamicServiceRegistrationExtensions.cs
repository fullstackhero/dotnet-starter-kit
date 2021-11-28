using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.DependencyInjection;

public static class DynamicServiceRegistrationExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        var transientServiceType = typeof(ITransientService);
        var scopedServiceType = typeof(IScopedService);
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

        foreach (var transientService in transientServices)
        {
            if (transientServiceType.IsAssignableFrom(transientService.Service))
            {
                services.AddTransient(transientService.Service, transientService.Implementation);
            }
        }

        foreach (var scopedService in scopedServices)
        {
            if (scopedServiceType.IsAssignableFrom(scopedService.Service))
            {
                services.AddScoped(scopedService.Service, scopedService.Implementation);
            }
        }

        return services;
    }

    public static IServiceCollection RegisterAppSettings(this IServiceCollection services, IConfiguration config)
    {
        var appSettingsType = typeof(IAppSettings);
        var servicetypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => appSettingsType.IsAssignableFrom(p))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                Service = t.GetInterfaces().FirstOrDefault(),
                Implementation = t
            })
            .Where(t => t.Service != null);

        foreach (var type in servicetypes)
        {
            if (appSettingsType.IsAssignableFrom(type.Service))
            {
                config.Bind(type.Implementation);
            }
        }

        return services;
    }
}