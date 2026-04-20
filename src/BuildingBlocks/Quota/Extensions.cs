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

        services
            .AddOptions<QuotaOptions>()
            .BindConfiguration(nameof(QuotaOptions));

        var quotaOptions = configuration.GetSection(nameof(QuotaOptions)).Get<QuotaOptions>() ?? new QuotaOptions();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(quotaOptions);
        services.AddSingleton<QuotaPlanResolver>();

        if (!quotaOptions.Enabled)
        {
            services.AddSingleton<IQuotaService, NoopQuotaService>();
            services.AddTransient<QuotaEnforcementMiddleware>();
            return services;
        }

        if (!string.IsNullOrWhiteSpace(quotaOptions.Redis))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var config = ConfigurationOptions.Parse(quotaOptions.Redis!);
                config.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(config);
            });

            services.AddSingleton<IQuotaService, RedisQuotaService>();
        }
        else
        {
            services.AddSingleton<IQuotaService, InMemoryQuotaService>();
        }

        services.AddTransient<QuotaEnforcementMiddleware>();

        return services;
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
