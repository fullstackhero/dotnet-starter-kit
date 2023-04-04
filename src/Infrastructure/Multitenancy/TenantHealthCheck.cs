using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public class TenantHealthCheck : IHealthCheck
{
    private readonly ILogger<TenantHealthCheck> _logger;

    public TenantHealthCheck(ILogger<TenantHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var check = new HealthCheckResult(HealthStatus.Healthy);
        _logger.LogInformation($"Status is {check.Status}");
        return Task.FromResult(check);
    }
}