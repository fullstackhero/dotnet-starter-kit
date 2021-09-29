using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Permissions;
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
        [MustHavePermission(Permissions.Roles.ListAll)]
        public async Task<IActionResult> GetListAsync()
        {
            var roles = await _roleService.GetListAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        [MustHavePermission(Permissions.Roles.View)]
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
        [MustHavePermission(Permissions.Roles.Register)]
        public async Task<IActionResult> RegisterRoleAsync(RoleRequest request)
        {
            var response = await _roleService.RegisterRoleAsync(request);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [MustHavePermission(Permissions.Roles.Remove)]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            var response = await _roleService.DeleteAsync(id);
            return Ok(response);
        }
    }
}