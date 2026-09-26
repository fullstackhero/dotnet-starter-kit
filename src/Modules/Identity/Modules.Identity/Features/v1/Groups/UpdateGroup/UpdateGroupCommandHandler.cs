using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.UpdateGroup;

public sealed class UpdateGroupCommandHandler : ICommandHandler<UpdateGroupCommand, GroupDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUserPermissionService _userPermissionService;

    public UpdateGroupCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser, IUserPermissionService userPermissionService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _userPermissionService = userPermissionService;
    }

    public async ValueTask<GroupDto> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await GetGroupAsync(command.Id, cancellationToken);

        // System groups are framework-managed — name, description, default flag, and role
        // assignments are all part of the seed contract that the startup syncer relies on.
        if (group.IsSystemGroup)
        {
            throw new ForbiddenException("System groups cannot be modified.")
            {
                MessageKey = "Identity.SystemGroupsCannotBeModified",
                ResourceSource = typeof(IdentityResources),
            };
        }

        await ValidateUniqueNameAsync(command.Id, command.Name, cancellationToken);
        await ValidateRoleIdsAsync(command.RoleIds, cancellationToken);

        var userId = _currentUser.GetUserId().ToString();
        group.Update(command.Name, command.Description, userId);
        group.SetAsDefault(command.IsDefault, userId);

        var currentRoleIdsBefore = group.GroupRoles.Select(gr => gr.RoleId).ToHashSet();
        var newRoleIds = UpdateRoleAssignments(group, command.RoleIds);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // If the set of group→role assignments actually changed, every member's
        // effective permission set may have shifted — invalidate each.
        if (!currentRoleIdsBefore.SetEquals(newRoleIds))
        {
            var memberIds = await _dbContext.UserGroups
                .Where(ug => ug.GroupId == command.Id)
                .Select(ug => ug.UserId)
                .ToListAsync(cancellationToken);
            foreach (var memberId in memberIds)
            {
                await _userPermissionService.InvalidatePermissionCacheAsync(memberId, cancellationToken).ConfigureAwait(false);
            }
        }

        return await BuildResponseAsync(group, newRoleIds, cancellationToken);
    }

    private async Task<Group> GetGroupAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Groups
            .Include(g => g.GroupRoles)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{id}' not found.")
            {
                MessageKey = "Identity.GroupNotFound",
                MessageArgs = [id],
                ResourceSource = typeof(IdentityResources),
            };
    }

    private async Task ValidateUniqueNameAsync(Guid excludeId, string name, CancellationToken cancellationToken)
    {
        var nameExists = await _dbContext.Groups
            .AnyAsync(g => g.Name == name && g.Id != excludeId, cancellationToken);

        if (nameExists)
        {
            throw new CustomException($"Group with name '{name}' already exists.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict)
            {
                MessageKey = "Identity.GroupNameAlreadyExists",
                MessageArgs = [name],
                ResourceSource = typeof(IdentityResources),
            };
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
            throw new NotFoundException($"Roles not found: {string.Join(", ", invalidRoleIds)}")
            {
                MessageKey = "Identity.RolesNotFoundWithIds",
                MessageArgs = [string.Join(", ", invalidRoleIds)],
                ResourceSource = typeof(IdentityResources),
            };
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
            .AsNoTracking()
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
            CreatedAt = group.CreatedOnUtc
        };
    }
}