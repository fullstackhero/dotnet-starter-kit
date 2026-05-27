using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace FSH.Framework.Web.FeatureFlags;

public static class Extensions
{
    /// <summary>
    /// Adds feature management with tenant-aware feature filters.
    /// Reads feature flags from the "FeatureManagement" configuration section.
    /// </summary>
    public static IServiceCollection AddHeroFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"))
            .AddFeatureFilter<TenantFeatureFilter>();

        return services;
    }
}
