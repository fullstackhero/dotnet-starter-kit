using Amazon.S3;
using Amazon.S3.Model;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage.DTOs;
using FSH.Framework.Storage.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Storage.S3;

internal sealed class S3StorageService : IStorageService
{
    internal const string PresignClientKey = "FSH.Storage.S3.Presign";

    private readonly IAmazonS3 _s3;
    private readonly IAmazonS3 _presignS3;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    // UploadAsync always hands back a durable public URL, so these objects live under the public root
    // that bucket policies open to anonymous reads. Keys written before the root existed stay "uploads/…".
    private const string UploadRoot = StorageVisibilityRoot.Public;

    public S3StorageService(
        IAmazonS3 s3,
        [FromKeyedServices(PresignClientKey)] IAmazonS3 presignS3,
        IOptions<S3StorageOptions> options,
        ILogger<S3StorageService> logger)
    {
        _s3 = s3;
        _presignS3 = presignS3;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _contentTypeProvider = new FileExtensionContentTypeProvider();

        if (string.IsNullOrWhiteSpace(_options.Bucket))
        {
            throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
        }
    }

    public async Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var rules = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
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

        var key = BuildKey<T>(request.FileName);

        // The bucket serves the object with the Content-Type stored here, so it comes from the validated
        // extension, never from the client: a client-declared text/html on "avatar.png" is stored XSS.
        if (!_contentTypeProvider.TryGetContentType(key, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        using var stream = new MemoryStream([.. request.Data]);

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        // Rely on bucket policy for public access; do not set ACLs to avoid conflicts with ACL-disabled buckets.
        await _s3.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Uploaded file to S3 bucket {Bucket} with key {Key}", _options.Bucket, key);
        }

        return BuildPublicUrl(key);
    }

    public async Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var key = NormalizeKey(path);
            await _s3.DeleteObjectAsync(_options.Bucket, key, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error deleting object {Path}: {StatusCode}", path, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error deleting S3 object {Path}", path);
        }
    }

    public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationKey);

        var source = NormalizeKey(sourceKey);
        var destination = NormalizeKey(destinationKey);

        // CopyObject keeps Content-Type and user metadata (MetadataDirective COPY is the default).
        await _s3.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = _options.Bucket,
            SourceKey = source,
            DestinationBucket = _options.Bucket,
            DestinationKey = destination
        }, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Copied S3 object in bucket {Bucket} from {SourceKey} to {DestinationKey}",
                _options.Bucket, source, destination);
        }
    }

    public async Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var key = NormalizeKey(path);
            var request = new GetObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key
            };

            var response = await _s3.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
            var fileName = Path.GetFileName(key);

            // Use response ContentType if available, otherwise determine from extension
            var contentType = response.Headers.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) && !_contentTypeProvider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            return new FileDownloadResponse
            {
                Stream = response.ResponseStream,
                ContentType = contentType!,
                FileName = fileName,
                ContentLength = response.ContentLength
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(ex, "S3 object not found: {Path}", path);
            }
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error downloading object {Path}: {StatusCode}", path, ex.StatusCode);
            return null;
        }
        // Fallback for unexpected non-S3 errors (e.g., network, serialization).
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error downloading S3 object {Path}", path);
            return null;
        }
    }

    public async Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        try
        {
            var key = NormalizeKey(path);
            var metadata = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken).ConfigureAwait(false);

            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return 0;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error reading object size {Path}: {StatusCode}", path, ex.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error reading S3 object size: {Path}", path);
            return 0;
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var key = NormalizeKey(path);
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            };

            await _s3.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error checking object existence {Path}: {StatusCode}", path, ex.StatusCode);
            return false;
        }
        // Fallback for unexpected non-S3 errors (e.g., network, configuration).
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error checking if S3 object exists: {Path}", path);
            return false;
        }
    }

    public async Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey,
        string contentType,
        long maxBytes,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var key = NormalizeKey(storageKey);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
            Protocol = ResolvePresignProtocol()
        };

        var url = await _presignS3.GetPreSignedURLAsync(request).ConfigureAwait(false);

        var requiredHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Content-Type"] = contentType
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Issued presigned PUT URL for bucket {Bucket} key {Key} expires {ExpiresAt}",
                _options.Bucket, key, expiresAt);
        }

        return new PresignedUploadUrl(new Uri(url), requiredHeaders, expiresAt);
    }

    public async Task<Uri> GenerateDownloadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        string? responseContentDisposition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var key = NormalizeKey(storageKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = ResolvePresignProtocol()
        };

        if (!string.IsNullOrWhiteSpace(responseContentDisposition))
        {
            request.ResponseHeaderOverrides.ContentDisposition = responseContentDisposition;
        }

        var url = await _presignS3.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    public async Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        try
        {
            var key = NormalizeKey(storageKey);
            var metadata = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken).ConfigureAwait(false);

            var contentType = string.IsNullOrWhiteSpace(metadata.Headers.ContentType)
                ? "application/octet-stream"
                : metadata.Headers.ContentType;

            var lastModified = metadata.LastModified.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(metadata.LastModified.Value, DateTimeKind.Utc), TimeSpan.Zero)
                : DateTimeOffset.UtcNow;

            return new StoredObjectMetadata(
                metadata.ContentLength,
                contentType,
                lastModified,
                metadata.ETag);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 HEAD failed for {Key}: {StatusCode}", storageKey, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error on S3 HEAD for {Key}", storageKey);
            return null;
        }
    }

    // MinIO and other S3-compatibles often serve plain HTTP, but the SDK defaults presigned URLs to
    // HTTPS regardless of ServiceURL scheme (un-PUTable there). Infer protocol from the presign endpoint.
    private Protocol ResolvePresignProtocol()
    {
        var endpoint = _options.PresignEndpoint;
        if (!string.IsNullOrWhiteSpace(endpoint)
            && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return Protocol.HTTP;
        }
        return Protocol.HTTPS;
    }

    private string BuildKey<T>(string fileName) where T : class
    {
        var relativePath = UploadRoot + StorageUploadKeys.NewKey<T>(fileName);
        if (!string.IsNullOrWhiteSpace(_options.Prefix))
        {
            return $"{_options.Prefix.TrimEnd('/')}/{relativePath}";
        }

        return relativePath;
    }

    public string BuildPublicUrl(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var key = NormalizeKey(storageKey);
        var safeKey = key.TrimStart('/');

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{safeKey}";
        }

        // S3-compatible endpoint with path-style addressing (MinIO and friends).
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.Bucket}/{safeKey}";
        }

        if (!_options.PublicRead)
        {
            return key;
        }

        if (string.IsNullOrWhiteSpace(_options.Region) || string.Equals(_options.Region, "us-east-1", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{_options.Bucket}.s3.amazonaws.com/{safeKey}";
        }

        return $"https://{_options.Bucket}.s3.{_options.Region}.amazonaws.com/{safeKey}";
    }

    private string NormalizeKey(string path)
    {
        // If a full URL was passed, strip host and query to get the object key.
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }

        var trimmed = path.TrimStart('/');

        // S3 itself treats "." and ".." as literal key characters, but S3-compatible servers, proxies
        // and CDNs in front of a bucket may normalise them. A key is never built with them, so refuse.
        if (trimmed.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException($"'{path}' is not a valid storage key.", nameof(path));
        }
        if (!string.IsNullOrWhiteSpace(_options.Prefix) && trimmed.StartsWith(_options.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (!string.IsNullOrWhiteSpace(_options.Prefix))
        {
            return $"{_options.Prefix.TrimEnd('/')}/{trimmed}";
        }

        return trimmed;
    }
}