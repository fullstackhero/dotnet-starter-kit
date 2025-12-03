using System.ComponentModel.DataAnnotations.Schema;

namespace FSH.Modules.Multitenancy.Provisioning;

public sealed class TenantProvisioningStep
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProvisioningId { get; private set; }

    public TenantProvisioningStepName Step { get; private set; }

    public TenantProvisioningStatus Status { get; private set; } = TenantProvisioningStatus.Pending;

    public string? Error { get; private set; }

    public DateTime? StartedUtc { get; private set; }

    public DateTime? CompletedUtc { get; private set; }

    [ForeignKey(nameof(ProvisioningId))]
    public TenantProvisioning? Provisioning { get; private set; }

    private TenantProvisioningStep()
    {
    }

    public TenantProvisioningStep(Guid provisioningId, TenantProvisioningStepName step)
    {
        ProvisioningId = provisioningId;
        Step = step;
    }

    public void MarkRunning()
    {
        Status = TenantProvisioningStatus.Running;
        StartedUtc ??= DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = TenantProvisioningStatus.Completed;
        CompletedUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = TenantProvisioningStatus.Failed;
        Error = error;
        CompletedUtc = DateTime.UtcNow;
    }
}
