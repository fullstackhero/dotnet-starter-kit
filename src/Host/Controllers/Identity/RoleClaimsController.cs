using DN.WebApi.Application.Identity.RoleClaims;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Identity;

public class RoleClaimsController : VersionNeutralApiController
{
    private readonly IRoleClaimsService _roleClaimService;

    public RoleClaimsController(IRoleClaimsService roleClaimService)
    {
        _roleClaimService = roleClaimService;
    }

    [HttpGet]
    [Authorize(Policy = FSHPermissions.RoleClaims.View)]
    public async Task<ActionResult<Result<List<RoleClaimResponse>>>> GetAllAsync()
    {
        var roleClaims = await _roleClaimService.GetAllAsync();
        return Ok(roleClaims);
    }

    [HttpGet("{roleId}")]
    [Authorize(Policy = FSHPermissions.RoleClaims.View)]
    public async Task<ActionResult<Result<List<RoleClaimResponse>>>> GetAllByRoleIdAsync([FromRoute] string roleId)
    {
        var response = await _roleClaimService.GetAllByRoleIdAsync(roleId);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = FSHPermissions.RoleClaims.Create)]
    public async Task<ActionResult<Result<string>>> PostAsync(RoleClaimRequest request)
    {
        var response = await _roleClaimService.SaveAsync(request);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = FSHPermissions.RoleClaims.Delete)]
    public async Task<ActionResult<Result<string>>> DeleteAsync(int id)
    {
        var response = await _roleClaimService.DeleteAsync(id);
        return Ok(response);
    }
}