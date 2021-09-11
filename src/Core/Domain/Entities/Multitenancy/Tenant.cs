using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Multitenancy
{
    public class Tenant : AuditableEntity
    {
        public string Name { get; set; }
        public string TID { get; set; }
        public string AdminEmail { get; set; }
        public string ConnectionString { get; set; }
    }
}