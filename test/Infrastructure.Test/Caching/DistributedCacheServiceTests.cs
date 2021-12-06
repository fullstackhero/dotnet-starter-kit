using DN.WebApi.Infrastructure.Caching;
using DN.WebApi.Infrastructure.Common.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Caching;

public class DistributedCacheServiceTests : CacheServiceTests<DistributedCacheService>
{
    protected override DistributedCacheService CreateCacheService() =>
        new DistributedCacheService(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new NewtonSoftService(),
            NullLogger<DistributedCacheService>.Instance);
}
