using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Multitenancy
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet("{key}")]
        [MustHavePermission(Permissions.Tenants.View)]
        public async Task<IActionResult> GetAsync(string key)
        {
            var tenant = await _tenantService.GetByKeyAsync(key);
            return Ok(tenant);
        }

        [HttpGet]
        [MustHavePermission(Permissions.Tenants.ListAll)]
        public async Task<IActionResult> GetAllAsync()
        {
            var tenants = await _tenantService.GetAllAsync();
            return Ok(tenants);
        }

        [HttpPost]
        [MustHavePermission(Permissions.Tenants.Create)]
        public async Task<IActionResult> CreateAsync(CreateTenantRequest request)
        {
            var tenantId = await _tenantService.CreateTenantAsync(request);
            return Ok(tenantId);
        }
    }
}