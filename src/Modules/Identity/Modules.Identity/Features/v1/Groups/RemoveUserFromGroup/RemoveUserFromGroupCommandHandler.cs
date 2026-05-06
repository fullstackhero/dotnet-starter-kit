using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public sealed class RemoveUserFromGroupCommandHandler : ICommandHandler<RemoveUserFromGroupCommand, Unit>
{
    private readonly IdentityDbContext _dbContext;

    public RemoveUserFromGroupCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(RemoveUserFromGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await _dbContext.UserGroups
            .Include(ug => ug.Group)
            .FirstOrDefaultAsync(ug => ug.GroupId == command.GroupId && ug.UserId == command.UserId, cancellationToken);

        if (membership is null)
        {
            throw new NotFoundException($"User '{command.UserId}' is not a member of group '{command.GroupId}'.");
        }

        // Default groups (e.g. the seeded "All Users") hold the invariant that
        // every user in the tenant is a member. Removing breaks that contract —
        // the next user to register would join a half-populated group.
        if (membership.Group is not null && membership.Group.IsDefault)
        {
            throw new ForbiddenException("Users cannot be removed from a default group.");
        }

        _dbContext.UserGroups.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}