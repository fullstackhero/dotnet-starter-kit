using FSH.Framework.Abstractions.Domain;

namespace FSH.Framework.Domain;

public class AuditableEntity<TId> : BaseEntity<TId>, IAuditable, ISoftDeletable
{
    public DateTime Created { get; private set; }

    public Guid CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
}
