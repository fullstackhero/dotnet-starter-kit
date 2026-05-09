using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

/// <summary>
/// Used when quota enforcement is disabled via configuration. Every check returns allowed with
/// an unlimited result so calling code remains unchanged.
/// </summary>
public sealed class NoopQuotaService : IQuotaService
{
    public ValueTask<QuotaCheckResult> CheckAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
        => ValueTask.FromResult(QuotaCheckResult.Unlimited(resource, 0));

    public ValueTask<long> RecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
        => ValueTask.FromResult(0L);

    public ValueTask<QuotaCheckResult> CheckAndRecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
        => ValueTask.FromResult(QuotaCheckResult.Unlimited(resource, 0));

    public ValueTask<long> GetCurrentAsync(string tenantId, QuotaResource resource, CancellationToken ct = default)
        => ValueTask.FromResult(0L);
}
