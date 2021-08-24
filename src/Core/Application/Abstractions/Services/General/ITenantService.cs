using DN.WebApi.Application.Settings;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface ITenantService
    {
        public string GetConnectionString();
        public Tenant GetTenant();
    }
}