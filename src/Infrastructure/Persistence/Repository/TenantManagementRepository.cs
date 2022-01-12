using Ardalis.Specification.EntityFrameworkCore;
using FSH.WebAPI.Application.Common.Persistence;
using FSH.WebAPI.Domain.Common.Contracts;
using FSH.WebAPI.Infrastructure.Persistence.Context;

namespace FSH.WebAPI.Infrastructure.Persistence.Repository;

// inherit from Ardalis.Specification type
public class TenantManagementRepository<T> : RepositoryBase<T>, ITenantManagementReadRepository<T>, ITenantManagementRepository<T>
    where T : class, IAggregateRoot
{
    public TenantManagementRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}