namespace FSH.WebApi.Domain.Common.Events;

public class EntityUpdatedEvent<T> : DomainEvent
    where T : IEntity
{
    public EntityUpdatedEvent(T entity) => Entity = entity;

    public T Entity { get; }
}
