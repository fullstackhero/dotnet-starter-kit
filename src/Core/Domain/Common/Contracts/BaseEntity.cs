namespace DN.WebApi.Domain.Common.Contracts;

public abstract class BaseEntity
{
    public List<DomainEvent> DomainEvents = new();
}