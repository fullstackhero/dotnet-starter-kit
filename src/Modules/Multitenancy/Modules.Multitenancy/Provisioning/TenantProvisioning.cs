namespace FSH.Modules.Multitenancy.Provisioning;

public sealed class TenantProvisioning
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string TenantId { get; private set; } = default!;

    public string CorrelationId { get; private set; } = default!;

    public TenantProvisioningStatus Status { get; private set; } = TenantProvisioningStatus.Pending;

    public string? CurrentStep { get; private set; }

    public string? Error { get; private set; }

    public string? JobId { get; private set; }

    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? StartedUtc { get; private set; }

    public DateTime? CompletedUtc { get; private set; }

    public ICollection<TenantProvisioningStep> Steps { get; private set; } = new List<TenantProvisioningStep>();

    private TenantProvisioning()
    {
    }

    public TenantProvisioning(string tenantId, string correlationId)
    {
        TenantId = tenantId;
        CorrelationId = correlationId;
        CreatedUtc = DateTime.UtcNow;
    }

    public void SetJobId(string jobId) => JobId = jobId;

    public void MarkRunning(string step)
    {
        Status = TenantProvisioningStatus.Running;
        StartedUtc ??= DateTime.UtcNow;
        CurrentStep = step;
    }

    public void MarkCompleted()
    {
        Status = TenantProvisioningStatus.Completed;
        CompletedUtc = DateTime.UtcNow;
        CurrentStep = null;
        Error = null;
    }

    public void MarkFailed(string step, string error)
    {
        Status = TenantProvisioningStatus.Failed;
        CurrentStep = step;
        Error = error;
        CompletedUtc = DateTime.UtcNow;
    }
}
