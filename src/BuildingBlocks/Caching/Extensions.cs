using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FSH.Framework.Caching;

/// <summary>
/// Extension methods for registering caching services in the dependency injection container.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds FullStackHero caching services to the service collection.
    /// Configures a hybrid L1/L2 cache with in-memory (L1) and Redis or distributed memory (L2).
    /// </summary>
    /// <param name="services">The service collection to add caching services to.</param>
    /// <param name="configuration">The application configuration containing caching options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// If Redis connection string is configured in <see cref="CachingOptions"/>, Redis is used for L2 cache.
    /// Otherwise, falls back to in-memory distributed cache for L2.
    /// The <see cref="HybridCacheService"/> is registered to provide both sync and async cache operations.
    /// </remarks>
    public static IServiceCollection AddHeroCaching(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CachingOptions>()
            .BindConfiguration(nameof(CachingOptions));

        // Always add memory cache for L1
        services.AddMemoryCache();

        var cacheOptions = configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>();
        if (cacheOptions == null || string.IsNullOrEmpty(cacheOptions.Redis))
        {
            // If no Redis, use memory cache for L2 as well
            services.AddDistributedMemoryCache();
            services.AddTransient<ICacheService, HybridCacheService>();
            return services;
        }

        // Use Redis for L2 cache
        services.AddStackExchangeRedisCache(options =>
        {
            var config = ConfigurationOptions.Parse(cacheOptions.Redis);
            config.AbortOnConnectFail = true;

            // Only override SSL if explicitly configured
            if (cacheOptions.EnableSsl.HasValue)
            {
                config.Ssl = cacheOptions.EnableSsl.Value;
            }

            options.ConfigurationOptions = config;
        });

        // Register hybrid cache service
        services.AddTransient<ICacheService, HybridCacheService>();

        return services;
    }
}
