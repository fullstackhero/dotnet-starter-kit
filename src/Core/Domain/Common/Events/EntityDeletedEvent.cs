namespace FSH.WebApi.Domain.Common.Events;

public class EntityDeletedEvent<T> : DomainEvent
    where T : IEntity
{
    public EntityDeletedEvent(T entity) => Entity = entity;

    public T Entity { get; }
}
