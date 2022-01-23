using Ardalis.Specification;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Common.Contracts;
using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Infrastructure.Persistence.Repository;

/// <summary>
/// The repository that implements IRepositoryWithEvents.
/// Implemented as a decorator. It only augments the Add,
/// Update and Delete calls where it adds the respective
/// EntityCreated, EntityUpdated or EntityDeleted event
/// before delegating to the decorated repository.
/// </summary>
public class EventAddingRepositoryDecorator<T> : IRepositoryWithEvents<T>
    where T : class, IAggregateRoot
{
    private readonly IRepository<T> _decorated;

    public EventAddingRepositoryDecorator(IRepository<T> decorated) => _decorated = decorated;

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.DomainEvents.Add(EntityCreatedEvent.WithEntity(entity));
        return _decorated.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.DomainEvents.Add(EntityUpdatedEvent.WithEntity(entity));
        return _decorated.UpdateAsync(entity, cancellationToken);
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.DomainEvents.Add(EntityDeletedEvent.WithEntity(entity));
        return _decorated.DeleteAsync(entity, cancellationToken);
    }

    public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            entity.DomainEvents.Add(EntityDeletedEvent.WithEntity(entity));
        }

        return _decorated.DeleteRangeAsync(entities, cancellationToken);
    }

    // The rest of the methods are simply forwarded.
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _decorated.SaveChangesAsync(cancellationToken);
    public Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
        where TId : notnull =>
        _decorated.GetByIdAsync(id, cancellationToken);
    public Task<T?> GetBySpecAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISingleResultSpecification, ISpecification<T> =>
        _decorated.GetBySpecAsync(specification, cancellationToken);
    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default) =>
        _decorated.GetBySpecAsync(specification, cancellationToken);
    public Task<List<T>> ListAsync(CancellationToken cancellationToken = default) =>
        _decorated.ListAsync(cancellationToken);
    public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        _decorated.ListAsync(specification, cancellationToken);
    public Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default) =>
        _decorated.ListAsync(specification, cancellationToken);
    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        _decorated.AnyAsync(specification, cancellationToken);
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        _decorated.AnyAsync(cancellationToken);
    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        _decorated.CountAsync(specification, cancellationToken);
    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _decorated.CountAsync(cancellationToken);
}