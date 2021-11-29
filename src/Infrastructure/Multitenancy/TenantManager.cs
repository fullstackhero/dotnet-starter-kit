using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Application.Settings;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Multitenancy;

public class TenantManager : ITenantManager
{
    private readonly IServiceProvider _di;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _appContext;
    private readonly IStringLocalizer<TenantService> _localizer;

    private readonly DatabaseSettings _options;

    private readonly TenantManagementDbContext _context;
    private readonly ICurrentUser _user;

    public TenantManager(ApplicationDbContext appContext, IStringLocalizer<TenantService> localizer, IOptions<DatabaseSettings> options, TenantManagementDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ICurrentUser user, IServiceProvider di)
    {
        _appContext = appContext;
        _localizer = localizer;
        _options = options.Value;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _user = user;
        _di = di;
    }

    public async Task<Result<TenantDto>> GetByKeyAsync(string key)
    {
        var tenant = await _context.Tenants.Where(a => a.Key == key).FirstOrDefaultAsync();
        if (tenant == null) throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(Tenant).Name, key));
        var tenantDto = tenant.Adapt<TenantDto>();
        return await Result<TenantDto>.SuccessAsync(tenantDto);
    }

    public async Task<Result<List<TenantDto>>> GetAllAsync()
    {
        var tenants = await _context.Tenants.ToListAsync();
        var tenantDto = tenants.Adapt<List<TenantDto>>();
        return await Result<List<TenantDto>>.SuccessAsync(tenantDto);
    }

    public async Task<Result<object>> CreateTenantAsync(CreateTenantRequest request)
    {
        if (_context.Tenants.Any(a => a.Key == request.Key)) throw new Exception("Tenant with same key exists.");
        if (string.IsNullOrEmpty(request.ConnectionString)) request.ConnectionString = _options.ConnectionString;
        bool isValidConnectionString = TenantBootstrapper.TryValidateConnectionString(_options, request.ConnectionString, request.Key);
        if (!isValidConnectionString) throw new Exception("Failed to Establish Connection to Database. Please check your connection string.");
        var tenant = new Tenant(request.Name, request.Key, request.AdminEmail, request.ConnectionString)
        {
            CreatedBy = _user.GetUserId()
        };
        var seeders = _di.GetServices<IDatabaseSeeder>().ToList();
        TenantBootstrapper.Initialize(_appContext, _options, tenant, _userManager, _roleManager, seeders);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return await Result<object>.SuccessAsync(tenant.Id);
    }

    public async Task<Result<object>> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request)
    {
        var tenant = await _context.Tenants.Where(a => a.Key == request.Tenant).FirstOrDefaultAsync();
        if (tenant == null) throw new Exception("Tenant Not Found.");
        tenant.SetValidity(request.ExtendedExpiryDate);
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
        return await Result<object>.SuccessAsync($"Tenant {request.Tenant}'s Subscription Upgraded. Now Valid till {tenant.ValidUpto}.");
    }

    public async Task<Result<object>> DeactivateTenantAsync(string tenant)
    {
        var tenantInfo = await _context.Tenants.Where(a => a.Key == tenant).FirstOrDefaultAsync();
        if (tenantInfo == null) throw new Exception("Tenant Not Found.");
        if (!tenantInfo.IsActive) throw new Exception("Tenant is already Deactivated.");
        tenantInfo.Deactivate();
        _context.Tenants.Update(tenantInfo);
        await _context.SaveChangesAsync();
        return await Result<object>.SuccessAsync($"Tenant {tenantInfo.Key} is now Deactivated.");
    }

    public async Task<Result<object>> ActivateTenantAsync(string tenant)
    {
        var tenantInfo = await _context.Tenants.Where(a => a.Key == tenant).FirstOrDefaultAsync();
        if (tenantInfo == null) throw new Exception("Tenant Not Found.");
        if (tenantInfo.IsActive) throw new Exception("Tenant is already Activated.");
        tenantInfo.Activate();
        _context.Tenants.Update(tenantInfo);
        await _context.SaveChangesAsync();
        return await Result<object>.SuccessAsync($"Tenant {tenant} is now Activated.");
    }
}