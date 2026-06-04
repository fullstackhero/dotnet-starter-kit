using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ResendConfirmationEmail;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandHandler : ICommandHandler<ResendConfirmationEmailCommand, Unit>
{
    private readonly IUserService _userService;

    public ResendConfirmationEmailCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<Unit> Handle(ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _userService.ResendConfirmationEmailAsync(command.UserId, command.Origin, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
