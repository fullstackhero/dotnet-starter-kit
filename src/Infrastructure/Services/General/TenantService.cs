using System.Web;
using AutoMapper;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Settings;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Entities.Multitenancy;
using DN.WebApi.Infrastructure.Persistence;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class TenantService : ITenantService
    {
        private readonly IStringLocalizer<TenantService> _localizer;

        private readonly ICurrentUser _currentUser;

        private readonly MultitenancySettings _options;

        private readonly TenantManagementDbContext _context;

        private readonly IMapper _mapper;

        private HttpContext _httpContext;

        private TenantDto _currentTenant;

        public TenantService(IOptions<MultitenancySettings> options, IHttpContextAccessor contextAccessor, ICurrentUser currentUser, IStringLocalizer<TenantService> localizer, TenantManagementDbContext context, IMapper mapper)
        {
            _localizer = localizer;
            _options = options.Value;
            _httpContext = contextAccessor.HttpContext;
            _currentUser = currentUser;
            _context = context;
            _mapper = mapper;
            if (_httpContext != null)
            {
                if (_currentUser.IsAuthenticated())
                {
                    SetTenant(_currentUser.GetTenantKey());
                }
                else
                {
                    // Check if Token is Expired
                    var tenantFromQueryString = System.Web.HttpUtility.ParseQueryString(_httpContext.Request.QueryString.Value).Get("tenantKey");
                    if (!string.IsNullOrEmpty(tenantFromQueryString))
                    {
                        SetTenant(tenantFromQueryString);
                    }
                    else if (_httpContext.Request.Headers.TryGetValue("tenant", out var tenantKey))
                    {
                        SetTenant(tenantKey);
                    }
                    else
                    {
                        throw new InvalidTenantException(_localizer["tenant.invalidtenant"]);
                    }
                }
            }
        }

        private void SetTenant(string tenantKey)
        {
            var tenant = _context.Tenants.Where(a => a.Key == tenantKey).FirstOrDefaultAsync().Result;
            _currentTenant = _mapper.Map<TenantDto>(tenant);
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
            _currentTenant.ConnectionString = _options.ConnectionString;
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

        public async Task<Result<TenantDto>> GetByKeyAsync(string key)
        {
            var tenant = await _context.Tenants.Where(a => a.Key == key).FirstOrDefaultAsync();
            if (tenant == null) throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(Tenant).Name, key));
            var tenantDto = _mapper.Map<TenantDto>(tenant);
            return await Result<TenantDto>.SuccessAsync(tenantDto);
        }

        public async Task<Result<List<TenantDto>>> GetAllAsync()
        {
            var tenants = await _context.Tenants.ToListAsync();
            var tenantDto = _mapper.Map<List<TenantDto>>(tenants);
            return await Result<List<TenantDto>>.SuccessAsync(tenantDto);
        }

        public async Task<Result<object>> CreateTenantAsync(CreateTenantRequest request)
        {
            if (_context.Tenants.Any(a => a.Key == request.Key)) throw new Exception("Tenant with same key exists.");
            var tenant = _mapper.Map<Tenant>(request);
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            return await Result<object>.SuccessAsync(tenant.Id);

        }
    }
}