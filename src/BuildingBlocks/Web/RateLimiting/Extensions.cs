using FSH.Framework.Shared.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace FSH.Framework.Web.RateLimiting;

public static class Extensions
{
    public static IServiceCollection AddHeroRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.GetSection(nameof(RateLimitingOptions)).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(nameof(RateLimitingOptions));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            if (!settings.Enabled)
            {
                return;
            }

            options.GlobalLimiter = CreateGlobalLimiter(settings);
            AddGlobalPolicy(options, settings);
            AddAuthPolicy(options, settings);
        });

        return services;
    }

    private static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter(RateLimitingOptions settings)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (IsHealthPath(context.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter("health");
            }

            var key = GetPartitionKey(context);
            return CreateFixedWindowPartition(key, settings.Global);
        });
    }

    private static void AddGlobalPolicy(Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options, RateLimitingOptions settings)
    {
        options.AddPolicy<string>("global", context =>
            CreateFixedWindowPartition(GetPartitionKey(context), settings.Global));
    }

    private static void AddAuthPolicy(Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options, RateLimitingOptions settings)
    {
        options.AddPolicy<string>("auth", context =>
            CreateFixedWindowPartition(GetPartitionKey(context), settings.Auth));
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(string partitionKey, FixedWindowPolicyOptions policy)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = policy.QueueLimit
            });
    }

    private static string GetPartitionKey(HttpContext context)
    {
        var tenant = context.User?.FindFirst(ClaimConstants.Tenant)?.Value;
        if (!string.IsNullOrWhiteSpace(tenant))
        {
            return $"tenant:{tenant}";
        }

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? "ip:unknown" : $"ip:{ip}";
    }

    private static bool IsHealthPath(PathString path) =>
        path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/ready", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/live", StringComparison.OrdinalIgnoreCase);

    public static IApplicationBuilder UseHeroRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var opts = app.ApplicationServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
        if (opts.Enabled)
        {
            app.UseRateLimiter();
        }
        return app;
    }
}
