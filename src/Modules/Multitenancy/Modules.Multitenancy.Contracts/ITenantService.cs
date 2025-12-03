using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenants;
using FSH.Framework.Shared.Multitenancy;

namespace FSH.Modules.Multitenancy.Contracts;

public interface ITenantService
{
    Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken);

    Task<bool> ExistsWithIdAsync(string id);

    Task<bool> ExistsWithNameAsync(string name);

    Task<TenantStatusDto> GetStatusAsync(string id);

    Task<string> CreateAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, CancellationToken cancellationToken);

    Task<string> ActivateAsync(string id, CancellationToken cancellationToken);

    Task<string> DeactivateAsync(string id);

    Task<DateTime> UpgradeSubscription(string id, DateTime extendedExpiryDate);

    Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);

    Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);
}
