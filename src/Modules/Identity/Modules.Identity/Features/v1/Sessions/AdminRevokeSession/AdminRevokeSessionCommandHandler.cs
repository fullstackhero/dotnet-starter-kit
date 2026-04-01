using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Sessions.AdminRevokeSession;

public sealed class AdminRevokeSessionCommandHandler(ISessionService sessionService, ICurrentUser currentUser) : ICommandHandler<AdminRevokeSessionCommand, bool>
{
    public async ValueTask<bool> Handle(AdminRevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetUserId().ToString();

        // Get the session to verify it belongs to the specified user
        var session = await sessionService.GetSessionAsync(command.SessionId, cancellationToken);
        if (session is null || session.UserId != command.UserId.ToString())
        {
            return false;
        }

        // Use the admin revocation method (doesn't check ownership)
        return await sessionService.RevokeSessionForAdminAsync(
            command.SessionId,
            adminId,
            command.Reason ?? "Revoked by administrator",
            cancellationToken);
    }
}