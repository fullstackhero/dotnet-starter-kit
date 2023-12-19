using FSH.Framework.Abstractions.Domain;

namespace FSH.Framework.Core.Domain;

public class AuditableEntity<TId> : BaseEntity<TId>, IAuditable, ISoftDeletable
{
    public DateTime Created { get; private set; } = DateTime.Now;

    public Guid CreatedBy { get; set; }

    public DateTime? LastModified { get; set; } = DateTime.Now;
    public Guid? LastModifiedBy { get; set; }
}

public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() => Id = Guid.NewGuid();
}
