using Ardalis.Specification;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Application.Common.Persistence;

// The Repository for the TenantManagement Db
// I(Read)RepositoryBase<T> is from Ardalis.Specification

public interface ITenantManagementRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

public interface ITenantManagementReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

// Easy access to the TenantRepository

public interface ITenantRepository : ITenantManagementRepository<Tenant>
{
}

public interface ITenantReadRepository : ITenantManagementReadRepository<Tenant>
{
}