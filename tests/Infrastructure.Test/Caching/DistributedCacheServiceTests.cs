using FSH.WebApi.Infrastructure.Caching;

namespace Infrastructure.Test.Caching;

public class DistributedCacheServiceTests : CacheServiceTests
{
    public DistributedCacheServiceTests(DistributedCacheService cacheService)
        : base(cacheService)
    {
    }
}