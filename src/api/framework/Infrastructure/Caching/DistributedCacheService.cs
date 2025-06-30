using System.Text;
using System.Text.Json;
using FSH.Framework.Core.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Caching;

public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        (_cache, _logger) = (cache, logger);
    }

    public T? Get<T>(string key) =>
        Get(key) is { } data
            ? Deserialize<T>(data)
            : default;

    private byte[]? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        try
        {
            return _cache.Get(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in Get for key {Key}", key);
            return null;
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default) =>
        await GetAsync(key, token) is { } data
            ? Deserialize<T>(data)
            : default;

    private async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try
        {
            return await _cache.GetAsync(key, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in GetAsync for key {Key}", key);
            return null;
        }
    }

    public void Refresh(string key)
    {
        try
        {
            _cache.Refresh(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in Refresh for key {Key}", key);
        }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        try
        {
            await _cache.RefreshAsync(key, token);
            _logger.LogDebug("refreshed cache with key : {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in RefreshAsync for key {Key}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in Remove for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            await _cache.RemoveAsync(key, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in RemoveAsync for key {Key}", key);
        }
    }

    public void Set<T>(string key, T value, TimeSpan? slidingExpiration = null) =>
        Set(key, Serialize(value), slidingExpiration);

    private void Set(string key, byte[] value, TimeSpan? slidingExpiration = null)
    {
        try
        {
            _cache.Set(key, value, GetOptions(slidingExpiration));
            _logger.LogDebug("cached data with key : {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in Set for key {Key}", key);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default) =>
        SetAsync(key, Serialize(value), slidingExpiration, cancellationToken);

    private async Task SetAsync(string key, byte[] value, TimeSpan? slidingExpiration = null, CancellationToken token = default)
    {
        try
        {
            await _cache.SetAsync(key, value, GetOptions(slidingExpiration), token);
            _logger.LogDebug("cached data with key : {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in SetAsync for key {Key}", key);
        }
    }

    private static byte[] Serialize<T>(T item)
    {
        return Encoding.Default.GetBytes(JsonSerializer.Serialize(item));
    }

    private static T Deserialize<T>(byte[] cachedData)
    {
        return JsonSerializer.Deserialize<T>(Encoding.Default.GetString(cachedData))!;
    }

    private static DistributedCacheEntryOptions GetOptions(TimeSpan? slidingExpiration)
    {
        var options = new DistributedCacheEntryOptions();
        if (slidingExpiration.HasValue)
        {
            options.SetSlidingExpiration(slidingExpiration.Value);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        }
        options.SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
        return options;
    }
}
