using DN.WebApi.Application.Common;

namespace DN.WebApi.Application.Multitenancy;

public interface ITenantService : IScopedService
{
    public string? GetDatabaseProvider();

    public string? GetConnectionString();

    public TenantDto? GetCurrentTenant();

    public void SetCurrentTenant(string tenant);
}