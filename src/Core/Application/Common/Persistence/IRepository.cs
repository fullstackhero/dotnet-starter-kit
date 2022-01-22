namespace FSH.WebApi.Application.Common.Persistence;

// The Repository for the Application Db
// IRepositoryBase<T> is from Ardalis.Specification

/// <summary>
/// The regular read/write repository.
/// </summary>
public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

/// <summary>
/// The read-only repository.
/// </summary>
public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}

/// <summary>
/// A special (read/write) repository, that also adds events to the
/// entities domain events before adding, updating or deleting entities.
/// </summary>
public interface IRepositoryWithEvents<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}