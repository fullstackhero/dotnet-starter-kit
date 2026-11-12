using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage.DTOs;
using FSH.Framework.Storage.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using System.Threading;

namespace FSH.Framework.Storage.Local;

public sealed class LocalStorageService : IStorageService
{
    // Every object this service creates or moves lives under one of these roots. Keys anywhere else in
    // the web root (the SPA, other static assets) are not storage objects and are never touched.
    private static readonly string[] ManagedRoots =
    [
        StorageUploadKeys.UploadBasePath + "/",
        StorageVisibilityRoot.Public,
        StorageVisibilityRoot.Private,
    ];

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _rootPath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public LocalStorageService(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath);
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

        if (!FileSignatures.Matches(extension, request.Data))
        {
            throw new InvalidOperationException($"File content does not match its '{extension}' extension.");
        }

        var relativePath = StorageUploadKeys.NewKey<T>(request.FileName);
        var fullPath = ResolveOrThrow(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, request.Data.ToArray(), cancellationToken);

        return relativePath;
    }

    public Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!TryResolve(path, out var fullPath) || !File.Exists(fullPath))
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

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(TryResolve(path, out var fullPath) && File.Exists(fullPath));

    public Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(TryResolve(path, out var fullPath) && File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L);

    public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        // A key that does not resolve to a storage object is treated like a missing one: callers
        // delete best-effort, and an out-of-root path must not become a delete anywhere else.
        if (TryResolve(path, out var fullPath) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationKey);
        cancellationToken.ThrowIfCancellationRequested();

        var sourcePath = ResolveOrThrow(sourceKey);
        var destinationPath = ResolveOrThrow(destinationKey);

        // Callers delete the source after a copy, so a failed copy must not look like a successful one.
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Cannot copy '{sourceKey}': the object does not exist.", sourceKey);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return Task.CompletedTask;
    }

    private string ResolveOrThrow(string key) =>
        TryResolve(key, out var fullPath)
            ? fullPath
            : throw new ArgumentException($"'{key}' is not a storage key this service manages.", nameof(key));

    /// <summary>
    /// Maps a storage key to a file under the web root, refusing anything that could land elsewhere.
    /// </summary>
    /// <remarks>
    /// Keys reach this service from stored database values and, through callers, ultimately from
    /// clients, so each one is treated as hostile:
    /// <list type="bullet">
    ///   <item>no <c>:</c> — drive letters (<c>C:\…</c>), URI schemes and NTFS alternate data streams;</item>
    ///   <item>no empty, <c>.</c> or <c>..</c> segments, and none ending in a dot or space, which Windows strips;</item>
    ///   <item>it must sit under one of the storage roots (uploads/, public/, private/);</item>
    ///   <item>and the fully resolved path must still be inside the web root.</item>
    /// </list>
    /// <c>Path.Combine</c> alone is not a guard: when its second argument is rooted it discards the first.
    /// </remarks>
    private bool TryResolve(string? key, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var normalized = key.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' '))
            {
                return false;
            }
        }

        if (!ManagedRoots.Any(root => normalized.StartsWith(root, StringComparison.Ordinal)))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(_rootPath + Path.DirectorySeparatorChar, PathComparison))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    // Dev-only presigning fallback when Storage:Provider != s3 (prod uses S3StorageService). Token
    // store is process-static so the dev middleware can consume the token without re-resolving DI.
    private static LocalPresignTokenStore? _staticTokenStore;
    public static LocalPresignTokenStore SharedTokenStore => LazyInitializer.EnsureInitialized(ref _staticTokenStore);

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

#pragma warning disable CA1055 // returns a server-relative path, not a well-formed Uri — see IStorageService.BuildPublicUrl
    public string BuildPublicUrl(string storageKey)
#pragma warning restore CA1055
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        // Resolved later against the dashboard's API origin (as UserProfileService does for legacy
        // avatars). The leading slash lets clients distinguish absolute from server-relative URLs.
        return $"/{normalized}";
    }

    public Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolve(storageKey, out var fullPath) || !File.Exists(fullPath))
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
            contentType,
            new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            ETag: null));
    }
}
