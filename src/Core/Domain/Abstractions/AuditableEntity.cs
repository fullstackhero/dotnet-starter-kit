namespace DN.WebApi.Domain.Abstractions
{
    public abstract class AuditableEntity : BaseEntity
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; private set; }
        public string LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }

        public AuditableEntity()
        {
            CreatedOn = DateTime.Now;
            LastModifiedOn = DateTime.Now;
        }
    }
}