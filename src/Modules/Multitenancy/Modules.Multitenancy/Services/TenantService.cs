using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenants;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Multitenancy.Features.v1.GetTenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Multitenancy.Services;

public sealed class TenantService : ITenantService
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly DatabaseOptions _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantProvisioningService _provisioningService;

    public TenantService(
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IOptions<DatabaseOptions> config,
        IServiceProvider serviceProvider,
        TenantDbContext dbContext,
        ITenantProvisioningService provisioningService)
    {
        _tenantStore = tenantStore;
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _provisioningService = provisioningService;
    }

    public async Task<string> ActivateAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);

        if (tenant.IsActive)
        {
            throw new CustomException($"tenant {id} is already activated");
        }

        await _provisioningService.EnsureCanActivateAsync(id, cancellationToken).ConfigureAwait(false);

        tenant.Activate();

        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);

        return $"tenant {id} is now activated";
    }

    public async Task<string> CreateAsync(string id,
        string name,
        string? connectionString,
        string adminEmail, string? issuer, CancellationToken cancellationToken)
    {
        if (connectionString?.Trim() == _config.ConnectionString.Trim())
        {
            connectionString = string.Empty;
        }

        AppTenantInfo tenant = new(id, name, connectionString, adminEmail, issuer);
        await _tenantStore.AddAsync(tenant).ConfigureAwait(false);

        return tenant.Id;
    }

    public async Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> DeactivateAsync(string id)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);
        if (!tenant.IsActive)
        {
            throw new CustomException($"tenant {id} is already deactivated");
        }

        int tenantCount = (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Count(t => t.IsActive);
        if (tenantCount <= 1)
        {
            throw new CustomException("At least one active tenant is required.");
        }

        if (!tenant.Id.Equals(MultitenancyConstants.Root.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new CustomException("The root tenant cannot be deactivated.");
        }

        tenant.Deactivate();
        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        return $"tenant {id} is now deactivated";
    }

    public async Task<bool> ExistsWithIdAsync(string id) =>
        await _tenantStore.GetAsync(id).ConfigureAwait(false) is not null;

    public async Task<bool> ExistsWithNameAsync(string name) =>
        (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Any(t => t.Name == name);

    public async Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AppTenantInfo> tenants = _dbContext.TenantInfo;
        var specification = new GetTenantsSpecification(query);
        IQueryable<TenantDto> projected = tenants.ApplySpecification(specification);

        return await projected
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantStatusDto> GetStatusAsync(string id)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);

        return new TenantStatusDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            IsActive = tenant.IsActive,
            ValidUpto = tenant.ValidUpto,
            HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString),
            AdminEmail = tenant.AdminEmail,
            Issuer = tenant.Issuer
        };
    }

    public async Task<DateTime> UpgradeSubscription(string id, DateTime extendedExpiryDate)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);
        tenant.SetValidity(extendedExpiryDate);
        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        return tenant.ValidUpto;
    }

    private async Task<AppTenantInfo> GetTenantInfoAsync(string id) =>
        await _tenantStore.GetAsync(id).ConfigureAwait(false)
            ?? throw new NotFoundException($"{typeof(AppTenantInfo).Name} {id} Not Found.");
}
