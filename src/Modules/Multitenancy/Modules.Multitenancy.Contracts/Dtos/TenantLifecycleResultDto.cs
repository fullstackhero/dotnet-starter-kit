namespace FSH.Modules.Multitenancy.Contracts.Dtos;

public sealed class TenantLifecycleResultDto
{
    public string TenantId { get; set; } = default!;

    public bool IsActive { get; set; }

    public DateTime? ValidUpto { get; set; }

    public string Message { get; set; } = string.Empty;
}