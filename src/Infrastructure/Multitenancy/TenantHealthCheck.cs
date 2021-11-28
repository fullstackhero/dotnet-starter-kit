using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class TenantHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Descoped
        var check = new HealthCheckResult(HealthStatus.Healthy);
        return await Task.FromResult<HealthCheckResult>(check);
    }
}