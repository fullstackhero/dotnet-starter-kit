﻿using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence.Initialization;
using Mapster;
using Microsoft.Extensions.Localization;

namespace FSH.WebApi.Infrastructure.Multitenancy;

internal class TenantService : ITenantService
{
    private readonly IMultiTenantStore<FSHTenantInfo> _tenantStore;
    private readonly IConnectionStringSecurer _csSecurer;
    private readonly ApplicationDbInitializer _dbInitializer;
    private readonly IStringLocalizer<TenantService> _localizer;

    public TenantService(IMultiTenantStore<FSHTenantInfo> tenantStore, IConnectionStringSecurer csSecurer, ApplicationDbInitializer dbInitializer, IStringLocalizer<TenantService> localizer)
    {
        _tenantStore = tenantStore;
        _csSecurer = csSecurer;
        _dbInitializer = dbInitializer;
        _localizer = localizer;
    }

    public async Task<List<TenantDto>> GetAllAsync()
    {
        var tenants = (await _tenantStore.GetAllAsync()).Adapt<List<TenantDto>>();

        tenants.ForEach(t => t.ConnectionString = _csSecurer.MakeSecure(t.ConnectionString));

        return tenants;
    }

    public async Task<bool> ExistsWithIdAsync(string id) =>
        await _tenantStore.TryGetAsync(id) is not null;

    public async Task<TenantDto> GetByIdAsync(string id) =>
        (await GetTenantInfoAsync(id))
            .Adapt<TenantDto>();

    public async Task<string> CreateAsync(string id, string name, string? connectionString, string adminEmail, CancellationToken cancellationToken)
    {
        var tenant = new FSHTenantInfo(id, name, connectionString ?? string.Empty, adminEmail);

        await _tenantStore.TryAddAsync(tenant);

        try
        {
            await _dbInitializer.InitializeAsync(cancellationToken);
        }
        catch
        {
            await _tenantStore.TryRemoveAsync(id);
            throw;
        }

        return tenant.Id;
    }

    public async Task<string> ActivateAsync(string id)
    {
        var tenant = await GetTenantInfoAsync(id);

        if (tenant.IsActive)
        {
            throw new ConflictException("Tenant is already Activated.");
        }

        tenant.Activate();

        await _tenantStore.TryUpdateAsync(tenant);

        return $"Tenant {id} is now Activated.";
    }

    public async Task<string> DeactivateAsync(string id)
    {
        var tenant = await GetTenantInfoAsync(id);

        if (!tenant.IsActive)
        {
            throw new ConflictException("Tenant is already Deactivated.");
        }

        tenant.Deactivate();

        await _tenantStore.TryUpdateAsync(tenant);

        return $"Tenant {id} is now Deactivated.";
    }

    public async Task<string> UpdateSubscription(string id, DateTime extendedExpiryDate)
    {
        var tenant = await GetTenantInfoAsync(id);

        tenant.SetValidity(extendedExpiryDate);

        await _tenantStore.TryUpdateAsync(tenant);

        return $"Tenant {id}'s Subscription Upgraded. Now Valid till {tenant.ValidUpto}.";
    }

    private async Task<FSHTenantInfo> GetTenantInfoAsync(string id) =>
        await _tenantStore.TryGetAsync(id)
            ?? throw new NotFoundException(string.Format(_localizer["entity.notfound"], typeof(FSHTenantInfo).Name, id));
}