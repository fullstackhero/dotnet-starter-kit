using DN.WebApi.Shared.DTOs.Multitenancy;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface ITenantService : IScopedService
    {
        public string GetDatabaseProvider();
        public string GetConnectionString();
        public TenantDto GetCurrentTenant();
    }
}