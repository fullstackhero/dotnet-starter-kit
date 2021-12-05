namespace DN.WebApi.Application.Common.Interfaces;

public interface ISerializingCacheService
{
    Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T?>> getItemCallback, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken);
}