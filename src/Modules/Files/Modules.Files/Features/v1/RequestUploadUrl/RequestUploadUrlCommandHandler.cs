using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Localization;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Features.v1.RequestUploadUrl;

public sealed class RequestUploadUrlCommandHandler(
    FilesDbContext db,
    IStorageService storage,
    FileAccessPolicyRegistry policies,
    IQuotaService quotas,
    ICurrentUser currentUser,
    IOptions<FilesOptions> options)
    : ICommandHandler<RequestUploadUrlCommand, PresignedUploadResponse>
{
    public async ValueTask<PresignedUploadResponse> Handle(RequestUploadUrlCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var tenantId = currentUser.GetTenant() ?? throw new UnauthorizedException("invalid tenant")
        {
            MessageKey = "Error.InvalidTenant",
        };
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("no current user")
            {
                MessageKey = "Error.NoCurrentUser",
            };
        }

        // Category lookup + extension/size validation.
        if (!options.Value.Categories.TryGetValue(cmd.Category, out var category))
        {
            throw new CustomException($"Unknown category '{cmd.Category}'.", (IEnumerable<string>?)null, HttpStatusCode.BadRequest)
            {
                MessageKey = "Files.UnknownCategory",
                MessageArgs = [cmd.Category],
                ResourceSource = typeof(FilesResources),
            };
        }

        var extension = Path.GetExtension(cmd.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !category.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new CustomException(
                $"Extension '{extension}' not allowed for category '{cmd.Category}'.",
                (IEnumerable<string>?)null,
                HttpStatusCode.BadRequest)
            {
                MessageKey = "Files.ExtensionNotAllowed",
                MessageArgs = [extension, cmd.Category],
                ResourceSource = typeof(FilesResources),
            };
        }

        if (cmd.SizeBytes > category.MaxBytes)
        {
            throw new CustomException(
                $"File exceeds max size of {category.MaxBytes} bytes for category '{cmd.Category}'.",
                (IEnumerable<string>?)null,
                HttpStatusCode.BadRequest)
            {
                MessageKey = "Files.FileExceedsMaxSize",
                MessageArgs = [category.MaxBytes, cmd.Category],
                ResourceSource = typeof(FilesResources),
            };
        }

        // Authorization: policy must exist and allow the attach.
        var policy = policies.Resolve(cmd.OwnerType)
            ?? throw new ForbiddenException($"No file access policy registered for owner type '{cmd.OwnerType}'.")
            {
                MessageKey = "Files.NoPolicyForOwnerType",
                MessageArgs = [cmd.OwnerType],
                ResourceSource = typeof(FilesResources),
            };
        if (!await policy.CanAttachAsync(cmd.OwnerId, userId.ToString(), cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("Not allowed to attach files to this owner.")
            {
                MessageKey = "Files.NotAllowedToAttach",
                ResourceSource = typeof(FilesResources),
            };
        }

        // Quota pre-check (no debit yet — debit happens on finalize with actual bytes).
        var quotaCheck = await quotas.CheckAsync(tenantId, QuotaResource.StorageBytes, cmd.SizeBytes, cancellationToken).ConfigureAwait(false);
        if (!quotaCheck.Allowed)
        {
            throw new CustomException(
                $"Storage quota exceeded ({quotaCheck.CurrentUsage}/{quotaCheck.Limit} bytes).",
                (IEnumerable<string>?)null,
                (HttpStatusCode)507)
            {
                MessageKey = "Files.StorageQuotaExceeded",
                MessageArgs = [quotaCheck.CurrentUsage, quotaCheck.Limit],
                ResourceSource = typeof(FilesResources),
            };
        }

        // Generate id + storage key + presigned URL.
        var id = Guid.CreateVersion7();
        var storageKey = StorageKeyBuilder.Build(tenantId, cmd.OwnerType, id, cmd.FileName, DateTimeOffset.UtcNow);
        var ttl = TimeSpan.FromMinutes(options.Value.UploadUrlTtlMinutes);
        var presigned = await storage.GenerateUploadUrlAsync(storageKey, cmd.ContentType, category.MaxBytes, ttl, cancellationToken).ConfigureAwait(false);

        var asset = FileAsset.CreatePending(
            id: id,
            ownerType: cmd.OwnerType,
            ownerId: cmd.OwnerId,
            originalFileName: cmd.FileName,
            sanitizedFileName: StorageKeyBuilder.Sanitize(cmd.FileName),
            contentType: cmd.ContentType,
            declaredSizeBytes: cmd.SizeBytes,
            storageKey: storageKey,
            visibility: cmd.Visibility,
            createdByUserId: userId.ToString(),
            uploadDeadline: DateTimeOffset.UtcNow.Add(ttl));

        db.FileAssets.Add(asset);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PresignedUploadResponse(asset.Id, presigned.Url, presigned.RequiredHeaders, presigned.ExpiresAt);
    }
}
