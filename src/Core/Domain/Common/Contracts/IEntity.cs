namespace DN.WebApi.Domain.Common.Contracts;

public interface IEntity
{
}

public interface IEntity<TKey> : IEntity
{
    TKey Id { get; }
}