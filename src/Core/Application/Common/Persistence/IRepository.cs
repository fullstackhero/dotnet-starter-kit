namespace FSH.WebApi.Application.Common.Persistence;

// The Repository for the Application Db
// IRepositoryBase<T> is from Ardalis.Specification

public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
}