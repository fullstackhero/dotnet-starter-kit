using FSH.Framework.Core.MultiTenancy.Features.Creation;

namespace FSH.Framework.Core.MultiTenancy.Abstractions;
public interface ITenantService
{
    Task<List<TenantDto>> GetAllAsync();
    Task<bool> ExistsWithIdAsync(string id);
    Task<bool> ExistsWithNameAsync(string name);
    Task<TenantDto> GetByIdAsync(string id);
    Task<string> CreateAsync(TenantCreationCommand request, CancellationToken cancellationToken);
    Task<string> ActivateAsync(string id);
    Task<string> DeactivateAsync(string id);
    Task<string> UpdateSubscription(string id, DateTime extendedExpiryDate);
}
