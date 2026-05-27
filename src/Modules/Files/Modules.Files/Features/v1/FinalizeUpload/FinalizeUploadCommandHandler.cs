using System.Diagnostics;
using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Features.v1.Internal;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.FinalizeUpload;

public sealed class FinalizeUploadCommandHandler(
    FilesDbContext db,
    IStorageService storage,
    IFileScanner scanner,
    IQuotaService quotas,
    IEventBus events,
    ICurrentUser currentUser)
    : ICommandHandler<FinalizeUploadCommand, FileAssetDto>
{
    public async ValueTask<FileAssetDto> Handle(FinalizeUploadCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = currentUser.GetTenant() ?? throw new UnauthorizedException("invalid tenant");
        var userId = currentUser.GetUserId().ToString();

        var asset = await db.FileAssets
            .FirstOrDefaultAsync(f => f.Id == cmd.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        if (!string.Equals(asset.CreatedByUserId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("not your pending file");
        }
        if (asset.Status != FileAssetStatus.PendingUpload)
        {
            throw new CustomException("file already finalized", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var head = await storage.HeadObjectAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("upload not received", (IEnumerable<string>?)null, HttpStatusCode.Conflict);

        // Allow declared+1% slack (S3 may differ slightly on multipart). Reject larger sizes.
        var maxAllowed = asset.SizeBytes + Math.Max(1024L, asset.SizeBytes / 100);
        if (head.SizeBytes > maxAllowed)
        {
            await storage.RemoveAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false);
            db.FileAssets.Remove(asset);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new CustomException(
                $"uploaded size ({head.SizeBytes}) exceeds declared ({asset.SizeBytes})",
                (IEnumerable<string>?)null,
                HttpStatusCode.BadRequest);
        }

        if (!string.Equals(head.ContentType, asset.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            await storage.RemoveAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false);
            db.FileAssets.Remove(asset);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new CustomException(
                "uploaded content-type mismatch",
                (IEnumerable<string>?)null,
                HttpStatusCode.BadRequest);
        }

        var scanResult = await scanner.ScanAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false);
        asset.MarkAvailable(head.SizeBytes, scanResult);

        // Debit quota with the actual bytes. Refunded on hard purge by PurgeDeletedFilesJob.
        await quotas.RecordAsync(tenantId, QuotaResource.StorageBytes, head.SizeBytes, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        await events.PublishAsync(new FileFinalizedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: correlationId,
            Source: "Files",
            FileAssetId: asset.Id,
            OwnerType: asset.OwnerType,
            OwnerId: asset.OwnerId,
            ContentType: asset.ContentType,
            SizeBytes: asset.SizeBytes,
            FinalStatus: (int)asset.Status), cancellationToken).ConfigureAwait(false);

        return FileAssetMapper.ToDto(asset);
    }
}
