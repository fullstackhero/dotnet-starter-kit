using DN.WebApi.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Test.Caching;

public class LocalCacheServiceTests : CacheServiceTests<LocalCacheService>
{
    protected override LocalCacheService CreateCacheService() =>
        new LocalCacheService(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LocalCacheService>.Instance);
}
