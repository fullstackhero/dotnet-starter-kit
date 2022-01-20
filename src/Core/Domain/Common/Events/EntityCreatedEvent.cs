namespace FSH.WebApi.Domain.Common.Events;

public class EntityCreatedEvent<T> : DomainEvent
    where T : class
{
    public EntityCreatedEvent(T entity) => Entity = entity;

    public T Entity { get; }
}