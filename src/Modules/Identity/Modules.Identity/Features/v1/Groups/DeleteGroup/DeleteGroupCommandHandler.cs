using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Groups.DeleteGroup;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.DeleteGroup;

public sealed class DeleteGroupCommandHandler : ICommandHandler<DeleteGroupCommand, Unit>
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUserPermissionService _userPermissionService;

    public DeleteGroupCommandHandler(IdentityDbContext dbContext, ICurrentUser currentUser, IUserPermissionService userPermissionService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _userPermissionService = userPermissionService;
    }

    public async ValueTask<Unit> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await _dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{command.Id}' not found.");

        if (group.IsSystemGroup)
        {
            throw new ForbiddenException("System groups cannot be deleted.");
        }

        // Snapshot members BEFORE delete — the soft-delete flips IsDeleted but
        // membership rows persist, so this lookup works either way; we capture
        // first for clarity.
        var memberIds = await _dbContext.UserGroups
            .Where(ug => ug.GroupId == command.Id)
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);

        // Soft delete via domain method
        group.Delete(_currentUser.GetUserId().ToString());

        await _dbContext.SaveChangesAsync(cancellationToken);

        // A deleted group can no longer contribute its roles to members' effective
        // permission sets — flush each member's cached entry.
        foreach (var userId in memberIds)
        {
            await _userPermissionService.InvalidatePermissionCacheAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}