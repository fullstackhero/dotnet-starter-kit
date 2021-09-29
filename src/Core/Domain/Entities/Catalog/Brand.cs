using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Catalog
{
    public class Brand : AuditableEntity, IMustHaveTenant
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string TenantKey { get; set; }

        public Brand(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public Brand Update(string name, string description)
        {
            Name = name;
            Description = description;
            return this;
        }
    }
}