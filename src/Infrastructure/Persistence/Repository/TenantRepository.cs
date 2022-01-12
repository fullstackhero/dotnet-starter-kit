using Ardalis.Specification.EntityFrameworkCore;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Persistence.Context;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

// Inherited from Ardalis.Specification's RepositoryBase<T>
public class TenantRepository : RepositoryBase<Tenant>, ITenantReadRepository, ITenantRepository
{
    public TenantRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}