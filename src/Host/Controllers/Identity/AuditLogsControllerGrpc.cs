using DN.WebApi.Application.Auditing;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Auditing;
using GrpcShared.Controllers;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Identity;

public class AuditLogsControllerGrpc : IAuditLogsControllerGrpc
{
    private readonly ICurrentUser _user;
    private readonly IAuditService _auditService;

    public AuditLogsControllerGrpc(IAuditService auditService, ICurrentUser user)
    {
        _auditService = auditService;
        _user = user;
    }

    public async Task<Result<IEnumerable<AuditResponse>>> GetMyLogsAsync(CallContext context)
    {
        return await _auditService.GetUserTrailsAsync(_user.GetUserId());
    }
}
