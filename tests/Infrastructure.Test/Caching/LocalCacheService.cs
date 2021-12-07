using DN.WebApi.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Test.Caching;

public class LocalCacheService : CacheService<DN.WebApi.Infrastructure.Caching.LocalCacheService>
{
    protected override DN.WebApi.Infrastructure.Caching.LocalCacheService CreateCacheService() =>
        new(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<DN.WebApi.Infrastructure.Caching.LocalCacheService>.Instance);
}
