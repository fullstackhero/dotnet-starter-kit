using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
using DN.WebApi.Shared.DTOs.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DN.WebApi.Bootstrapper.Controllers.Multitenancy
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantManager _tenantService;

        public TenantsController(ITenantManager tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet("{key}")]
        [MustHavePermission(RootPermissions.Tenants.View)]
        public async Task<IActionResult> GetAsync(string key)
        {
            var tenant = await _tenantService.GetByKeyAsync(key);
            return Ok(tenant);
        }

        [HttpGet]
        [MustHavePermission(RootPermissions.Tenants.ListAll)]
        public async Task<IActionResult> GetAllAsync()
        {
            var tenants = await _tenantService.GetAllAsync();
            return Ok(tenants);
        }

        [HttpPost]
        [MustHavePermission(RootPermissions.Tenants.Create)]
        public async Task<IActionResult> CreateAsync(CreateTenantRequest request)
        {
            var tenantId = await _tenantService.CreateTenantAsync(request);
            return Ok(tenantId);
        }
    }
}