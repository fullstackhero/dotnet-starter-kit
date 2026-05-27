using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FSH.Framework.Web.Health;

/// <summary>
/// Health check that verifies Hangfire storage is accessible.
/// </summary>
public sealed class HangfireHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var storage = JobStorage.Current;
            using var connection = storage.GetConnection();
            return Task.FromResult(HealthCheckResult.Healthy("Hangfire storage is accessible."));
        }
#pragma warning disable CA1031 // Health checks must catch all exceptions to report degraded/unhealthy
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is not accessible.", ex));
        }
#pragma warning restore CA1031
    }
}
