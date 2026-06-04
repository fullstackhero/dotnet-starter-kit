namespace FSH.Modules.Multitenancy.Domain;

/// <summary>
/// Dedup ledger for expiry notifications. The daily scan records one row per
/// (tenant, notice type, validity period) so a tenant is notified once per state per validity window —
/// the row re-arms automatically when <c>ValidUpto</c> changes on renewal. Lives in the cross-tenant
/// <c>TenantDbContext</c> (not tenant-filtered), so the background scan can read/write it without a
/// tenant context.
/// </summary>
public sealed class TenantExpiryNotice
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public string NoticeType { get; private set; } = default!;
    public DateTime ValidUptoUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private TenantExpiryNotice()
    {
    }

    public static TenantExpiryNotice Record(string tenantId, string noticeType, DateTime validUptoUtc, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(noticeType);

        return new TenantExpiryNotice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NoticeType = noticeType,
            ValidUptoUtc = DateTime.SpecifyKind(validUptoUtc, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
        };
    }
}

/// <summary>Stable string keys for <see cref="TenantExpiryNotice.NoticeType"/>.</summary>
public static class TenantExpiryNoticeTypes
{
    public const string NearingExpiry = "NearingExpiry";
    public const string EnteredGrace = "EnteredGrace";
    public const string Expired = "Expired";
}
