using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Exceptions;
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
                if (_httpContext.Request.Headers.TryGetValue("tenant", out var tenantId))
                {
                    _currentTenant = _tenantSettings.Tenants.Where(a => a.TID == tenantId).FirstOrDefault();
                    if(_currentTenant == null)
                    {
                        throw new InvalidTenantException();
                    }
                }
                else
                {
                     throw new InvalidTenantException();
                }
            }
        }
        public string GetConnectionString()
        {
            return _currentTenant?.ConnectionString;
        }
        public Tenant GetTenant()
        {
            return _currentTenant;
        }
    }
}