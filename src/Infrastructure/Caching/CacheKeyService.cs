using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Caching;

namespace FSH.WebApi.Infrastructure.Caching;

public class CacheKeyService : ICacheKeyService
{
    private readonly ITenantInfo? _currentTenant;

    public CacheKeyService(ITenantInfo currentTenant) => _currentTenant = currentTenant;

    public string GetCacheKey(string name, object id, bool includeTenantId = true)
    {
        string tenantId = includeTenantId
            ? _currentTenant?.Id ?? throw new InvalidOperationException("GetCacheKey: includeTenantId set to true and no ITenantInfo available.")
            : "GLOBAL";
        return $"{tenantId}-{name}-{id}";
    }
}