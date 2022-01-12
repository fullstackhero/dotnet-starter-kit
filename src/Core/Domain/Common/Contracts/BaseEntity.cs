using MassTransit;

namespace FSH.WebApi.Domain.Common.Contracts;

public abstract class BaseEntity : BaseEntity<DefaultIdType>
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}

public abstract class BaseEntity<T> : IEntity<T>
{
    public T Id { get; protected set; } = default!;

    public List<DomainEvent> DomainEvents = new();
}