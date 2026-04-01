using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ChangePassword;

public sealed class ChangePasswordCommandHandler(IUserService userService, ICurrentUser currentUser) : ICommandHandler<ChangePasswordCommand, string>
{
    public async ValueTask<string> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new InvalidOperationException("User is not authenticated.");
        }

        var userId = currentUser.GetUserId().ToString();

        await userService.ChangePasswordAsync(command.Password, command.NewPassword, command.ConfirmNewPassword, userId).ConfigureAwait(false);

        return "password reset email sent";
    }
}