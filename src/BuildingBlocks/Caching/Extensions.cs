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
    /// <see cref="CachingOptions.Redis"/> is set) or an in-memory distributed cache fallback,
    /// then wraps it with <see cref="ObservableHybridCache"/> so every operation emits OTel
    /// metrics and activities via <see cref="Telemetry.CachingTelemetry"/>.
    /// </summary>
    /// <remarks>
    /// HybridCache provides stampede-protected <c>GetOrCreateAsync</c>, built-in L1 (in-process)
    /// + L2 (distributed) layering, and logical tag-based invalidation. Consumers inject
    /// <see cref="HybridCache"/> directly — the decorator is transparent.
    /// </remarks>
    public static IServiceCollection AddHeroCaching(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CachingOptions>()
            .BindConfiguration(nameof(CachingOptions));

        var cacheOptions = configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>() ?? new CachingOptions();

        // L2: Redis if configured, in-memory distributed cache otherwise.
        // Note: Microsoft.Extensions.Caching.StackExchangeRedis 9.0+ implements
        // IBufferDistributedCache, which HybridCache uses for zero-copy reads.
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

        // HybridCache auto-composes with whatever IDistributedCache is registered above.
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = cacheOptions.DefaultExpiration,            // L1 + L2 total lifetime
                LocalCacheExpiration = cacheOptions.DefaultLocalCacheExpiration, // L1 only
            };
            options.MaximumKeyLength = cacheOptions.MaximumKeyLength;
            options.MaximumPayloadBytes = cacheOptions.MaximumPayloadBytes;
        });

        // Wrap HybridCache with the OTel-emitting decorator. We capture the existing descriptor
        // (a DefaultHybridCache factory installed by AddHybridCache), remove it, and register a
        // factory that builds the inner via the original descriptor and returns our wrapper.
        DecorateHybridCache(services);

        return services;
    }

    private static void DecorateHybridCache(IServiceCollection services)
    {
        var originalDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(HybridCache))
            ?? throw new InvalidOperationException("HybridCache is not registered. AddHybridCache must be called before DecorateHybridCache.");

        services.Remove(originalDescriptor);

        services.AddSingleton<HybridCache>(sp =>
        {
            HybridCache inner;
            if (originalDescriptor.ImplementationInstance is HybridCache instance)
            {
                inner = instance;
            }
            else if (originalDescriptor.ImplementationFactory is { } factory)
            {
                inner = (HybridCache)factory(sp);
            }
            else
            {
                inner = (HybridCache)ActivatorUtilities.CreateInstance(sp, originalDescriptor.ImplementationType!);
            }

            return new ObservableHybridCache(inner);
        });
    }
}
