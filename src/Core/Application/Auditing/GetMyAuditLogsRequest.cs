using DN.WebApi.Application.Identity.Interfaces;
using MediatR;

namespace DN.WebApi.Application.Auditing;

public class GetMyAuditLogsRequest : IRequest<List<AuditDto>>
{
}

public class GetMyAuditLogsRequestHandler : IRequestHandler<GetMyAuditLogsRequest, List<AuditDto>>
{
    private readonly ICurrentUser _user;
    private readonly IAuditService _auditService;

    public GetMyAuditLogsRequestHandler(ICurrentUser user, IAuditService auditService) =>
        (_user, _auditService) = (user, auditService);

    public Task<List<AuditDto>> Handle(GetMyAuditLogsRequest request, CancellationToken cancellationToken) =>
        _auditService.GetUserTrailsAsync(_user.GetUserId());
}