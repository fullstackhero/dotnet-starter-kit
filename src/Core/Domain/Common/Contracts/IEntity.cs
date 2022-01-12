namespace FSH.WebApi.Domain.Common.Contracts;

public interface IEntity
{
    List<DomainEvent> DomainEvents { get; }
}

public interface IEntity<T> : IEntity
{
    T Id { get; }
}