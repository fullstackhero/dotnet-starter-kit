using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Features.v1.Internal;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.ChangeVisibility;

public sealed class ChangeFileVisibilityCommandHandler(
    FilesDbContext db,
    FileAccessPolicyRegistry policies,
    ICurrentUser currentUser,
    IStorageService storage)
    : ICommandHandler<ChangeFileVisibilityCommand, FileAssetDto>
{
    public async ValueTask<FileAssetDto> Handle(ChangeFileVisibilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        if (cmd.Visibility is not (Visibility.Public or Visibility.Private))
        {
            throw new CustomException(
                $"Unknown visibility value '{cmd.Visibility}'.",
                errors: null,
                System.Net.HttpStatusCode.BadRequest);
        }

        var f = await db.FileAssets
            .FirstOrDefaultAsync(x => x.Id == cmd.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = currentUser.GetUserId().ToString();
        var policy = policies.Resolve(f.OwnerType)
            ?? throw new ForbiddenException("no policy");
        var ctx = new FileAccessContext(f.Id, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanChangeVisibilityAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("not allowed to change this file's visibility");
        }

        f.ChangeVisibility(cmd.Visibility);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var publicUrl = f.Visibility == Visibility.Public
            ? storage.BuildPublicUrl(f.StorageKey)
            : null;
        return FileAssetMapper.ToDto(f, publicUrl);
    }
}
