using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace FSH.Modules.Multitenancy.Provisioning;

public interface ITenantProvisioningService
{
    Task<TenantProvisioning> StartAsync(string tenantId, CancellationToken cancellationToken);

    Task<TenantProvisioning?> GetLatestAsync(string tenantId, CancellationToken cancellationToken);

    Task<TenantProvisioningStatusDto> GetStatusAsync(string tenantId, CancellationToken cancellationToken);

    Task EnsureCanActivateAsync(string tenantId, CancellationToken cancellationToken);

    Task<string> RetryAsync(string tenantId, CancellationToken cancellationToken);

    Task<bool> MarkRunningAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken);

    Task MarkStepCompletedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken);

    Task MarkFailedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, string error, CancellationToken cancellationToken);

    Task MarkCompletedAsync(string tenantId, string correlationId, CancellationToken cancellationToken);
}
