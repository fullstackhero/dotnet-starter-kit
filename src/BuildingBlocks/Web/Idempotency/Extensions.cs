using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Web.Idempotency;

public static class Extensions
{
    /// <summary>
    /// Registers idempotency options for use by IdempotencyEndpointFilter.
    /// Apply to specific endpoints via .WithIdempotency() extension.
    /// </summary>
    public static IServiceCollection AddHeroIdempotency(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<IdempotencyOptions>()
            .BindConfiguration(nameof(IdempotencyOptions));

        return services;
    }
}
