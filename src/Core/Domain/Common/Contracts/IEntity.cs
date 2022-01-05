namespace DN.WebApi.Domain.Common.Contracts;

public interface IEntity
{

}

public interface IEntity<T> : IEntity
{
    new T Id { get; }
}
