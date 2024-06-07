using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Core.Identity.Roles.Features;
using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using FSH.Framework.Core.Identity.Roles.Features.DeleteRole;
using Microsoft.AspNetCore.Identity;

namespace FSH.Framework.Infrastructure.Identity.Roles;

public class RoleService : IRoleService
{
    private readonly RoleManager<FshRole> _roleManager;

    public RoleService(RoleManager<FshRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<RoleResponse>> GetAllRolesAsync()
    {
        return await Task.Run(() => _roleManager.Roles
            .Select(role => new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description })
            .ToList());
    }

    public async Task<RoleResponse?> GetRoleByIdAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        return role != null ? new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description } : null;
    }

    public async Task<RoleResponse> CreateOrUpdateRoleAsync(CreateOrUpdateRoleCommand command)
    {
        var role = await _roleManager.FindByNameAsync(command.Name);

        if (role != null)
        {
            role.Description = command.Description;
            await _roleManager.UpdateAsync(role);
        }
        else
        {
            role = new FshRole(command.Name, command.Description);
            await _roleManager.CreateAsync(role);
        }

        return new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description };
    }

    public async Task DeleteRoleAsync(DeleteRoleCommand command)
    {
        var role = await _roleManager.FindByIdAsync(command.Id);
        if (role != null)
        {
            await _roleManager.DeleteAsync(role);
        }
    }
}
