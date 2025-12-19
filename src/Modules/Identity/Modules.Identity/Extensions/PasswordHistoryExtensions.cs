using FSH.Modules.Identity.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Identity.Extensions;

/// <summary>
/// Extension methods for password history configuration.
/// </summary>
public static class PasswordHistoryExtensions
{
    /// <summary>
    /// Adds password history configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure password history options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigurePasswordHistory(
        this IServiceCollection services,
        Action<PasswordHistoryOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<PasswordHistoryOptions>(_ => { });
        }

        return services;
    }
}
