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

        var group = await GetGroupAsync(command.Id, cancellationToken);
        await ValidateUniqueNameAsync(command.Id, command.Name, cancellationToken);
        await ValidateRoleIdsAsync(command.RoleIds, cancellationToken);

        var userId = _currentUser.GetUserId().ToString();
        group.Update(command.Name, command.Description, userId);
        group.SetAsDefault(command.IsDefault, userId);

        var newRoleIds = UpdateRoleAssignments(group, command.RoleIds);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(group, newRoleIds, cancellationToken);
    }

    private async Task<Group> GetGroupAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Groups
            .Include(g => g.GroupRoles)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{id}' not found.");
    }

    private async Task ValidateUniqueNameAsync(Guid excludeId, string name, CancellationToken cancellationToken)
    {
        var nameExists = await _dbContext.Groups
            .AnyAsync(g => g.Name == name && g.Id != excludeId, cancellationToken);

        if (nameExists)
        {
            throw new CustomException($"Group with name '{name}' already exists.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);
        }
    }

    private async Task ValidateRoleIdsAsync(IReadOnlyList<string>? roleIds, CancellationToken cancellationToken)
    {
        if (roleIds is not { Count: > 0 })
        {
            return;
        }

        var existingRoleIds = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var invalidRoleIds = roleIds.Except(existingRoleIds).ToList();
        if (invalidRoleIds.Count > 0)
        {
            throw new NotFoundException($"Roles not found: {string.Join(", ", invalidRoleIds)}");
        }
    }

    private static HashSet<string> UpdateRoleAssignments(Group group, IReadOnlyList<string>? roleIds)
    {
        var currentRoleIds = group.GroupRoles.Select(gr => gr.RoleId).ToHashSet();
        var newRoleIds = roleIds?.ToHashSet() ?? [];

        var rolesToRemove = group.GroupRoles.Where(gr => !newRoleIds.Contains(gr.RoleId)).ToList();
        foreach (var role in rolesToRemove)
        {
            group.GroupRoles.Remove(role);
        }

        foreach (var roleId in newRoleIds.Where(id => !currentRoleIds.Contains(id)))
        {
            group.GroupRoles.Add(GroupRole.Create(group.Id, roleId));
        }

        return newRoleIds;
    }

    private async Task<GroupDto> BuildResponseAsync(Group group, HashSet<string> roleIds, CancellationToken cancellationToken)
    {
        var memberCount = await _dbContext.UserGroups
            .CountAsync(ug => ug.GroupId == group.Id, cancellationToken);

        var roleNames = roleIds.Count > 0
            ? await _dbContext.Roles
                .Where(r => roleIds.Contains(r.Id))
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
            RoleIds = roleIds.ToList().AsReadOnly(),
            RoleNames = roleNames.AsReadOnly(),
            CreatedAt = group.CreatedAt
        };
    }
}
