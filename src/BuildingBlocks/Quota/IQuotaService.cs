using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

public interface IQuotaService
{
    /// <summary>
    /// Checks whether <paramref name="amount"/> units of <paramref name="resource"/> would fit within
    /// the tenant's current quota. Does NOT mutate the counter.
    /// </summary>
    ValueTask<QuotaCheckResult> CheckAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default);

    /// <summary>
    /// Increments the counter for <paramref name="resource"/> by <paramref name="amount"/> and
    /// returns the new cumulative usage for the current period. Does not perform limit enforcement.
    /// </summary>
    ValueTask<long> RecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default);

    /// <summary>
    /// Atomically checks and records in one step. If the limit would be exceeded the counter is
    /// not incremented and <see cref="QuotaCheckResult.Allowed"/> is false.
    /// </summary>
    ValueTask<QuotaCheckResult> CheckAndRecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default);

    /// <summary>
    /// Returns the current usage for <paramref name="resource"/> in the active period. For gauge-based
    /// resources this delegates to registered <see cref="IQuotaGaugeProvider"/> instances.
    /// </summary>
    ValueTask<long> GetCurrentAsync(string tenantId, QuotaResource resource, CancellationToken ct = default);
}

/// <summary>
/// Extensibility hook: modules can implement this to report live usage for gauge-based resources
/// (e.g. StorageBytes, Users). The quota service will invoke the provider that matches the requested
/// resource on demand.
/// </summary>
public interface IQuotaGaugeProvider
{
    QuotaResource Resource { get; }
    ValueTask<long> GetCurrentAsync(string tenantId, CancellationToken ct = default);
}
