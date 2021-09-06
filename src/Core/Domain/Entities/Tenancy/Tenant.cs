using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Tenancy
{
    public class Tenant : AuditableEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string AdminEmail { get; set; }
        public string ConnectionString { get; set; }
        public string DBProvider { get; set; }
        public bool IsActive { get; set; }
        public DateTime ValidUpto { get; set; }
    }
}