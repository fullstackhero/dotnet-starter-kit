using System;

namespace DN.WebApi.Domain.Contracts
{
    public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDelete
    {
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; private set; }
        public Guid LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public Guid? DeletedBy { get; set; }

        public AuditableEntity()
        {
            CreatedOn = DateTime.UtcNow;
            LastModifiedOn = DateTime.UtcNow;
        }
    }
}