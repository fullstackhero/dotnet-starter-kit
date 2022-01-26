using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Test.Caching;

public class LocalCacheService : CacheService<FSH.WebApi.Infrastructure.Caching.LocalCacheService>
{
    protected override FSH.WebApi.Infrastructure.Caching.LocalCacheService CreateCacheService() =>
        new(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<FSH.WebApi.Infrastructure.Caching.LocalCacheService>.Instance);
}