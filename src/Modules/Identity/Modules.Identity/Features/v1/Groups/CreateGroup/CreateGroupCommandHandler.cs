using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.CreateGroup;

public sealed class CreateGroupCommandHandler : ICommandHandler<CreateGroupCommand, GroupDto>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateGroupCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<GroupDto> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate name is unique within tenant
        var nameExists = await _dbContext.Groups
            .AnyAsync(g => g.Name == command.Name, cancellationToken);

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

        var group = Group.Create(
            name: command.Name,
            description: command.Description,
            isDefault: command.IsDefault,
            isSystemGroup: false,
            createdBy: _currentUser.GetUserId().ToString());

        // Add role assignments
        if (command.RoleIds is { Count: > 0 })
        {
            foreach (var roleId in command.RoleIds)
            {
                _dbContext.GroupRoles.Add(GroupRole.Create(group.Id, roleId));
            }
        }

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get role names for response
        var roleNames = command.RoleIds is { Count: > 0 }
            ? await _dbContext.Roles
                .Where(r => command.RoleIds.Contains(r.Id))
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
            MemberCount = 0,
            RoleIds = command.RoleIds?.AsReadOnly(),
            RoleNames = roleNames.AsReadOnly(),
            CreatedAt = group.CreatedAt
        };
    }
}
