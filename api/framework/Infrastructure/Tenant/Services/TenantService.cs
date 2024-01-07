using Finbuckle.MultiTenant;
using FSH.Framework.Core.Abstraction.Persistence;
using FSH.Framework.Core.Configurations;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Tenant.Abstractions;
using FSH.Framework.Core.Tenant.Dtos;
using FSH.Framework.Core.Tenant.Features.RegisterTenant;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Tenant.Services;
public sealed class TenantService : ITenantService
{
    private readonly IMultiTenantStore<FshTenantInfo> _tenantStore;
    private readonly DatabaseOptions _config;
    private readonly IServiceProvider _serviceProvider;

    public TenantService(IMultiTenantStore<FshTenantInfo> tenantStore, IOptions<DatabaseOptions> config, IServiceProvider serviceProvider)
    {
        _tenantStore = tenantStore;
        _config = config.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> ActivateAsync(string id)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);

        if (tenant.IsActive)
        {
            throw new FshException("Tenant is already Activated.");
        }

        tenant.Activate();

        await _tenantStore.TryUpdateAsync(tenant).ConfigureAwait(false);

        return $"Tenant {id} is now Activated.";
    }

    public async Task<string> CreateAsync(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var connectionString = request.ConnectionString;
        if (request.ConnectionString?.Trim() == _config.ConnectionString.Trim())
        {
            connectionString = string.Empty;
        }

        FshTenantInfo tenant = new(request.Id, request.Name, connectionString, request.AdminEmail, request.Issuer);
        await _tenantStore.TryAddAsync(tenant).ConfigureAwait(false);

        await InitializeDatabase(tenant).ConfigureAwait(false);

        return tenant.Id;
    }

    private async Task InitializeDatabase(FshTenantInfo tenant)
    {
        // First create a new scope
        using var scope = _serviceProvider.CreateScope();

        // Then set current tenant so the right connection string is used
        _serviceProvider.GetRequiredService<IMultiTenantContextAccessor>()
            .MultiTenantContext = new MultiTenantContext<FshTenantInfo>()
            {
                TenantInfo = tenant
            };

        // using the scope, perform migrations / seeding
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
        foreach (var initializer in initializers)
        {
            await initializer.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
            await initializer.SeedAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    public async Task<string> DeactivateAsync(string id)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);
        if (!tenant.IsActive)
        {
            throw new FshException("Tenant is already Deactivated.");
        }

        tenant.Deactivate();
        await _tenantStore.TryUpdateAsync(tenant).ConfigureAwait(false);
        return $"Tenant {id} is now Activated.";
    }

    public async Task<bool> ExistsWithIdAsync(string id) =>
        await _tenantStore.TryGetAsync(id).ConfigureAwait(false) is not null;

    public async Task<bool> ExistsWithNameAsync(string name) =>
        (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Any(t => t.Name == name);
    public async Task<List<TenantDetail>> GetAllAsync()
    {
        var tenants = (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Adapt<List<TenantDetail>>();
        return tenants;
    }

    public async Task<TenantDetail> GetByIdAsync(string id) =>
        (await GetTenantInfoAsync(id).ConfigureAwait(false))
            .Adapt<TenantDetail>();

    public async Task<string> UpdateSubscription(string id, DateTime extendedExpiryDate)
    {
        var tenant = await GetTenantInfoAsync(id).ConfigureAwait(false);
        tenant.SetValidity(extendedExpiryDate);
        await _tenantStore.TryUpdateAsync(tenant).ConfigureAwait(false);
        return $"Tenant {id}'s Subscription Upgraded. Now Valid till {tenant.ValidUpto}.";
    }

    private async Task<FshTenantInfo> GetTenantInfoAsync(string id) =>
    await _tenantStore.TryGetAsync(id).ConfigureAwait(false)
        ?? throw new NotFoundException($"{typeof(FshTenantInfo).Name} {id} Not Found.");
}
