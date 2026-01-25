using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;

public sealed class UpdateGroupCommandHandler : ICommandHandler<UpdateGroupCommand, GroupDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateGroupCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<GroupDto> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await _dbContext.Groups
            .Include(g => g.GroupRoles)
            .FirstOrDefaultAsync(g => g.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{command.Id}' not found.");

        // Validate name is unique within tenant (excluding self)
        var nameExists = await _dbContext.Groups
            .AnyAsync(g => g.Name == command.Name && g.Id != command.Id, cancellationToken);

        if (nameExists)
        {
            throw new CustomException($"Group with name '{command.Name}' already exists.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);
        }

        // Validate role IDs exist
        if (command.RoleIds is { Count: > 0 })
        {
            var existingRoleIds = await _dbContext.Roles
                .Where(r => command.RoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            var invalidRoleIds = command.RoleIds.Except(existingRoleIds).ToList();
            if (invalidRoleIds.Count > 0)
            {
                throw new NotFoundException($"Roles not found: {string.Join(", ", invalidRoleIds)}");
            }
        }

        // Update group properties
        group.Update(command.Name, command.Description, _currentUser.GetUserId().ToString());
        group.SetAsDefault(command.IsDefault, _currentUser.GetUserId().ToString());

        // Update role assignments
        var currentRoleIds = group.GroupRoles.Select(gr => gr.RoleId).ToHashSet();
        var newRoleIds = command.RoleIds?.ToHashSet() ?? [];

        // Remove roles no longer assigned
        var rolesToRemove = group.GroupRoles.Where(gr => !newRoleIds.Contains(gr.RoleId)).ToList();
        foreach (var role in rolesToRemove)
        {
            group.GroupRoles.Remove(role);
        }

        // Add new role assignments
        foreach (var roleId in newRoleIds.Where(id => !currentRoleIds.Contains(id)))
        {
            group.GroupRoles.Add(GroupRole.Create(group.Id, roleId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get member count and role names for response
        var memberCount = await _dbContext.UserGroups
            .CountAsync(ug => ug.GroupId == group.Id, cancellationToken);

        var roleNames = newRoleIds.Count > 0
            ? await _dbContext.Roles
                .Where(r => newRoleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(cancellationToken)
            : [];

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsDefault = group.IsDefault,
            IsSystemGroup = group.IsSystemGroup,
            MemberCount = memberCount,
            RoleIds = newRoleIds.ToList().AsReadOnly(),
            RoleNames = roleNames.AsReadOnly(),
            CreatedAt = group.CreatedAt
        };
    }
}
