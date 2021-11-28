using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Shared.DTOs.Multitenancy;

namespace DN.WebApi.Application.Multitenancy;

public interface ITenantService : IScopedService
{
    public string GetDatabaseProvider();

    public string GetConnectionString();

    public TenantDto GetCurrentTenant();

    public void SetCurrentTenant(string tenant);
}