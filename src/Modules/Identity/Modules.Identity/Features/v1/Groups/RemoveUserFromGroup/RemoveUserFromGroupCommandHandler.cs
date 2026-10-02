using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public sealed class RemoveUserFromGroupCommandHandler : ICommandHandler<RemoveUserFromGroupCommand, Unit>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IUserPermissionService _userPermissionService;

    public RemoveUserFromGroupCommandHandler(IdentityDbContext dbContext, IUserPermissionService userPermissionService)
    {
        _dbContext = dbContext;
        _userPermissionService = userPermissionService;
    }

    public async ValueTask<Unit> Handle(RemoveUserFromGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await _dbContext.UserGroups
            .Include(ug => ug.Group)
            .FirstOrDefaultAsync(ug => ug.GroupId == command.GroupId && ug.UserId == command.UserId, cancellationToken);

        if (membership is null)
        {
            throw new NotFoundException($"User '{command.UserId}' is not a member of group '{command.GroupId}'.")
            {
                MessageKey = "Identity.UserNotMemberOfGroup",
                MessageArgs = [command.UserId, command.GroupId],
                ResourceSource = typeof(IdentityResources),
            };
        }

        // Default groups (e.g. seeded "All Users") require every tenant user to be a member, so
        // removing one breaks that invariant and leaves later registrants in a half-populated group.
        if (membership.Group is not null && membership.Group.IsDefault)
        {
            throw new ForbiddenException("Users cannot be removed from a default group.")
            {
                MessageKey = "Identity.CannotRemoveFromDefaultGroup",
                ResourceSource = typeof(IdentityResources),
            };
        }

        _dbContext.UserGroups.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Leaving a group may revoke roles the user only held through this group —
        // invalidate so the cached permission set is rebuilt on next request.
        await _userPermissionService.InvalidatePermissionCacheAsync(command.UserId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}