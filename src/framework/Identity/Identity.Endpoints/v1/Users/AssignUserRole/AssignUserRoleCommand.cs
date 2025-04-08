using FSH.Framework.Core.Identity.Users.Dtos;

namespace FSH.Framework.Identity.Endpoints.v1.Users.AssignUserRole;

public class AssignUserRoleCommand
{
    /// <summary>
    /// A list of user-role assignment entries.
    /// </summary>
    public IReadOnlyList<UserRoleDetail> UserRoles { get; init; } = Array.Empty<UserRoleDetail>();
}
