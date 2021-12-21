using DN.WebApi.Application.Auditing;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Identity;

[ApiController]
[Route("api/audit-logs")]
[ApiVersionNeutral]
[ApiConventionType(typeof(FSHApiConventions))]
public class AuditLogsController : ControllerBase
{
    private readonly ICurrentUser _user;
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService, ICurrentUser user)
    {
        _auditService = auditService;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<AuditResponse>>>> GetMyLogsAsync()
    {
        return Ok(await _auditService.GetUserTrailsAsync(_user.GetUserId()));
    }
}