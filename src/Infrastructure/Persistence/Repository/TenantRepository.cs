using Ardalis.Specification.EntityFrameworkCore;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence.Context;

namespace FSH.WebApi.Infrastructure.Persistence.Repository;

// Inherited from Ardalis.Specification's RepositoryBase<T>
public class TenantRepository : RepositoryBase<Tenant>, ITenantReadRepository, ITenantRepository
{
    public TenantRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}