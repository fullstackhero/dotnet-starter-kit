using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Framework.Caching;

/// <summary>
/// <see cref="IGlobalCacheService"/> implementation that delegates directly to
/// <see cref="HybridCache"/> without any tenant scoping.
/// Registered as <c>Singleton</c> — safe because no per-request state is involved.
/// </summary>
internal sealed class GlobalHybridCache : IGlobalCacheService
{
    private readonly HybridCache _cache;

    public GlobalHybridCache(HybridCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    /// <inheritdoc/>
    public ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(key, state, factory, options, tags, cancellationToken);

    /// <inheritdoc/>
    public ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => _cache.SetAsync(key, value, options, tags, cancellationToken);

    /// <inheritdoc/>
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(key, cancellationToken);

    /// <inheritdoc/>
    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        => _cache.RemoveByTagAsync(tag, cancellationToken);
}
