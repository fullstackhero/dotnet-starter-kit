using Ardalis.Specification;
using DN.WebApi.Domain.Common.Contracts;

namespace DN.WebApi.Application.Common.Persistence;

// from Ardalis.Specification

// Application Db

public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}