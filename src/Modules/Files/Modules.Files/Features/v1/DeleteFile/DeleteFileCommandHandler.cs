using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.DeleteFile;

internal sealed class DeleteFileCommandHandler(
    FilesDbContext db,
    FileAccessPolicyRegistry policies,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteFileCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteFileCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var f = await db.FileAssets
            .FirstOrDefaultAsync(x => x.Id == cmd.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = currentUser.GetUserId().ToString();
        var policy = policies.Resolve(f.OwnerType)
            ?? throw new ForbiddenException("no policy");
        var ctx = new FileAccessContext(f.Id, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanDeleteAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("not allowed to delete this file");
        }

        // Soft-delete: the framework's AuditableEntitySaveChangesInterceptor sets IsDeleted +
        // DeletedOnUtc + DeletedBy on Remove() calls for ISoftDeletable entities. The byte purge
        // happens later via PurgeDeletedFilesJob after the retention window.
        db.FileAssets.Remove(f);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
