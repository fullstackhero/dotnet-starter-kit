using Finbuckle.MultiTenant;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.MultiTenancy;
using FSH.Framework.Core.MultiTenancy.Abstractions;
using FSH.Framework.Core.MultiTenancy.Features.Creation;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Multitenancy.Services;
public sealed class TenantService : ITenantService
{
    private readonly IMultiTenantStore<FshTenantInfo> _tenantStore;
    private readonly DbConfig _config;
    private readonly IServiceProvider _serviceProvider;

    public TenantService(IMultiTenantStore<FshTenantInfo> tenantStore, IOptions<DbConfig> config, IServiceProvider serviceProvider)
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

    public async Task<string> CreateAsync(TenantCreationCommand request, CancellationToken cancellationToken)
    {
        var connectionString = request.ConnectionString;
        if (request.ConnectionString?.Trim() == _config.ConnectionString.Trim())
        {
            connectionString = string.Empty;
        }

        FshTenantInfo tenant = new(request.Id, request.Name, connectionString, request.AdminEmail, request.Issuer);
        await _tenantStore.TryAddAsync(tenant).ConfigureAwait(false);

        await BootstrapTenantDatabase(tenant).ConfigureAwait(false);

        return tenant.Id;
    }

    private async Task BootstrapTenantDatabase(FshTenantInfo tenant)
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
        var dbServices = scope.ServiceProvider.GetServices<IDbBootstrapper>();
        foreach (var db in dbServices)
        {
            await db.StartAsync(tenant, CancellationToken.None).ConfigureAwait(false);
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
    public async Task<List<TenantDto>> GetAllAsync()
    {
        var tenants = (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Adapt<List<TenantDto>>();
        return tenants;
    }

    public async Task<TenantDto> GetByIdAsync(string id) =>
        (await GetTenantInfoAsync(id).ConfigureAwait(false))
            .Adapt<TenantDto>();

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
