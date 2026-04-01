using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.GetUserSessions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Sessions.GetUserSessions;

public sealed class GetUserSessionsQueryHandler(ISessionService sessionService) : IQueryHandler<GetUserSessionsQuery, List<UserSessionDto>>
{
    public async ValueTask<List<UserSessionDto>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        return await sessionService.GetUserSessionsForAdminAsync(query.UserId.ToString(), cancellationToken);
    }
}