using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FSH.Framework.Shared.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Web.RateLimiting;

public static class Extensions
{
    private const string HealthPartitionKey = "health";
    private const string AnonymousPartitionKey = "anonymous";
    private const string UnknownIpPartitionKey = "ip:unknown";
    private const string TooManyRequestsType = "https://datatracker.ietf.org/doc/html/rfc6585#section-4";

    public static IServiceCollection AddHeroRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.GetSection(nameof(RateLimitingOptions)).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(nameof(RateLimitingOptions));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejected;

            if (!settings.Enabled)
            {
                return;
            }

            options.GlobalLimiter = CreateChainedGlobalLimiter(settings);
            AddAuthPolicy(options, settings);
        });

        return services;
    }

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

    private static PartitionedRateLimiter<HttpContext> CreateChainedGlobalLimiter(RateLimitingOptions settings) =>
        PartitionedRateLimiter.CreateChained(
            CreateTenantLimiter(settings),
            CreateUserLimiter(settings),
            CreateIpLimiter(settings));

    private static PartitionedRateLimiter<HttpContext> CreateTenantLimiter(RateLimitingOptions settings) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (IsHealthPath(context.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter(HealthPartitionKey);
            }

            var tenant = context.User?.FindFirst(ClaimConstants.Tenant)?.Value;
            return string.IsNullOrWhiteSpace(tenant)
                ? RateLimitPartition.GetNoLimiter(AnonymousPartitionKey)
                : CreateFixedWindowPartition($"tenant:{tenant}", settings.Tenant);
        });

    private static PartitionedRateLimiter<HttpContext> CreateUserLimiter(RateLimitingOptions settings) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (IsHealthPath(context.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter(HealthPartitionKey);
            }

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrWhiteSpace(userId)
                ? RateLimitPartition.GetNoLimiter(AnonymousPartitionKey)
                : CreateFixedWindowPartition($"user:{userId}", settings.User);
        });

    private static PartitionedRateLimiter<HttpContext> CreateIpLimiter(RateLimitingOptions settings) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (IsHealthPath(context.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter(HealthPartitionKey);
            }

            var ip = context.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrWhiteSpace(ip)
                ? RateLimitPartition.GetNoLimiter(UnknownIpPartitionKey)
                : CreateFixedWindowPartition($"ip:{ip}", settings.Ip);
        });

    private static void AddAuthPolicy(RateLimiterOptions options, RateLimitingOptions settings)
    {
        options.AddPolicy<string>("auth", context =>
            CreateFixedWindowPartition(GetAuthPartitionKey(context), settings.Auth));
    }

    private static string GetAuthPartitionKey(HttpContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? UnknownIpPartitionKey : $"ip:{ip}";
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(string partitionKey, FixedWindowPolicyOptions policy) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = policy.QueueLimit
            });

    private static bool IsHealthPath(PathString path) =>
        path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/ready", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/live", StringComparison.OrdinalIgnoreCase);

    private static async ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please retry later.",
            Type = TooManyRequestsType,
            Instance = httpContext.Request.Path
        };

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        problem.Extensions["traceId"] = traceId;

        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? httpContext.TraceIdentifier;
        problem.Extensions["correlationId"] = correlationId;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
    }
}
