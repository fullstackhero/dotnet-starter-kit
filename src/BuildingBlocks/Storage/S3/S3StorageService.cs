using Amazon.S3;
using Amazon.S3.Model;
using FSH.Framework.Storage.DTOs;
using FSH.Framework.Storage.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.RegularExpressions;

namespace FSH.Framework.Storage.S3;

internal sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;

    private const string UploadBasePath = "uploads";

    public S3StorageService(IAmazonS3 s3, IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger)
    {
        _s3 = s3;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

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

        var key = BuildKey<T>(SanitizeFileName(request.FileName));

        using var stream = new MemoryStream([.. request.Data]);

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = request.ContentType
        };

        // Rely on bucket policy for public access; do not set ACLs to avoid conflicts with ACL-disabled buckets.
        await _s3.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Uploaded file to S3 bucket {Bucket} with key {Key}", _options.Bucket, key);

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete S3 object {Path}", path);
        }
    }

    private string BuildKey<T>(string fileName) where T : class
    {
        var folder = Regex.Replace(typeof(T).Name.ToLowerInvariant(), @"[^a-z0-9]", "_");
        var relativePath = Path.Combine(UploadBasePath, folder, $"{Guid.NewGuid():N}_{fileName}").Replace("\\", "/", StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(_options.Prefix))
        {
            return $"{_options.Prefix.TrimEnd('/')}/{relativePath}";
        }

        return relativePath;
    }

    private string BuildPublicUrl(string key)
    {
        var safeKey = key.TrimStart('/');

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{safeKey}";
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

    private static string SanitizeFileName(string fileName)
    {
        return Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
    }
}
