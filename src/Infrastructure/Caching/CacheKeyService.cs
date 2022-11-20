using FSH.WebApi.Application.Common.Caching;

namespace FSH.WebApi.Infrastructure.Caching;

public class CacheKeyService : ICacheKeyService
{

    public CacheKeyService() { }

    public string GetCacheKey(string name, object id)
    {
        return $"{name}-{id}";
    }
}