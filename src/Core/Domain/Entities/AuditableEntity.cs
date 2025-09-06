using FSH.Framework.Core.Domain.Interfaces;

namespace FSH.Framework.Core.Domain.Entities;
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable, ISoftDelete
{
    public string? CreatedBy { get; protected set; }
    public DateTime CreatedOnUtc { get; protected set; }
    public string? LastModifiedBy { get; protected set; }
    public DateTime? LastModifiedOnUtc { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedOnUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }
    public void SoftDelete(string? by, DateTime whenUtc)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedBy = by;
        DeletedOnUtc = whenUtc;
    }
    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedBy = null;
        DeletedOnUtc = null;
    }
}

public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() => Id = Guid.NewGuid();
}