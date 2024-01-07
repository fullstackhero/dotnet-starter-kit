using FSH.Framework.Core.Tenant.Dtos;
using FSH.Framework.Core.Tenant.Features.RegisterTenant;

namespace FSH.Framework.Core.Tenant.Abstractions;
public interface ITenantService
{
    Task<List<TenantDetail>> GetAllAsync();
    Task<bool> ExistsWithIdAsync(string id);
    Task<bool> ExistsWithNameAsync(string name);
    Task<TenantDetail> GetByIdAsync(string id);
    Task<string> CreateAsync(RegisterTenantCommand request, CancellationToken cancellationToken);
    Task<string> ActivateAsync(string id);
    Task<string> DeactivateAsync(string id);
    Task<string> UpdateSubscription(string id, DateTime extendedExpiryDate);
}
