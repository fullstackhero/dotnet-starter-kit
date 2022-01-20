namespace FSH.WebApi.Domain.Common.Events;

public class EntityUpdatedEvent<T> : DomainEvent
    where T : class
{
    public EntityUpdatedEvent(T entity) => Entity = entity;

    public T Entity { get; }
}