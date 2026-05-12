using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Features.v1.Internal;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Features.v1.GetFileMetadata;

public sealed class GetFileMetadataQueryHandler(
    FilesDbContext db,
    FileAccessPolicyRegistry policies,
    ICurrentUser currentUser,
    IStorageService storage,
    IOptions<FilesOptions> options)
    : IQueryHandler<GetFileMetadataQuery, FileAssetDto>
{
    public async ValueTask<FileAssetDto> Handle(GetFileMetadataQuery q, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(q);

        var f = await db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = currentUser.GetUserId().ToString();
        var policy = policies.Resolve(f.OwnerType)
            ?? throw new NotFoundException("file not found"); // don't leak existence on missing policy

        var ctx = new FileAccessContext(f.Id, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("file not found");
        }

        string? publicUrl = null;
        if (f.Visibility == Visibility.Public)
        {
            var uri = await storage.GenerateDownloadUrlAsync(
                f.StorageKey,
                TimeSpan.FromMinutes(options.Value.DownloadUrlTtlMinutes),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            publicUrl = uri.ToString();
        }

        return FileAssetMapper.ToDto(f, publicUrl);
    }
}
