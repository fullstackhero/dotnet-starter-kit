using System.Collections.Concurrent;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

/// <summary>
/// Per-process quota counter. Suitable for development and tests; not shared across instances so
/// limits are applied independently per host. In multi-node deployments configure Redis instead.
/// </summary>
public sealed class InMemoryQuotaService : IQuotaService
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly QuotaOptions _options;
    private readonly QuotaPlanResolver _planResolver;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? _tenantAccessor;
    private readonly IReadOnlyDictionary<QuotaResource, IQuotaGaugeProvider> _gauges;
    private readonly TimeProvider _timeProvider;

    public InMemoryQuotaService(
        QuotaOptions options,
        QuotaPlanResolver planResolver,
        IEnumerable<IQuotaGaugeProvider> gauges,
        TimeProvider timeProvider,
        IMultiTenantContextAccessor<AppTenantInfo>? tenantAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(planResolver);
        ArgumentNullException.ThrowIfNull(gauges);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options;
        _planResolver = planResolver;
        _timeProvider = timeProvider;
        _tenantAccessor = tenantAccessor;
        _gauges = gauges.ToDictionary(g => g.Resource);
    }

    public ValueTask<QuotaCheckResult> CheckAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var (limit, exempt) = ResolveLimit(tenantId, resource);
        var current = GetCounter(tenantId, resource);

        if (exempt || limit == long.MaxValue)
        {
            return ValueTask.FromResult(QuotaCheckResult.Unlimited(resource, current));
        }

        var allowed = current + amount <= limit;
        return ValueTask.FromResult(new QuotaCheckResult(allowed, resource, current, limit, GetPeriodResetUtc(resource)));
    }

    public ValueTask<long> RecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (!IsCounterResource(resource))
        {
            return GetCurrentAsync(tenantId, resource, ct);
        }

        var key = BuildCounterKey(tenantId, resource);
        var updated = _counters.AddOrUpdate(key, amount, (_, v) => v + amount);
        return ValueTask.FromResult(updated);
    }

    public async ValueTask<QuotaCheckResult> CheckAndRecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var (limit, exempt) = ResolveLimit(tenantId, resource);

        if (exempt || limit == long.MaxValue)
        {
            var after = await RecordAsync(tenantId, resource, amount, ct).ConfigureAwait(false);
            return QuotaCheckResult.Unlimited(resource, after);
        }

        if (!IsCounterResource(resource))
        {
            return await CheckAsync(tenantId, resource, amount, ct).ConfigureAwait(false);
        }

        var key = BuildCounterKey(tenantId, resource);
        var newValue = _counters.AddOrUpdate(key, amount, (_, v) => v + amount);

        if (newValue <= limit)
        {
            return new QuotaCheckResult(true, resource, newValue, limit, GetPeriodResetUtc(resource));
        }

        _counters.AddOrUpdate(key, 0, (_, v) => v - amount);
        return new QuotaCheckResult(false, resource, newValue - amount, limit, GetPeriodResetUtc(resource));
    }

    public ValueTask<long> GetCurrentAsync(string tenantId, QuotaResource resource, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!IsCounterResource(resource))
        {
            if (_gauges.TryGetValue(resource, out var provider))
            {
                return provider.GetCurrentAsync(tenantId, ct);
            }

            return ValueTask.FromResult(0L);
        }

        return ValueTask.FromResult(GetCounter(tenantId, resource));
    }

    private long GetCounter(string tenantId, QuotaResource resource)
    {
        return _counters.TryGetValue(BuildCounterKey(tenantId, resource), out var value) ? value : 0;
    }

    private (long Limit, bool Exempt) ResolveLimit(string tenantId, QuotaResource resource)
    {
        if (_options.ExemptRootTenant && string.Equals(tenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
        {
            return (long.MaxValue, true);
        }

        var tenant = _tenantAccessor?.MultiTenantContext?.TenantInfo;
        if (tenant is not null && !string.Equals(tenant.Id, tenantId, StringComparison.Ordinal))
        {
            tenant = null;
        }

        return (_planResolver.ResolveLimit(tenant, resource), false);
    }

    private static bool IsCounterResource(QuotaResource resource) => resource switch
    {
        QuotaResource.ApiCalls => true,
        _ => false
    };

    private string BuildCounterKey(string tenantId, QuotaResource resource)
    {
        var now = _timeProvider.GetUtcNow();
        var period = $"{now.Year:D4}{now.Month:D2}";
        return $"quota:{tenantId}:{resource}:{period}";
    }

    private DateTimeOffset? GetPeriodResetUtc(QuotaResource resource)
    {
        if (!IsCounterResource(resource))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        return now.Month == 12
            ? new DateTimeOffset(now.Year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(now.Year, now.Month + 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
