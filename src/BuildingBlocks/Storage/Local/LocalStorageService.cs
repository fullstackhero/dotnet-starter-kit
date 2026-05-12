using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage.DTOs;
using FSH.Framework.Storage.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.RegularExpressions;

namespace FSH.Framework.Storage.Local;

public class LocalStorageService : IStorageService
{
    private const string UploadBasePath = "uploads";
    private readonly string _rootPath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public LocalStorageService(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _rootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    public async Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var rules = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed: {string.Join(", ", rules.AllowedExtensions)}");
        }

        if (request.Data.Count > rules.MaxSizeInMB * 1024 * 1024)
        {
            throw new InvalidOperationException($"File exceeds max size of {rules.MaxSizeInMB} MB.");
        }

#pragma warning disable CA1308 // folder names are intentionally lower-case for URLs/paths
        var folder = Regex.Replace(typeof(T).Name.ToLowerInvariant(), @"[^a-z0-9]", "_");
#pragma warning restore CA1308
        var safeFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(request.FileName)}";
        var relativePath = Path.Combine(UploadBasePath, folder, safeFileName);
        var fullPath = Path.Combine(_rootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, request.Data.ToArray(), cancellationToken);

        return relativePath.Replace("\\", "/", StringComparison.Ordinal); // Normalize for URLs
    }

    public Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult<FileDownloadResponse?>(null);
        }

        var normalizedPath = path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileDownloadResponse?>(null);
        }

        var fileInfo = new FileInfo(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);

        return Task.FromResult<FileDownloadResponse?>(new FileDownloadResponse
        {
            Stream = stream,
            ContentType = contentType,
            FileName = fileName,
            ContentLength = fileInfo.Length
        });
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        var normalizedPath = path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);

        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(0L);
        }

        var normalizedPath = path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(0L);
        }

        return Task.FromResult(new FileInfo(fullPath).Length);
    }

    public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return Task.CompletedTask;

        var fullPath = Path.Combine(_rootPath, path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string fileName)
    {
        return Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
    }

    // Local presigning is a development fallback when Storage:Provider != s3. Production deployments
    // use S3StorageService. The token store is process-static so the dev middleware (registered in
    // the host when Provider=local) can consume the token without re-resolving DI scope.
    private static LocalPresignTokenStore? _staticTokenStore;
    public static LocalPresignTokenStore SharedTokenStore => _staticTokenStore ??= new LocalPresignTokenStore();

    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey, string contentType, long maxBytes, TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var token = SharedTokenStore.Issue(storageKey, contentType, maxBytes, ttl);
        var url = new Uri($"local://upload/{token}", UriKind.Absolute);
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["Content-Type"] = contentType };
        return Task.FromResult(new PresignedUploadUrl(url, headers, DateTimeOffset.UtcNow.Add(ttl)));
    }

    public Task<Uri> GenerateDownloadUrlAsync(
        string storageKey, TimeSpan ttl, string? responseContentDisposition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        // Local mode serves files from /wwwroot — no signing required.
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult(new Uri($"/{normalized}", UriKind.Relative));
    }

    public Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult<StoredObjectMetadata?>(null);
        }

        var normalizedPath = storageKey.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<StoredObjectMetadata?>(null);
        }

        var info = new FileInfo(fullPath);
        if (!_contentTypeProvider.TryGetContentType(info.Name, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return Task.FromResult<StoredObjectMetadata?>(new StoredObjectMetadata(
            info.Length,
            contentType!,
            new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            ETag: null));
    }
}