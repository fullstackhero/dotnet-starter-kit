using FSH.WebAPI.Application.Auditing;

namespace FSH.WebAPI.Host.Controllers.Identity;

[Route("api/audit-logs")]
public class AuditLogsController : VersionNeutralApiController
{
    [HttpGet]
    [MustHavePermission(FSHPermissions.AuditLogs.View)]
    public Task<List<AuditDto>> GetMyLogsAsync()
    {
        return Mediator.Send(new GetMyAuditLogsRequest());
    }
}