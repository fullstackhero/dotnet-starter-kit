namespace FSH.WebApi.Domain.Common.Events;

public class EntityDeletedEvent<T> : DomainEvent
    where T : class
{
    public EntityDeletedEvent(T entity) => Entity = entity;

    public T Entity { get; }
}