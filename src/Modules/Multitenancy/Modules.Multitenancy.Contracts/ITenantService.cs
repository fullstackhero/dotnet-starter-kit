using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenants;

namespace FSH.Modules.Multitenancy.Contracts;

public interface ITenantService
{
    Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken);

    Task<bool> ExistsWithIdAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default);

    Task<TenantStatusDto> GetStatusAsync(string id, CancellationToken cancellationToken = default);

    Task<string> CreateAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, string planKey, DateTime validUpto, CancellationToken cancellationToken);

    Task<string> ActivateAsync(string id, CancellationToken cancellationToken);

    Task<string> DeactivateAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends the tenant's validity by one plan term, stacking on remaining time (no backdating), and
    /// switches the tenant's plan when <paramref name="newPlanKey"/> differs. Returns the term window
    /// applied and whether the plan changed, so the caller can publish a matching renewal event.
    /// </summary>
    Task<(DateTime PeriodStartUtc, DateTime ValidUpto, bool PlanChanged)> RenewAsync(
        string id, string newPlanKey, int termMonths, CancellationToken cancellationToken = default);

    Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);

    Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);
}