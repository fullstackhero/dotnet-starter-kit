using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Roles;
using FSH.Framework.Core.Identity.Roles.Features.CreateOrUpdateRole;
using Microsoft.AspNetCore.Identity;

namespace FSH.Framework.Infrastructure.Identity.Roles;

public class RoleService(RoleManager<FshRole> roleManager) : IRoleService
{
    private readonly RoleManager<FshRole> _roleManager = roleManager;

    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        return await Task.Run(() => _roleManager.Roles
            .Select(role => new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description })
            .ToList());
    }

    public async Task<RoleDto?> GetRoleAsync(string id)
    {
        FshRole? role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("Role Not Found.");

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task<RoleDto> CreateOrUpdateRoleAsync(CreateOrUpdateRoleCommand command)
    {
        FshRole? role = await _roleManager.FindByIdAsync(command.Id);

        if (role != null)
        {
            role.Name = command.Name;
            role.Description = command.Description;
            await _roleManager.UpdateAsync(role);
        }
        else
        {
            role = new FshRole(command.Name, command.Description);
            await _roleManager.CreateAsync(role);
        }

        return new RoleDto { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task DeleteRoleAsync(string id)
    {
        FshRole? role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("Role Not Found.");

        await _roleManager.DeleteAsync(role);
    }
}
