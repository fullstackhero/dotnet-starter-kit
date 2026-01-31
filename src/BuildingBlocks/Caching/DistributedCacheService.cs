using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FSH.Framework.Caching;

/// <summary>
/// Implementation of <see cref="ICacheService"/> using distributed cache (Redis or in-memory).
/// Provides JSON serialization for cached objects with configurable expiration policies.
/// </summary>
public sealed class DistributedCacheService : ICacheService
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly CachingOptions _opts;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheService"/>.
    /// </summary>
    /// <param name="cache">The underlying distributed cache implementation.</param>
    /// <param name="logger">Logger for cache operations.</param>
    /// <param name="opts">Caching configuration options.</param>
    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger,
        IOptions<CachingOptions> opts)
    {
        ArgumentNullException.ThrowIfNull(opts);

        _cache = cache;
        _logger = logger;
        _opts = opts.Value;
    }

    /// <inheritdoc />
    public async Task<T?> GetItemAsync<T>(string key, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            var bytes = await _cache.GetAsync(key, ct).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0) return default;
            return JsonSerializer.Deserialize<T>(Utf8.GetString(bytes), JsonOpts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache get failed for {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetItemAsync<T>(string key, T value, TimeSpan? sliding = default, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            var bytes = Utf8.GetBytes(JsonSerializer.Serialize(value, JsonOpts));
            await _cache.SetAsync(key, bytes, BuildEntryOptions(sliding), ct).ConfigureAwait(false);
            _logger.LogDebug("Cached {Key}", key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache set failed for {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        key = Normalize(key);
        try { await _cache.RemoveAsync(key, ct).ConfigureAwait(false); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { _logger.LogWarning(ex, "Cache remove failed for {Key}", key); }
    }

    /// <inheritdoc />
    public async Task RefreshItemAsync(string key, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            await _cache.RefreshAsync(key, ct).ConfigureAwait(false);
            _logger.LogDebug("Refreshed {Key}", key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { _logger.LogWarning(ex, "Cache refresh failed for {Key}", key); }
    }

    /// <inheritdoc />
    public T? GetItem<T>(string key) => GetItemAsync<T>(key).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void SetItem<T>(string key, T value, TimeSpan? sliding = default) => SetItemAsync(key, value, sliding).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void RemoveItem(string key) => RemoveItemAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void RefreshItem(string key) => RefreshItemAsync(key).GetAwaiter().GetResult();

    /// <summary>
    /// Builds cache entry options with configured expiration settings.
    /// </summary>
    /// <param name="sliding">Optional sliding expiration override.</param>
    /// <returns>Configured cache entry options.</returns>
    private DistributedCacheEntryOptions BuildEntryOptions(TimeSpan? sliding)
    {
        var o = new DistributedCacheEntryOptions();

        if (sliding.HasValue)
            o.SetSlidingExpiration(sliding.Value);
        else if (_opts.DefaultSlidingExpiration.HasValue)
            o.SetSlidingExpiration(_opts.DefaultSlidingExpiration.Value);

        if (_opts.DefaultAbsoluteExpiration.HasValue)
            o.SetAbsoluteExpiration(_opts.DefaultAbsoluteExpiration.Value);

        return o;
    }

    /// <summary>
    /// Normalizes the cache key by applying the configured prefix.
    /// </summary>
    /// <param name="key">The original cache key.</param>
    /// <returns>The normalized key with prefix applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null or whitespace.</exception>
    private string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        var prefix = _opts.KeyPrefix ?? string.Empty;
        if (prefix.Length == 0)
        {
            return key;
        }

        return key.StartsWith(prefix, StringComparison.Ordinal)
            ? key
            : prefix + key;
    }
}
