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
}