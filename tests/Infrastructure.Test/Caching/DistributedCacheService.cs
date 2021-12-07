using DN.WebApi.Infrastructure.Caching;
using DN.WebApi.Infrastructure.Common.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Caching;

public class DistributedCacheService : CacheService<DN.WebApi.Infrastructure.Caching.DistributedCacheService>
{
    protected override DN.WebApi.Infrastructure.Caching.DistributedCacheService CreateCacheService() =>
        new(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new NewtonSoftService(),
            NullLogger<DN.WebApi.Infrastructure.Caching.DistributedCacheService>.Instance);
}
