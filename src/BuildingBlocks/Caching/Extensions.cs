using Microsoft.AspNetCore.DataProtection;
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

        // L2: Redis if configured, in-memory distributed cache otherwise. StackExchangeRedis 9.0+
        // implements IBufferDistributedCache, which HybridCache uses for zero-copy reads.
        if (string.IsNullOrEmpty(cacheOptions.Redis))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            // Connect once and share the multiplexer across the Redis cache, Data Protection key
            // persistence, and future consumers — one connection pool per host, not per feature.
            var redisConfig = ConfigurationOptions.Parse(cacheOptions.Redis);
            redisConfig.AbortOnConnectFail = false;
            if (cacheOptions.EnableSsl.HasValue)
            {
                redisConfig.Ssl = cacheOptions.EnableSsl.Value;
            }
            var sharedMultiplexer = ConnectionMultiplexer.Connect(redisConfig);
            services.AddSingleton<IConnectionMultiplexer>(sharedMultiplexer);

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () =>
                    Task.FromResult<IConnectionMultiplexer>(sharedMultiplexer);
            });

            // Persist Data Protection keys (auth cookies, reset/confirmation tokens, antiforgery) to
            // Redis so multi-instance hosts share a key ring and tokens survive rolling restarts.
            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(sharedMultiplexer, "DataProtection-Keys")
                .SetApplicationName("FSH.Starter");
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

        // Wrap HybridCache with the OTel-emitting decorator: capture the descriptor AddHybridCache
        // installed, remove it, and register a factory that builds the inner and returns our wrapper.
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
