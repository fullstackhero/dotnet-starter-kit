using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ResetPassword;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ResetPassword;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, string>
{
    private readonly IUserService _userService;

    public ResetPasswordCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<string> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _userService.ResetPasswordAsync(command.Email, command.Password, command.Token, cancellationToken).ConfigureAwait(false);

        return "Password has been reset.";
    }
}