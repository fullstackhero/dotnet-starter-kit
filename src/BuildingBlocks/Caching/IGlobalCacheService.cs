using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Framework.Caching;

/// <summary>
/// Cross-tenant cache service for entries that are intentionally shared across all tenants.
/// Examples: system defaults, global configuration, shared lookup tables.
/// </summary>
/// <remarks>
/// Use this service ONLY for data that is genuinely identical for every tenant.
/// For any per-tenant data use <see cref="ITenantCacheService"/> instead.
/// Registered as <c>Singleton</c> because there is no per-request tenant context involved.
/// </remarks>
public interface IGlobalCacheService
{
    /// <summary>Gets or creates a cross-tenant cache entry.</summary>
    ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sets a cross-tenant cache entry.</summary>
    ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a cross-tenant cache entry by key.</summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes all cross-tenant entries tagged with the given tag.</summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
