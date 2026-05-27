using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FSH.Framework.Web.Health;

/// <summary>
/// Health check that verifies Redis connectivity by performing a round-trip set/remove.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            const string key = "__health_check__";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            };
            await _cache.SetStringAsync(key, "ok", options, cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Redis is accessible.");
        }
#pragma warning disable CA1031 // Health checks must catch all exceptions to report degraded/unhealthy
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is not accessible.", ex);
        }
#pragma warning restore CA1031
    }
}
