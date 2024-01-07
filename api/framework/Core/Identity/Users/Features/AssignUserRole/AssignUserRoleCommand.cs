using System.Collections.ObjectModel;
using FSH.Framework.Core.Identity.Users.Dtos;

namespace FSH.Framework.Core.Identity.Users.Features.AssignUserRole;
public class AssignUserRoleCommand
{
    public Collection<UserRoleDetail> UserRoles { get; } = new();
}
