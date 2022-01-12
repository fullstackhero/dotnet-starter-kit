using Ardalis.Specification.EntityFrameworkCore;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Common.Contracts;
using FSH.WebApi.Infrastructure.Persistence.Context;

namespace FSH.WebApi.Infrastructure.Persistence.Repository;

// inherit from Ardalis.Specification type
public class TenantManagementRepository<T> : RepositoryBase<T>, ITenantManagementReadRepository<T>, ITenantManagementRepository<T>
    where T : class, IAggregateRoot
{
    public TenantManagementRepository(TenantManagementDbContext dbContext)
        : base(dbContext)
    {
    }
}