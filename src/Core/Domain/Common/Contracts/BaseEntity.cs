using MassTransit;

namespace DN.WebApi.Domain.Common.Contracts;

public abstract class BaseEntity<TKey> : IEntity<TKey>
{
    public TKey Id { get; protected set; } = default!;
    public List<DomainEvent> DomainEvents = new();
}

public abstract class BaseEntity : BaseEntity<DefaultIdType>
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}