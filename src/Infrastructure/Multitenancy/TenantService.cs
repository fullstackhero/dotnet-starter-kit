using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Mapster;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Text;
using DN.WebApi.Application.Common.Constants;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class TenantService : ITenantService
{
    private readonly ISerializerService _serializer;

    private readonly ICacheService _cache;

    private readonly IStringLocalizer<TenantService> _localizer;

    private readonly DatabaseSettings _options;

    private readonly TenantManagementDbContext _context;

    private TenantDto _currentTenant;

    public TenantService(
        IOptions<DatabaseSettings> options,
        IStringLocalizer<TenantService> localizer,
        TenantManagementDbContext context,
        ICacheService cache,
        ISerializerService serializer)
    {
        _localizer = localizer;
        _options = options.Value;
        _context = context;
        _cache = cache;
        _serializer = serializer;
    }

    public string GetConnectionString()
    {
        return _currentTenant?.ConnectionString;
    }

    public string GetDatabaseProvider()
    {
        return _options.DBProvider;
    }

    public TenantDto GetCurrentTenant()
    {
        return _currentTenant;
    }

    private void SetDefaultConnectionStringToCurrentTenant()
    {
        _currentTenant.ConnectionString = _options.ConnectionString;
    }

    public void SetCurrentTenant(string tenant)
    {
        if (_currentTenant != null)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        TenantDto tenantDto;
        string cacheKey = CacheKeys.GetCacheKey("tenant", tenant);
        byte[] cachedData = !string.IsNullOrWhiteSpace(cacheKey) ? _cache.Get(cacheKey) : null;
        if (cachedData != null)
        {
            _cache.Refresh(cacheKey);
            tenantDto = _serializer.Deserialize<TenantDto>(Encoding.Default.GetString(cachedData));
        }
        else
        {
            var tenantInfo = _context.Tenants.Where(a => a.Key == tenant).FirstOrDefault();
            tenantDto = tenantInfo.Adapt<TenantDto>();
            if (tenantDto != null)
            {
                var options = new DistributedCacheEntryOptions();
                byte[] serializedData = Encoding.Default.GetBytes(_serializer.Serialize(tenantDto));
                _cache.Set(cacheKey, serializedData, options);
            }
        }

        if (tenantDto == null)
        {
            throw new InvalidTenantException(_localizer["tenant.invalid"]);
        }

        if (tenantDto.Key != MultitenancyConstants.Root.Key)
        {
            if (!tenantDto.IsActive)
            {
                throw new InvalidTenantException(_localizer["tenant.inactive"]);
            }

            if (DateTime.UtcNow > tenantDto.ValidUpto)
            {
                throw new InvalidTenantException(_localizer["tenant.expired"]);
            }
        }

        _currentTenant = tenantDto;
        if (string.IsNullOrEmpty(_currentTenant.ConnectionString))
        {
            SetDefaultConnectionStringToCurrentTenant();
        }
    }
}