using Ardalis.Specification;
using FSH.Modules.Common.Core.Domain.Contracts;

namespace FSH.Framework.Core.Persistence;
public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}