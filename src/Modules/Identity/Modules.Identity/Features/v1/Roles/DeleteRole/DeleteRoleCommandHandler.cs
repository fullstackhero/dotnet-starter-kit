using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.DeleteRole;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler(IRoleService roleService) : ICommandHandler<DeleteRoleCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await roleService.DeleteRoleAsync(command.Id, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}