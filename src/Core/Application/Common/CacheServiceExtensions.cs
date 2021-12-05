using System.Text;
using DN.WebApi.Application.Common.Interfaces;

namespace DN.WebApi.Application.Common;

public static class CacheServiceExtensions
{
    public static byte[]? GetOrSet(this ICacheService cache, string key, Func<byte[]> getItemCallback, TimeSpan? slidingExpiration = null)
    {
        byte[]? value = cache.Get(key);

        if (value is not null)
        {
            return value;
        }

        value = getItemCallback();

        if (value is not null)
        {
            cache.Set(key, value, slidingExpiration);
        }

        return value;
    }

    public static async Task<byte[]?> GetOrSetAsync(this ICacheService cache, string key, Func<Task<byte[]>> getItemCallback, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
    {
        byte[]? value = await cache.GetAsync(key, cancellationToken);

        if (value is not null)
        {
            return value;
        }

        value = await getItemCallback();

        if (value is not null)
        {
            await cache.SetAsync(key, value, slidingExpiration, cancellationToken);
        }

        return value;
    }

    public static void SetString(this ICacheService cache, string key, string value, TimeSpan? slidingEpiration = null)
    {
        _ = key ?? throw new ArgumentNullException(nameof(key));
        _ = value ?? throw new ArgumentNullException(nameof(value));

        cache.Set(key, Encoding.UTF8.GetBytes(value), slidingEpiration);
    }

    public static Task SetStringAsync(this ICacheService cache, string key, string value, TimeSpan? slidingEpiration = null, in CancellationToken token = default)
    {
        _ = key ?? throw new ArgumentNullException(nameof(key));
        _ = value ?? throw new ArgumentNullException(nameof(value));

        return cache.SetAsync(key, Encoding.UTF8.GetBytes(value), slidingEpiration, token);
    }

    public static string? GetString(this ICacheService cache, string key)
    {
        byte[]? data = cache.Get(key);
        if (data == null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(data, 0, data.Length);
    }

    public static async Task<string?> GetStringAsync(this ICacheService cache, string key, CancellationToken token = default)
    {
        byte[]? data = await cache.GetAsync(key, token).ConfigureAwait(false);
        if (data == null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(data, 0, data.Length);
    }

    public static string? GetOrSetString(this ICacheService cache, string key, Func<string?> getStringCallback, TimeSpan? slidingExpiration = null)
    {
        string? value = cache.GetString(key);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        value = getStringCallback();

        if (!string.IsNullOrEmpty(value))
        {
            cache.SetString(key, value, slidingExpiration);
        }

        return value;
    }

    public static async Task<string?> GetOrSetStringAsync(this ICacheService cache, string key, Func<Task<string?>> getStringCallback, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
    {
        string? value = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        value = await getStringCallback();

        if (!string.IsNullOrEmpty(value))
        {
            await cache.SetStringAsync(key, value, slidingExpiration, cancellationToken);
        }

        return value;
    }
}