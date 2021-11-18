using System;
using System.Linq;
using System.Net;
using System.Text;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Constants;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class TenantService : ITenantService
    {
        private readonly ISerializerService _serializer;
        private readonly ICacheService _cache;

        private readonly IStringLocalizer<TenantService> _localizer;

        private readonly ICurrentUser _currentUser;

        private readonly DatabaseSettings _options;

        private readonly TenantManagementDbContext _context;

        private readonly HttpContext _httpContext;

        private TenantDto _currentTenant;

        public TenantService(IOptions<DatabaseSettings> options, IHttpContextAccessor contextAccessor, ICurrentUser currentUser, IStringLocalizer<TenantService> localizer, TenantManagementDbContext context, ICacheService cache, ISerializerService serializer, PerformingContext performingContext)
        {
            _localizer = localizer;
            _options = options.Value;
            _httpContext = contextAccessor.HttpContext;
            _currentUser = currentUser;
            _context = context;

            // _mapper = mapper;
            _cache = cache;
            _serializer = serializer;
            if (_httpContext != null)
            {
                if (_currentUser.IsAuthenticated())
                {
                    SetTenant(_currentUser.GetTenant());
                }
                else
                {
                    string tenantFromQueryString = System.Web.HttpUtility.ParseQueryString(_httpContext.Request.QueryString.Value).Get("tenant");
                    if (!string.IsNullOrEmpty(tenantFromQueryString))
                    {
                        SetTenant(tenantFromQueryString);
                    }
                    else if (_httpContext.Request.Headers.TryGetValue("tenant", out var tenant))
                    {
                        SetTenant(tenant);
                    }
                    else
                    {
                        throw new IdentityException(_localizer["auth.failed"], statusCode: HttpStatusCode.Unauthorized);
                    }
                }
            }
            else if (performingContext != null)
            {
                string tenant = performingContext.GetJobParameter<string>("tenant");
                if (!string.IsNullOrEmpty(tenant))
                {
                    SetTenant(tenant);
                }
            }
        }

        public string GetConnectionString()
        {
            return _currentTenant?.ConnectionString;
        }

        public string GetDatabaseProvider()
        {
            return _options.DBProvider;
        }

        public TenantDto GetCurrentTenant()
        {
            return _currentTenant;
        }

        private void SetDefaultConnectionStringToCurrentTenant()
        {
            _currentTenant.ConnectionString = _options.ConnectionString;
        }

        private void SetTenant(string tenant)
        {
            TenantDto tenantDto;
            string cacheKey = CacheKeys.GetCacheKey("tenant", tenant);
            byte[] cachedData = !string.IsNullOrWhiteSpace(cacheKey) ? _cache.GetAsync(cacheKey).Result : null;
            if (cachedData != null)
            {
                _cache.RefreshAsync(cacheKey).Wait();
                tenantDto = _serializer.Deserialize<TenantDto>(Encoding.Default.GetString(cachedData));
            }
            else
            {
                var tenantInfo = _context.Tenants.Where(a => a.Key == tenant).FirstOrDefaultAsync().Result;
                tenantDto = tenantInfo.Adapt<TenantDto>();
                if (tenantDto != null)
                {
                    var options = new DistributedCacheEntryOptions();
                    byte[] serializedData = Encoding.Default.GetBytes(_serializer.Serialize(tenantDto));
                    _cache.SetAsync(cacheKey, serializedData, options).Wait();
                }
            }

            if (tenantDto == null)
            {
                throw new InvalidTenantException(_localizer["tenant.invalid"]);
            }

            if (tenantDto.Key != MultitenancyConstants.Root.Key)
            {
                if (!tenantDto.IsActive)
                {
                    throw new InvalidTenantException(_localizer["tenant.inactive"]);
                }

                if (DateTime.UtcNow > tenantDto.ValidUpto)
                {
                    throw new InvalidTenantException(_localizer["tenant.expired"]);
                }
            }

            _currentTenant = tenantDto;
            if (string.IsNullOrEmpty(_currentTenant.ConnectionString))
            {
                SetDefaultConnectionStringToCurrentTenant();
            }
        }
    }
}