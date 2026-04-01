using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.AddUsersToGroup;

public sealed class AddUsersToGroupCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser) : ICommandHandler<AddUsersToGroupCommand, AddUsersToGroupResponse>
{
    public async ValueTask<AddUsersToGroupResponse> Handle(AddUsersToGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate group exists
        var groupExists = await dbContext.Groups
            .AnyAsync(g => g.Id == command.GroupId, cancellationToken);

        if (!groupExists)
        {
            throw new NotFoundException($"Group with ID '{command.GroupId}' not found.");
        }

        // Validate user IDs exist
        var existingUserIds = await dbContext.Users
            .Where(u => command.UserIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var invalidUserIds = command.UserIds.Except(existingUserIds).ToList();
        if (invalidUserIds.Count > 0)
        {
            throw new NotFoundException($"Users not found: {string.Join(", ", invalidUserIds)}");
        }

        // Get existing memberships
        var existingMemberships = await dbContext.UserGroups
            .Where(ug => ug.GroupId == command.GroupId && command.UserIds.Contains(ug.UserId))
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);

        var alreadyMemberUserIds = existingMemberships.ToList();
        var usersToAdd = command.UserIds.Except(existingMemberships).ToList();

        // Add new memberships
        var currentUserId = currentUser.GetUserId().ToString();
        foreach (var userId in usersToAdd)
        {
            dbContext.UserGroups.Add(UserGroup.Create(userId, command.GroupId, currentUserId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddUsersToGroupResponse(usersToAdd.Count, alreadyMemberUserIds);
    }
}