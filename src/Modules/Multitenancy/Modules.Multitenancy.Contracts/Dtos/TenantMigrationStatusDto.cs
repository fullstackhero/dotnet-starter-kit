namespace FSH.Modules.Multitenancy.Contracts.Dtos;

public sealed class TenantMigrationStatusDto
{
    public string TenantId { get; set; } = default!;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime? ValidUpto { get; set; }

    public bool HasPendingMigrations { get; set; }

    public string? Provider { get; set; }

    public string? LastAppliedMigration { get; set; }

    public IReadOnlyCollection<string> PendingMigrations { get; set; } = Array.Empty<string>();

    public string? Error { get; set; }
}