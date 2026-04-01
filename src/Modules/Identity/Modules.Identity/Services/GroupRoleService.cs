using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Services;

public sealed class GroupRoleService(IdentityDbContext dbContext) : IGroupRoleService
{
    public async Task<IReadOnlyList<string>> GetUserGroupRolesAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // Get all group IDs the user belongs to
        var userGroupIds = await dbContext.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync(ct);

        if (userGroupIds.Count == 0)
        {
            return [];
        }

        // Get all distinct role names from those groups
        var groupRoles = await dbContext.GroupRoles
            .Where(gr => userGroupIds.Contains(gr.GroupId))
            .Select(gr => gr.Role!.Name!)
            .Distinct()
            .ToListAsync(ct);

        return groupRoles;
    }
}