using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.UpdateUser;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.UpdateUser;

public sealed class UpdateUserCommandHandler(IUserService userService) : ICommandHandler<UpdateUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await userService.UpdateAsync(
            command.Id,
            command.FirstName ?? string.Empty,
            command.LastName ?? string.Empty,
            command.PhoneNumber ?? string.Empty,
            command.Image!,
            command.DeleteCurrentImage).ConfigureAwait(false);

        return Unit.Value;
    }
}