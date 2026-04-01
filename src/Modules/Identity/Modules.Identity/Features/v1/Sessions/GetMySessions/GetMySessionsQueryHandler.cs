using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.GetMySessions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Sessions.GetMySessions;

public sealed class GetMySessionsQueryHandler(ISessionService sessionService, ICurrentUser currentUser) : IQueryHandler<GetMySessionsQuery, List<UserSessionDto>>
{
    public async ValueTask<List<UserSessionDto>> Handle(GetMySessionsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        return await sessionService.GetUserSessionsAsync(userId, cancellationToken);
    }
}