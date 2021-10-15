using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Shared.DTOs.Identity.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleClaimsController : ControllerBase
    {
        private readonly IRoleClaimsService _roleClaimService;

        public RoleClaimsController(IRoleClaimsService roleClaimService)
        {
            _roleClaimService = roleClaimService;
        }

        [Authorize(Policy = Permissions.RoleClaims.View)]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var roleClaims = await _roleClaimService.GetAllAsync();
            return Ok(roleClaims);
        }

        [Authorize(Policy = Permissions.RoleClaims.View)]
        [HttpGet("{roleId}")]
        public async Task<IActionResult> GetAllByRoleIdAsync([FromRoute] string roleId)
        {
            var response = await _roleClaimService.GetAllByRoleIdAsync(roleId);
            return Ok(response);
        }

        [Authorize(Policy = Permissions.RoleClaims.Create)]
        [HttpPost]
        public async Task<IActionResult> PostAsync(RoleClaimRequest request)
        {
            var response = await _roleClaimService.SaveAsync(request);
            return Ok(response);
        }

        [Authorize(Policy = Permissions.RoleClaims.Delete)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var response = await _roleClaimService.DeleteAsync(id);
            return Ok(response);
        }
    }
}