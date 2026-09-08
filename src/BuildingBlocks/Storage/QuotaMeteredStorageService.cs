using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage.DTOs;
using FSH.Framework.Storage.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Storage;

/// <summary>
/// Decorates <see cref="IStorageService"/> so every upload charges the tenant's
/// <see cref="QuotaResource.StorageBytes"/> meter and every delete refunds it. When no tenant is
/// resolved on the request we pass through unmetered — this matches the middleware's posture of
/// only enforcing for tenanted traffic. Upload failures roll the counter back so a partial PUT
/// can't leave an inflated balance.
/// </summary>
internal sealed class QuotaMeteredStorageService : IStorageService
{
    private readonly IStorageService _inner;
    private readonly IQuotaService _quotas;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ILogger<QuotaMeteredStorageService> _logger;

    public QuotaMeteredStorageService(
        IStorageService inner,
        IQuotaService quotas,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ILogger<QuotaMeteredStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(quotas);
        ArgumentNullException.ThrowIfNull(tenantAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _quotas = quotas;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return await _inner.UploadAsync<T>(request, fileType, cancellationToken).ConfigureAwait(false);
        }

        var bytes = request.Data.Count;
        var check = await _quotas
            .CheckAndRecordAsync(tenantId, QuotaResource.StorageBytes, bytes, cancellationToken)
            .ConfigureAwait(false);

        if (!check.Allowed)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Rejected upload for tenant {TenantId} — storage quota exceeded ({Current}/{Limit} bytes)",
                    tenantId, check.CurrentUsage, check.Limit);
            }

            throw new CustomException(
                $"Storage quota exceeded ({check.CurrentUsage}/{check.Limit} bytes).",
                errors: null,
                HttpStatusCode.InsufficientStorage)
            {
                MessageKey = "Storage.QuotaExceeded",
                MessageArgs = [check.CurrentUsage, check.Limit],
            };
        }

        try
        {
            return await _inner.UploadAsync<T>(request, fileType, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Roll the charge back so a failed write doesn't permanently consume quota.
            await _quotas
                .RecordAsync(tenantId, QuotaResource.StorageBytes, -bytes, CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    public Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
        => _inner.DownloadAsync(path, cancellationToken);

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        => _inner.ExistsAsync(path, cancellationToken);

    public Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
        => _inner.GetSizeAsync(path, cancellationToken);

    public async Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        // Probe size before delete so we can debit the exact amount. Missing objects report 0.
        long size = 0;
        if (!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(path))
        {
            size = await _inner.GetSizeAsync(path, cancellationToken).ConfigureAwait(false);
        }

        await _inner.RemoveAsync(path, cancellationToken).ConfigureAwait(false);

        if (size > 0 && !string.IsNullOrWhiteSpace(tenantId))
        {
            await _quotas
                .RecordAsync(tenantId, QuotaResource.StorageBytes, -size, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    // Presigned URL minting + HEAD pass through — no bytes move, so quota isn't touched. The Files
    // module debits on finalize (size from HEAD) and refunds on hard purge (via RemoveAsync above).
    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(string storageKey, string contentType, long maxBytes, TimeSpan ttl, CancellationToken cancellationToken = default)
        => _inner.GenerateUploadUrlAsync(storageKey, contentType, maxBytes, ttl, cancellationToken);

    public Task<Uri> GenerateDownloadUrlAsync(string storageKey, TimeSpan ttl, string? responseContentDisposition = null, CancellationToken cancellationToken = default)
        => _inner.GenerateDownloadUrlAsync(storageKey, ttl, responseContentDisposition, cancellationToken);

    public Task<StoredObjectMetadata?> HeadObjectAsync(string storageKey, CancellationToken cancellationToken = default)
        => _inner.HeadObjectAsync(storageKey, cancellationToken);

    public string BuildPublicUrl(string storageKey) => _inner.BuildPublicUrl(storageKey);
}
