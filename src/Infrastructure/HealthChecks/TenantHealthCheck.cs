using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.HealthChecks
{
    public class TenantHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Descoped
            var check = new HealthCheckResult(HealthStatus.Healthy);
            return await Task.FromResult<HealthCheckResult>(check);
        }
    }
}