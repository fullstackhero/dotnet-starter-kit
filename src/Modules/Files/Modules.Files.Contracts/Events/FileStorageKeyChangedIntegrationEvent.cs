using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Files.Contracts.v1.DTOs;

namespace FSH.Modules.Files.Contracts.Events;

/// <summary>
/// Raised when a FileAsset's object moves to a new storage key — its visibility changed, or a file
/// uploaded before visibility lived in the key was moved under the <c>public/</c> root. The old key is
/// deleted once this event is committed, so any module that persisted a URL built from it (a product
/// image, a user's avatar) must rewrite that URL: <see cref="RewriteUrl"/> does the rewrite and keeps
/// whatever base the URL was built with.
/// </summary>
public sealed record FileStorageKeyChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid FileAssetId,
    string OwnerType,
    Guid? OwnerId,
    string OldStorageKey,
    string NewStorageKey,
    Visibility Visibility) : IIntegrationEvent
{
    /// <summary>
    /// Returns <paramref name="url"/> pointed at <see cref="NewStorageKey"/> when it addresses
    /// <see cref="OldStorageKey"/> (public URLs end with the key, whatever the base URL or bucket
    /// prefix), or <c>null</c> when the URL is for some other object and should be left alone.
    /// </summary>
#pragma warning disable CA1055 // persisted URLs are strings (local storage produces server-relative paths)
    public string? RewriteUrl(string? url)
#pragma warning restore CA1055
    {
        if (string.IsNullOrWhiteSpace(url) || !url.EndsWith(OldStorageKey, StringComparison.Ordinal))
        {
            return null;
        }

        // Guard against a key that merely shares a suffix with another path segment.
        var prefixLength = url.Length - OldStorageKey.Length;
        if (prefixLength > 0 && url[prefixLength - 1] != '/')
        {
            return null;
        }

        return string.Concat(url.AsSpan(0, prefixLength), NewStorageKey);
    }
}
