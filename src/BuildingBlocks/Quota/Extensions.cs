using FSH.Framework.Core.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FSH.Framework.Quota;

public static class Extensions
{
    /// <summary>
    /// Registers the quota service. Uses Redis-backed counters when <see cref="QuotaOptions.Redis"/>
    /// is configured; otherwise falls back to a per-process in-memory counter (development/tests
    /// only — not shared across instances).
    /// </summary>
    public static IServiceCollection AddHeroQuotas(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // The KeyPrefix fallback is applied to BOTH views of the options — the bound IOptions and the
        // singleton the quota services take — so they never disagree.
        services
            .AddOptions<QuotaOptions>()
            .BindConfiguration(nameof(QuotaOptions))
            .PostConfigure(options => InheritCachingKeyPrefix(options, configuration));

        var quotaOptions = configuration.GetSection(nameof(QuotaOptions)).Get<QuotaOptions>() ?? new QuotaOptions();
        InheritCachingKeyPrefix(quotaOptions, configuration);

        // Fail at startup, not on the first quota check, when the prefix is not a valid Redis key prefix.
        _ = RedisKeyPrefix.Normalize(quotaOptions.KeyPrefix);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(quotaOptions);
        services.AddSingleton<QuotaPlanResolver>();

        if (!quotaOptions.Enabled)
        {
            services.AddScoped<IQuotaService, NoopQuotaService>();
            services.AddTransient<QuotaEnforcementMiddleware>();
            return services;
        }

        if (!string.IsNullOrWhiteSpace(quotaOptions.Redis))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var config = ConfigurationOptions.Parse(quotaOptions.Redis);
                config.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(config);
            });

            // Scoped so gauge providers with scoped dependencies (e.g. DbContext) resolve per request.
            services.AddScoped<IQuotaService, RedisQuotaService>();
        }
        else
        {
            services.AddSingleton<InMemoryQuotaStore>();
            services.AddScoped<IQuotaService, InMemoryQuotaService>();
        }

        services.AddTransient<QuotaEnforcementMiddleware>();

        return services;
    }

    /// <summary>
    /// Quota counters live under <c>CachingOptions:KeyPrefix</c> unless <c>QuotaOptions:KeyPrefix</c>
    /// overrides it (quota may point at its own Redis).
    /// </summary>
    private static void InheritCachingKeyPrefix(QuotaOptions options, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(options.KeyPrefix))
        {
            options.KeyPrefix = configuration[RedisKeyPrefix.ConfigurationKey];
        }
    }

    /// <summary>
    /// Inserts the quota enforcement middleware into the pipeline. Must run after authentication
    /// (so we know the tenant) and after the rate limiter (so rate-limited requests don't burn
    /// quota). The middleware no-ops when <see cref="QuotaOptions.Enabled"/> is false.
    /// </summary>
    public static IApplicationBuilder UseHeroQuotas(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<QuotaEnforcementMiddleware>();
    }
}
