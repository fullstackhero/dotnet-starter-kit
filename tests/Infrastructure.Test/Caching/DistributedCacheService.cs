using FSH.WebAPI.Infrastructure.Common.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Caching;

public class DistributedCacheService : CacheService<FSH.WebAPI.Infrastructure.Caching.DistributedCacheService>
{
    protected override FSH.WebAPI.Infrastructure.Caching.DistributedCacheService CreateCacheService() =>
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new NewtonSoftService(),
            NullLogger<FSH.WebAPI.Infrastructure.Caching.DistributedCacheService>.Instance);
}