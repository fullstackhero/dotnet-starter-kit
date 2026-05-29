using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Framework.Caching;

/// <summary>
/// Tenant-scoped wrapper over <see cref="HybridCache"/>.
/// All keys and tags are automatically prefixed with the current tenant identifier,
/// preventing cross-tenant cache collisions without requiring callers to manage
/// the prefix themselves.
/// </summary>
/// <remarks>
/// Register as <c>Scoped</c> because the tenant context (and therefore the prefix)
/// changes per HTTP request. Inject <see cref="ITenantCacheService"/> in module
/// code — do NOT inject <see cref="HybridCache"/> directly from business modules.
/// </remarks>
public interface ITenantCacheService
{
    /// <summary>
    /// Gets an existing cache entry or creates a new one using the provided factory,
    /// automatically scoping the key and tags to the current tenant.
    /// </summary>
    ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cache entry for the given key, scoped to the current tenant.
    /// </summary>
    ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cache entry by key, scoped to the current tenant.
    /// </summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries carrying the given tag, scoped to the current tenant.
    /// </summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries carrying any of the given tags, scoped to the current tenant.
    /// </summary>
    ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}
