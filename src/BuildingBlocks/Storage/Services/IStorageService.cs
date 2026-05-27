using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage.DTOs;

namespace FSH.Framework.Storage.Services;

public interface IStorageService
{
    Task<string> UploadAsync<T>(
        FileUploadRequest request,
        FileType fileType,
        CancellationToken cancellationToken = default) where T : class;

    Task<FileDownloadResponse?> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the size in bytes of the object at <paramref name="path"/>, or 0 if it does not exist.
    /// Used by quota metering to debit storage usage on delete without requiring callers to track sizes.
    /// </summary>
    Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default);

    Task RemoveAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mint a short-lived presigned PUT URL the browser uses to upload bytes directly to S3-compatible storage.
    /// Returns the URL plus any headers the browser MUST include verbatim in its PUT (typically Content-Type
    /// when the signature constrains it). Used by the Files module's <c>RequestUploadUrl</c> endpoint.
    /// </summary>
    Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey,
        string contentType,
        long maxBytes,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mint a short-lived presigned GET URL. When <paramref name="responseContentDisposition"/> is
    /// supplied, S3 echoes it in the download response so the browser surfaces the original filename
    /// rather than the storage key.
    /// </summary>
    Task<Uri> GenerateDownloadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        string? responseContentDisposition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HEAD the object at <paramref name="storageKey"/>. Returns <c>null</c> when the object does not
    /// exist. The Files module's finalize handler uses this to verify size + content-type vs declared
    /// values before transitioning a row out of <c>PendingUpload</c>.
    /// </summary>
    Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute a durable, non-expiring public URL for an object. Used when a <c>FileAsset</c> with
    /// <c>Visibility=Public</c> is consumed by a long-lived persisted reference (e.g. a Product's
    /// <c>imageUrl</c>) where a presigned 5-minute URL would expire shortly after save.
    ///
    /// S3 backends build this from <c>PublicBaseUrl</c> (or the bucket's S3 host) and assume the
    /// bucket policy grants public-read on the object. Local storage returns a path relative to the
    /// API origin's wwwroot. Callers that want auth-gated access should use
    /// <see cref="GenerateDownloadUrlAsync"/> instead.
    /// </summary>
    /// <remarks>
    /// Returns <c>string</c> intentionally — local storage produces a server-relative path
    /// (resolved later by the client against the API origin) which is not a well-formed Uri.
    /// </remarks>
#pragma warning disable CA1055 // Uri vs string — see remarks above
    string BuildPublicUrl(string storageKey);
#pragma warning restore CA1055
}