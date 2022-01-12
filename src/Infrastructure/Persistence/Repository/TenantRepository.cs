using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence.Context;

namespace FSH.WebApi.Infrastructure.Persistence.Repository;

public class TenantRepository : TenantManagementRepository<Tenant>, ITenantReadRepository, ITenantRepository
{
    public TenantRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}