using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Persistence.Contexts;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

public class TenantRepository : TenantManagementRepository<Tenant>, ITenantReadRepository, ITenantRepository
{
    public TenantRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}