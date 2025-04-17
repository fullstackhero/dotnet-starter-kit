namespace FSH.Modules.Common.Core.Caching;

public static class CacheServiceExtensions
{
    public static T? GetOrSet<T>(this ICacheService cache, string key, Func<T?> getItemCallback, TimeSpan? slidingExpiration = null)
    {
        T? value = cache.GetItem<T>(key);

        if (value is not null)
        {
            return value;
        }

        value = getItemCallback();

        if (value is not null)
        {
            cache.SetItem(key, value, slidingExpiration);
        }

        return value;
    }

    public static async Task<T?> GetOrSetAsync<T>(this ICacheService cache, string key, Func<Task<T>> task, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
    {
        T? value = await cache.GetItemAsync<T>(key, cancellationToken);

        if (value is not null)
        {
            return value;
        }

        value = await task();

        if (value is not null)
        {
            await cache.SetItemAsync(key, value, slidingExpiration, cancellationToken);
        }

        return value;
    }
}