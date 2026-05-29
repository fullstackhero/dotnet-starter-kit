using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Framework.Caching;

/// <summary>
/// <see cref="ITenantCacheService"/> implementation that wraps <see cref="HybridCache"/>
/// and automatically scopes every key and tag with the active tenant identifier.
/// </summary>
/// <remarks>
/// This class must be registered as <c>Scoped</c> so that each HTTP request gets
/// a fresh instance bound to the correct tenant context. The underlying
/// <see cref="HybridCache"/> remains <c>Singleton</c> and is shared across tenants —
/// only the key/tag prefixing is tenant-specific.
/// </remarks>
internal sealed class TenantHybridCache : ITenantCacheService
{
    private readonly HybridCache _cache;
    private readonly IMultiTenantContextAccessor _tenantAccessor;

    public TenantHybridCache(HybridCache cache, IMultiTenantContextAccessor tenantAccessor)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(tenantAccessor);
        _cache = cache;
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>
    /// Internal factory for test projects (via InternalsVisibleTo). Prefer DI for production code.
    /// </summary>
    internal static ITenantCacheService Create(HybridCache cache, IMultiTenantContextAccessor accessor)
        => new TenantHybridCache(cache, accessor);

    // -----------------------------------------------------------------------
    // Key/tag scoping helpers
    // -----------------------------------------------------------------------

    private string GetTenantId()
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException(
                "No active tenant context. TenantHybridCache requires a resolved tenant. " +
                "Ensure the request passes through the Finbuckle middleware before the cache is accessed.");
        }

        return tenantId;
    }

    /// <summary>Scopes a logical key to the current tenant: <c>t:{tenantId}:{key}</c>.</summary>
    private string ScopeKey(string key) => $"t:{GetTenantId()}:{key}";

    /// <summary>
    /// Returns the full tag set for a cache entry:
    /// <list type="bullet">
    ///   <item><c>tenant:{tenantId}</c> — whole-tenant purge tag (used by <see cref="CacheKeys.Tags.Tenant"/>).</item>
    ///   <item><c>t:{tenantId}:{callerTag}</c> for every caller-supplied tag — scoped so
    ///     <see cref="RemoveByTagAsync(string, CancellationToken)"/> can look up the same
    ///     prefixed tag and actually find the stored entries.</item>
    /// </list>
    /// Without the per-tag prefix the SET and REMOVE paths would use different tag strings,
    /// making all tag-based invalidation a silent no-op.
    /// </summary>
    private static IEnumerable<string> ScopeTags(string tenantId, IEnumerable<string>? callerTags)
    {
        // Whole-tenant purge tag — lets callers blow away an entire tenant's cache.
        yield return CacheKeys.Tags.Tenant(tenantId);

        if (callerTags is null) yield break;

        // Per-tag scoped form — must match the lookup key built in RemoveByTagAsync.
        foreach (var tag in callerTags)
            yield return $"t:{tenantId}:{tag}";
    }

    // -----------------------------------------------------------------------
    // ITenantCacheService implementation
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        return _cache.GetOrCreateAsync(
            ScopeKey(key),
            state,
            factory,
            options,
            ScopeTags(tenantId, tags),
            cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        return _cache.SetAsync(
            ScopeKey(key),
            value,
            options,
            ScopeTags(tenantId, tags),
            cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(ScopeKey(key), cancellationToken);

    /// <inheritdoc/>
    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        // Scope the tag so only entries for this tenant are evicted.
        return _cache.RemoveByTagAsync($"t:{tenantId}:{tag}", cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        return _cache.RemoveByTagAsync(tags.Select(t => $"t:{tenantId}:{t}"), cancellationToken);
    }
}
