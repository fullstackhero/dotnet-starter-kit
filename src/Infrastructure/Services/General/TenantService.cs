using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class TenantService : ITenantService
    {
        private readonly IStringLocalizer<TenantService> _localizer;

        private readonly ICurrentUser _currentUser;

        private readonly TenantSettings _tenantSettings;

        private HttpContext _httpContext;

        private Tenant _currentTenant;

        public TenantService(IOptions<TenantSettings> options, IHttpContextAccessor contextAccessor, ICurrentUser currentUser, IStringLocalizer<TenantService> localizer)
        {
            _localizer = localizer;
            _tenantSettings = options.Value;
            _httpContext = contextAccessor.HttpContext;
            _currentUser = currentUser;
            if (_httpContext != null)
            {
                if (_currentUser.IsAuthenticated())
                {
                    SetTenant(_currentUser.GetTenantId());
                }
                else
                {
                    if (_httpContext.Request.Headers.TryGetValue("tenant", out var tenantId))
                    {
                        SetTenant(tenantId);
                    }
                    else
                    {
                        throw new InvalidTenantException(_localizer["tenant.invalidtenant"]);
                    }
                }
            }
        }

        private void SetTenant(string tenantId)
        {
            _currentTenant = _tenantSettings.Tenants.Where(a => a.TID == tenantId).FirstOrDefault();
            if (_currentTenant == null)
            {
                throw new InvalidTenantException(_localizer["tenant.invalidtenant"]);
            }

            if (string.IsNullOrEmpty(_currentTenant.ConnectionString))
            {
                SetDefaultConnectionStringToCurrentTenant();
            }
        }

        private void SetDefaultConnectionStringToCurrentTenant()
        {
            _currentTenant.ConnectionString = _tenantSettings.Defaults.ConnectionString;
        }

        public string GetConnectionString()
        {
            return _currentTenant?.ConnectionString;
        }

        public string GetDatabaseProvider()
        {
            return _tenantSettings.Defaults?.DBProvider;
        }

        public Tenant GetTenant()
        {
            return _currentTenant;
        }
    }
}