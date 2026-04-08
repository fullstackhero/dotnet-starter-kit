using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FSH.Framework.Caching;

/// <summary>
/// DI extensions for the HybridCache-backed caching building block.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="HybridCache"/> layered over either Redis (when
    /// <see cref="CachingOptions.Redis"/> is set) or an in-memory distributed cache fallback.
    /// </summary>
    /// <remarks>
    /// HybridCache provides stampede-protected <c>GetOrCreateAsync</c>, built-in L1 (in-process)
    /// + L2 (distributed) layering, and logical tag-based invalidation. Consumers should inject
    /// <see cref="HybridCache"/> directly rather than a wrapper interface.
    /// </remarks>
    public static IServiceCollection AddHeroCaching(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CachingOptions>()
            .BindConfiguration(nameof(CachingOptions));

        var cacheOptions = configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>() ?? new CachingOptions();

        // L2: Redis if configured, otherwise in-memory distributed cache.
        if (string.IsNullOrEmpty(cacheOptions.Redis))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                var config = ConfigurationOptions.Parse(cacheOptions.Redis);
                config.AbortOnConnectFail = false;

                // Only override SSL if explicitly configured — respect the connection string default otherwise.
                if (cacheOptions.EnableSsl.HasValue)
                {
                    config.Ssl = cacheOptions.EnableSsl.Value;
                }

                options.ConfigurationOptions = config;
            });
        }

        // HybridCache auto-composes with the registered IDistributedCache above.
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = cacheOptions.DefaultExpiration,
                LocalCacheExpiration = cacheOptions.DefaultLocalCacheExpiration,
            };
            options.MaximumKeyLength = cacheOptions.MaximumKeyLength;
            options.MaximumPayloadBytes = cacheOptions.MaximumPayloadBytes;
        });

        return services;
    }
}
