using FSH.WebAPI.Application.Common.Persistence;
using FSH.WebAPI.Domain.Multitenancy;
using FSH.WebAPI.Infrastructure.Persistence.Context;

namespace FSH.WebAPI.Infrastructure.Persistence.Repository;

public class TenantRepository : TenantManagementRepository<Tenant>, ITenantReadRepository, ITenantRepository
{
    public TenantRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}