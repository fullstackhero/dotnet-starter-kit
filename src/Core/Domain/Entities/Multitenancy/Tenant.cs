using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Multitenancy
{
    public class Tenant : AuditableEntity
    {
        public string Name { get; private set; }
        public string Key { get; private set; }
        public string AdminEmail { get; private set; }
        public string ConnectionString { get; private set; }

        public Tenant(string name, string key, string adminEmail, string connectionString)
        {
            Name = name;
            Key = key;
            AdminEmail = adminEmail;
            ConnectionString = connectionString;
        }

        protected Tenant()
        {

        }
    }
}