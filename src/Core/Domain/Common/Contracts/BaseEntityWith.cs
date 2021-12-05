namespace DN.WebApi.Domain.Common.Contracts;

public class BaseEntityWith<T> : BaseEntity
{
    public T Id { get; set; }
}