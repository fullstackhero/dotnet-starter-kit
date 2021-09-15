using System;

namespace DN.WebApi.Domain.Contracts
{
    public interface IAuditableEntity
    {
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; }
        public Guid LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
    }
}