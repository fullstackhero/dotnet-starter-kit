using System;

namespace DN.WebApi.Domain.Contracts
{
    public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDelete
    {
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsModified { get; set; }
        public bool IsDeleted { get; set; }

        public AuditableEntity()
        {
            CreatedOn = DateTime.UtcNow;
            IsModified = false;
            IsDeleted = false;
        }
    }
}