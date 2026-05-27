using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ToggleUserStatus;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ToggleUserStatus;

public sealed class ToggleUserStatusCommandHandler : ICommandHandler<ToggleUserStatusCommand, Unit>
{
    private readonly IUserService _userService;

    public ToggleUserStatusCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<Unit> Handle(ToggleUserStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            throw new ArgumentException("UserId must be provided.", nameof(command.UserId));
        }

        await _userService.ToggleStatusAsync(command.ActivateUser, command.UserId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}