using DN.WebApi.Application.Common;
using DN.WebApi.Application.Common.Constants;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Mapster;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class TenantService : ITenantService
{
    private readonly ICacheService _cache;

    private readonly IStringLocalizer<TenantService> _localizer;

    private readonly DatabaseSettings _options;

    private readonly TenantManagementDbContext _context;

    private TenantDto? _currentTenant;

    public TenantService(
        IOptions<DatabaseSettings> options,
        IStringLocalizer<TenantService> localizer,
        TenantManagementDbContext context,
        ICacheService cache)
    {
        _localizer = localizer;
        _options = options.Value;
        _context = context;
        _cache = cache;
    }

    public string? GetConnectionString() =>
        _currentTenant?.ConnectionString;

    public string? GetDatabaseProvider() =>
        _options.DBProvider;

    public TenantDto? GetCurrentTenant() =>
        _currentTenant;

    public void SetCurrentTenant(string tenant)
    {
        if (_currentTenant is not null)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        var tenantDto = _cache.GetOrSet(
            CacheKeys.GetCacheKey("tenant", tenant),
            () =>
            {
                var tenantInfo = _context.Tenants.Where(a => a.Key == tenant).FirstOrDefault();
                return tenantInfo?.Adapt<TenantDto>();
            });

        if (tenantDto is null)
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
            _currentTenant.ConnectionString = _options.ConnectionString;
        }
    }
}