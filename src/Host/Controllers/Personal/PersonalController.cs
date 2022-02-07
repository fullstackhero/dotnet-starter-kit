using System.Security.Claims;
using FSH.WebApi.Application.Auditing;
using FSH.WebApi.Application.Identity.Users;

namespace FSH.WebApi.Host.Controllers.Identity;

public class PersonalController : VersionNeutralApiController
{
    private readonly IUserService _userService;

    public PersonalController(IUserService userService) => _userService = userService;

    [HttpGet("logs")]
    [OpenApiOperation("Get audit logs of currently logged in user.", "")]
    public Task<List<AuditDto>> GetMyLogsAsync()
    {
        return Mediator.Send(new GetMyAuditLogsRequest());
    }

    [HttpGet("permissions")]
    [OpenApiOperation("Get permissions of currently logged in user.", "")]
    public async Task<ActionResult<List<string>>> GetMyPermissionsAsync(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return Ok(await _userService.GetPermissionsAsync(userId, cancellationToken));
    }
}