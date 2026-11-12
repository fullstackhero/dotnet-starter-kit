using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Localization;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace FSH.Modules.Files.Features.v1.GetFileDownloadUrl;

public sealed class GetFileDownloadUrlQueryHandler(
    FilesDbContext db,
    IStorageService storage,
    FileAccessPolicyRegistry policies,
    ICurrentUser currentUser,
    IOptions<FilesOptions> options)
    : IQueryHandler<GetFileDownloadUrlQuery, PresignedDownloadResponse>
{
    public async ValueTask<PresignedDownloadResponse> Handle(GetFileDownloadUrlQuery q, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(q);

        var f = await db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found").Localized<FilesResources>("Files.FileNotFound");

        var userId = currentUser.GetUserId().ToString();
        var policy = policies.Resolve(f.OwnerType)
            ?? throw new NotFoundException("file not found").Localized<FilesResources>("Files.FileNotFound");

        var ctx = new FileAccessContext(f.Id, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("file not found").Localized<FilesResources>("Files.FileNotFound");
        }

        var ttl = TimeSpan.FromMinutes(options.Value.DownloadUrlTtlMinutes);
        // The original name is client-supplied: quote it properly (RFC 6266 / 5987) rather than
        // splicing it into the header, where a '"' or ';' would rewrite the disposition.
        var disposition = new ContentDispositionHeaderValue(q.Inline ? "inline" : "attachment")
        {
            FileName = HeaderUtilities.EscapeAsQuotedString(f.FileName),
            FileNameStar = f.OriginalFileName,
        }.ToString();
        var url = await storage.GenerateDownloadUrlAsync(f.StorageKey, ttl, disposition, cancellationToken).ConfigureAwait(false);
        return new PresignedDownloadResponse(url, DateTimeOffset.UtcNow.Add(ttl));
    }
}
