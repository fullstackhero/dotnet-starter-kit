using DN.WebApi.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Caching;

public class DistributedCacheService : ICacheService
{
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly IDistributedCache _cache;

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public byte[]? Get(string key)
    {
        try
        {
            return _cache.Get(key);
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try
        {
            return await _cache.GetAsync(key, token);
        }
        catch
        {
            return null;
        }
    }

    public void Refresh(string key)
    {
        try
        {
            _cache.Refresh(key);
        }
        catch
        {
        }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        try
        {
            await _cache.RefreshAsync(key, token);
            _logger.LogDebug(string.Format("Cache Refreshed : {0}", key));
        }
        catch
        {
        }
    }

    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
        }
        catch
        {
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            await _cache.RemoveAsync(key, token);
        }
        catch
        {
        }
    }

    public void Set(string key, byte[] value, TimeSpan? slidingExpiration = null)
    {
        try
        {
            _cache.Set(key, value, GetOptions(slidingExpiration));
            _logger.LogDebug($"Added to Cache : {key}");
        }
        catch
        {
        }
    }

    public async Task SetAsync(string key, byte[] value, TimeSpan? slidingExpiration, CancellationToken token = default)
    {
        try
        {
            await _cache.SetAsync(key, value, GetOptions(slidingExpiration), token);
            _logger.LogDebug($"Added to Cache : {key}");
        }
        catch
        {
        }
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
            // TODO: add to appsettings?
            options.SetSlidingExpiration(TimeSpan.FromMinutes(10)); // Default expiration time of 10 minutes.
        }

        return options;
    }
}