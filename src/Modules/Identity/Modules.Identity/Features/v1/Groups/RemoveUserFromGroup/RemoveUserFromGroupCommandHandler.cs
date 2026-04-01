using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public sealed class RemoveUserFromGroupCommandHandler(IdentityDbContext dbContext) : ICommandHandler<RemoveUserFromGroupCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveUserFromGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await dbContext.UserGroups
            .FirstOrDefaultAsync(ug => ug.GroupId == command.GroupId && ug.UserId == command.UserId, cancellationToken) ?? throw new NotFoundException($"User '{command.UserId}' is not a member of group '{command.GroupId}'.");
        dbContext.UserGroups.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}