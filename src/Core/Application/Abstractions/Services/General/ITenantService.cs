using DN.WebApi.Application.Settings;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface ITenantService : ITransientService
    {
        public string GetDatabaseProvider();
        public string GetConnectionString();
        public Tenant GetTenant();
    }
}