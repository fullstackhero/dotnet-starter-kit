using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly ICurrentUser _currentUser;
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService, ICurrentUser currentUser)
        {
            _roleService = roleService;
            _currentUser = currentUser;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetListAsync()
        {
            var roles = await _roleService.GetListAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var roles = await _roleService.GetByIdAsync(id);
            return Ok(roles);
        }

        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetRolePermissionsAsync(string id)
        {
            var roles = await _roleService.GetByIdAsync(id);
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