using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Core.Roles;

namespace FSH.Framework.Identity.Contracts.v1.Users.AssignUserRoles;
public sealed class AssignUserRolesCommand : ICommand<string>
{
    public required string UserId { get; init; }
    public IReadOnlyList<UserRoleDto> UserRoles { get; init; } = Array.Empty<UserRoleDto>();
}
