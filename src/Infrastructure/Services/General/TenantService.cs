using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class TenantService : ITenantService
    {
        private readonly TenantSettings _tenantSettings;
        private HttpContext _httpContext;
        private Tenant _currentTenant;
        public TenantService(IOptions<TenantSettings> options, IHttpContextAccessor contextAccessor)
        {

        }
        public string GetConnectionString()
        {
            return _currentTenant.ConnectionString;
        }
    }
}