using MassTransit;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSH.WebApi.Domain.Common.Contracts;

public abstract class BaseEntity : BaseEntity<DefaultIdType>
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}

public abstract class BaseEntity<TId> : IEntity<TId>, IEntityTestable
{
    public TId Id { get; protected set; } = default!;

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new();

    #region IEntityTestable
    protected virtual string GetInternalCode(int num) => GetPrefixInternalCode() + num.ToString("0000");
    public string GetPrefixInternalCode() => GetType().Name;
    [NotMapped]
    public string? InternalCode { get; set; }
    public virtual void SetInternalCode(int? num) => InternalCode = num is null ? null : GetInternalCode((int)num);
    public void CleanInternalCode() => SetInternalCode(null);
    #endregion
}