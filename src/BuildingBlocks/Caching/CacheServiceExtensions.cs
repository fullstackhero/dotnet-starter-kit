namespace FSH.Framework.Caching;

/// <summary>
/// Extension methods for <see cref="ICacheService"/> providing cache-aside pattern implementations.
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Gets an item from cache, or sets it using the provided callback if not found.
    /// Implements the cache-aside pattern synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The unique cache key.</param>
    /// <param name="getItemCallback">A callback function to retrieve the item if not in cache.</param>
    /// <param name="slidingExpiration">Optional sliding expiration for the cached item.</param>
    /// <returns>The cached item or the newly retrieved and cached item.</returns>
    public static T? GetOrSet<T>(this ICacheService cache, string key, Func<T?> getItemCallback, TimeSpan? slidingExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);

        T? value = cache.GetItem<T>(key);

        if (value is not null)
        {
            return value;
        }

        ArgumentNullException.ThrowIfNull(getItemCallback);
        value = getItemCallback();

        if (value is not null)
        {
            cache.SetItem(key, value, slidingExpiration);
        }

        return value;
    }

    /// <summary>
    /// Asynchronously gets an item from cache, or sets it using the provided task if not found.
    /// Implements the cache-aside pattern asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The unique cache key.</param>
    /// <param name="task">An async function to retrieve the item if not in cache.</param>
    /// <param name="slidingExpiration">Optional sliding expiration for the cached item.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The cached item or the newly retrieved and cached item.</returns>
    public static async Task<T?> GetOrSetAsync<T>(this ICacheService cache, string key, Func<Task<T>> task, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        T? value = await cache.GetItemAsync<T>(key, cancellationToken);

        if (value is not null)
        {
            return value;
        }

        ArgumentNullException.ThrowIfNull(task);
        value = await task();

        if (value is not null)
        {
            await cache.SetItemAsync(key, value, slidingExpiration, cancellationToken);
        }

        return value;
    }
}
