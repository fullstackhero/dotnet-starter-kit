using Ardalis.Specification.EntityFrameworkCore;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Infrastructure.Persistence.Contexts;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

// inherit from Ardalis.Specification type
public class TenantManagementRepository<T> : RepositoryBase<T>, ITenantManagementReadRepository<T>, ITenantManagementRepository<T>
    where T : class, IAggregateRoot
{
    public TenantManagementRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}