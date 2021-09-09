using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetListAsync()
        {
            var roles = await _roleService.GetListAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAsync(RoleRequest request)
        {
            var response = await _roleService.SaveAsync(request);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            var response = await _roleService.DeleteAsync(id);
            return Ok(response);
        }
    }
}