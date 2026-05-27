using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.DeleteUser;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.DeleteUser;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Unit>
{
    private readonly IUserService _userService;

    public DeleteUserCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<Unit> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _userService.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}