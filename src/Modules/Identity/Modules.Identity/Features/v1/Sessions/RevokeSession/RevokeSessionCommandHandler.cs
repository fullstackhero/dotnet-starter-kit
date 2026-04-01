using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.RevokeSession;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Sessions.RevokeSession;

public sealed class RevokeSessionCommandHandler(ISessionService sessionService, ICurrentUser currentUser) : ICommandHandler<RevokeSessionCommand, bool>
{
    public async ValueTask<bool> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        return await sessionService.RevokeSessionAsync(
            command.SessionId,
            userId,
            "User requested",
            cancellationToken);
    }
}