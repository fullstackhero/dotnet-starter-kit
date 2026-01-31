namespace FSH.Framework.Caching;

/// <summary>
/// Provides caching operations for storing and retrieving items from cache.
/// Supports both synchronous and asynchronous operations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Asynchronously retrieves an item from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The cached item if found; otherwise, null.</returns>
    Task<T?> GetItemAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously stores an item in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="sliding">Optional sliding expiration. Uses default if not specified.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    Task SetItemAsync<T>(string key, T value, TimeSpan? sliding = default, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously removes an item from the cache.
    /// </summary>
    /// <param name="key">The unique cache key to remove.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    Task RemoveItemAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously refreshes the sliding expiration of a cached item.
    /// </summary>
    /// <param name="key">The unique cache key to refresh.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    Task RefreshItemAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an item from the cache synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <returns>The cached item if found; otherwise, null.</returns>
    T? GetItem<T>(string key);

    /// <summary>
    /// Stores an item in the cache synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The unique cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="sliding">Optional sliding expiration. Uses default if not specified.</param>
    void SetItem<T>(string key, T value, TimeSpan? sliding = default);

    /// <summary>
    /// Removes an item from the cache synchronously.
    /// </summary>
    /// <param name="key">The unique cache key to remove.</param>
    void RemoveItem(string key);

    /// <summary>
    /// Refreshes the sliding expiration of a cached item synchronously.
    /// </summary>
    /// <param name="key">The unique cache key to refresh.</param>
    void RefreshItem(string key);
}
