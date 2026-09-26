using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.SetProfileImage;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.SetProfileImage;

public sealed class SetProfileImageCommandHandler(
    IUserProfileService profileService,
    ICurrentUser currentUser)
    : ICommandHandler<SetProfileImageCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetProfileImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("no current user")
            {
                MessageKey = "Error.NoCurrentUser",
            };
        }

        await profileService
            .SetImageUrlAsync(userId.ToString(), command.ImageUrl, cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
