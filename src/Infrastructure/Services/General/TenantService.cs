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
            _tenantSettings = options.Value;
            _httpContext = contextAccessor.HttpContext;
            if (_httpContext != null)
            {
                if (_httpContext.Request.Headers.TryGetValue("tenant", out var tenantName))
                {
                    _currentTenant = _tenantSettings.Tenants.Where(a => a.Name == tenantName).FirstOrDefault();
                }
            }
        }
        public string GetConnectionString()
        {
            return _currentTenant?.ConnectionString;
        }
    }
}