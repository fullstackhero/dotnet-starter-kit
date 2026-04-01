using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.Groups.DeleteGroup;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.DeleteGroup;

public sealed class DeleteGroupCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser) : ICommandHandler<DeleteGroupCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{command.Id}' not found.");

        if (group.IsSystemGroup)
        {
            throw new ForbiddenException("System groups cannot be deleted.");
        }

        // Soft delete via domain method
        group.Delete(currentUser.GetUserId().ToString());

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}