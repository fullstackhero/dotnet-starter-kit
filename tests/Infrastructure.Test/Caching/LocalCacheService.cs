using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Test.Caching;

public class LocalCacheService : CacheService<FSH.WebAPI.Infrastructure.Caching.LocalCacheService>
{
    protected override FSH.WebAPI.Infrastructure.Caching.LocalCacheService CreateCacheService() =>
        new(new MemoryCache(new MemoryCacheOptions()),
            NullLogger<FSH.WebAPI.Infrastructure.Caching.LocalCacheService>.Instance);
}