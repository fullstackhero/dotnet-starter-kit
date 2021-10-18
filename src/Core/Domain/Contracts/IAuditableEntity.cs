using System;

namespace DN.WebApi.Domain.Contracts
{
    public interface IAuditableEntity
    {
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; }
        public bool IsModified { get; set; }
        public bool IsDeleted { get; set; }
    }
}