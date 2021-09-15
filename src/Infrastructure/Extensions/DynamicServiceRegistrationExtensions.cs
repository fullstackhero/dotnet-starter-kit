using DN.WebApi.Application.Abstractions.Services;
using DN.WebApi.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            var transientServiceType = typeof(ITransientService);
            var servicetypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => transientServiceType.IsAssignableFrom(p))
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Service = t.GetInterfaces().FirstOrDefault(),
                    Implementation = t
                })
                .Where(t => t.Service != null);

            foreach (var type in servicetypes)
            {
                if (transientServiceType.IsAssignableFrom(type.Service))
                {
                    services.AddTransient(type.Service, type.Implementation);
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
}