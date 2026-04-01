using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;

public sealed class AdminRevokeAllSessionsCommandHandler(ISessionService sessionService, ICurrentUser currentUser) : ICommandHandler<AdminRevokeAllSessionsCommand, int>
{
    public async ValueTask<int> Handle(AdminRevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetUserId().ToString();
        return await sessionService.RevokeAllSessionsForAdminAsync(
            command.UserId.ToString(),
            adminId,
            command.Reason ?? "Revoked by administrator",
            cancellationToken);
    }
}