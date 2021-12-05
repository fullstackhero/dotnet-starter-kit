using System.Text;
using DN.WebApi.Application.Common;
using DN.WebApi.Application.Common.Interfaces;

namespace DN.WebApi.Infrastructure.Caching;

internal class SerializingCacheService : ISerializingCacheService
{
    private readonly ICacheService _cache;
    private readonly ISerializerService _serializer;

    public SerializingCacheService(ICacheService cache, ISerializerService serializer) =>
        (_cache, _serializer) = (cache, serializer);

    public async Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T?>> getItemCallback, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            throw new ArgumentException("CacheKey can't be null or empty.", nameof(cacheKey));
        }

        byte[]? value = await _cache.GetOrSetAsync(
            cacheKey,
            async () => Serialize(await getItemCallback()),
            slidingExpiration,
            cancellationToken);

        return value is not null
            ? Deserialize<T>(value)
            : default;
    }

    private byte[] Serialize<T>(T item) =>
        Encoding.Default.GetBytes(_serializer.Serialize(item));

    private T Deserialize<T>(byte[] cachedData) =>
        _serializer.Deserialize<T>(Encoding.Default.GetString(cachedData));

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(key, cancellationToken);
}