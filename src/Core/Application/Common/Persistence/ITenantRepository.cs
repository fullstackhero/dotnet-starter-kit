using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Application.Common.Persistence;

// Easier access to the TenantRepository

public interface ITenantRepository : ITenantManagementRepository<Tenant>
{
}

public interface ITenantReadRepository : ITenantManagementReadRepository<Tenant>
{
}