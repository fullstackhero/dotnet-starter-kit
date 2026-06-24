namespace FSH.Modules.Multitenancy.Contracts.Dtos;

public sealed class TenantStatusDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTime ValidUpto { get; init; }
    public bool HasConnectionString { get; init; }
    public string AdminEmail { get; init; } = default!;
    public string? Issuer { get; init; }

    /// <summary>The tenant's current billing plan key (drives quotas + subscription).</summary>
    public string? Plan { get; init; }

    /// <summary>Derived lifecycle state: "Active", "InGrace", or "Expired".</summary>
    public string ExpiryState { get; init; } = "Active";

    /// <summary>Instant after which a lapsed tenant is hard-blocked (ValidUpto + grace period).</summary>
    public DateTime GraceEndsUtc { get; init; }
}