using Ardalis.Specification;
using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Application.Common.Persistence;

// from Ardalis.Specification

// TenantManagement Db

public interface ITenantManagementRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

public interface ITenantManagementReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}