using FSH.WebApi.Application.Identity.RoleClaims;

namespace FSH.WebApi.Host.Controllers.Identity;

public class RoleClaimsController : VersionNeutralApiController
{
    private readonly IRoleClaimsService _roleClaimService;

    public RoleClaimsController(IRoleClaimsService roleClaimService) => _roleClaimService = roleClaimService;

    [HttpGet]
    [Authorize(Policy = FSHPermissions.RoleClaims.View)]
    public Task<List<RoleClaimDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _roleClaimService.GetAllAsync(cancellationToken);
    }

    [HttpGet("{roleId}")]
    [Authorize(Policy = FSHPermissions.RoleClaims.View)]
    public Task<List<RoleClaimDto>> GetAllByRoleIdAsync([FromRoute] string roleId, CancellationToken cancellationToken)
    {
        return _roleClaimService.GetAllByRoleIdAsync(roleId, cancellationToken);
    }

    [HttpPost]
    [Authorize(Policy = FSHPermissions.RoleClaims.Create)]
    public Task<string> PostAsync(RoleClaimRequest request, CancellationToken cancellationToken)
    {
        return _roleClaimService.SaveAsync(request, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = FSHPermissions.RoleClaims.Delete)]
    public Task<string> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return _roleClaimService.DeleteAsync(id, cancellationToken);
    }
}