using FSH.Framework.Identity.Core.Roles;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Identity.Contracts.v1.Users.AssignUserRoles;
public sealed class AssignUserRolesCommand : ICommand<string>
{
    public required string UserId { get; init; }
    public List<UserRoleDto> UserRoles { get; init; } = new();
}