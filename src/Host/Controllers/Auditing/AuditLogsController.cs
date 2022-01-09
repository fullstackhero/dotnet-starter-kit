using DN.WebApi.Application.Auditing;
using DN.WebApi.Shared.Authorization;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Identity;

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