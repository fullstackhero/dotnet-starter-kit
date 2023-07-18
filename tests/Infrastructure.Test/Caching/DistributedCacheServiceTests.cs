using FL_CRMS_ERP_WEBAPI.Infrastructure.Caching;

namespace Infrastructure.Test.Caching;

public class DistributedCacheServiceTests : CacheServiceTests
{
    public DistributedCacheServiceTests(DistributedCacheService cacheService)
        : base(cacheService)
    {
    }
}