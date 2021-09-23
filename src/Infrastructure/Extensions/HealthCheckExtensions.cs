using DN.WebApi.Infrastructure.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class HealthCheckExtensions
    {
        internal static IServiceCollection AddHealthCheckExtension(this IServiceCollection services)
        {
            services.AddHealthChecks().AddCheck<TenantHealthCheck>("Tenant");
            return services;
        }
    }
}