namespace DN.WebApi.Domain.Common.Contracts;

public abstract class BaseEntity
{
    public virtual object Id { get; protected set; }
    public List<DomainEvent> DomainEvents = new();
}