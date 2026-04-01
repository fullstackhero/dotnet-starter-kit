using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.UpdatePermissions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

public sealed class UpdatePermissionsCommandHandler(IRoleService roleService) : ICommandHandler<UpdatePermissionsCommand, string>
{
    public async ValueTask<string> Handle(UpdatePermissionsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await roleService.UpdatePermissionsAsync(command.RoleId, command.Permissions, cancellationToken).ConfigureAwait(false);
    }
}