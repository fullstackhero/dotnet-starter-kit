namespace DN.WebApi.Domain.Common.Contracts;

public interface IEntity
{

}

public interface IEntity<T> : IEntity
{
    T Id { get; }
}
