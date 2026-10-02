using System.Text.RegularExpressions;
using FSH.Framework.Storage;
using FSH.Modules.Files.Contracts.v1.DTOs;

namespace FSH.Modules.Files.Services;

/// <summary>
/// Builds canonical storage keys for the Files module:
///   {public|private}/tenants/{tenantId}/{ownerType-lower}/{yyyy}/{MM}/{fileAssetId:N}/{sanitized-filename}.
/// The root segment carries the file's <see cref="Visibility"/>, and this class is the one place that
/// decides it: bucket policies grant anonymous read on <c>public/*</c> only (see
/// <see cref="StorageVisibilityRoot"/>), so a key under the wrong root is either unreadable or leaks.
/// Tenant prefix is mandatory defense-in-depth even when the bucket is already tenant-scoped via
/// connection-string-per-tenant — keys carry the prefix so cross-tenant key-collision is impossible
/// even if a future deployment puts multiple tenants in one bucket.
/// </summary>
public static partial class StorageKeyBuilder
{
    [GeneratedRegex(@"[^a-zA-Z0-9_\.-]")]
    private static partial Regex UnsafeChars();

    public static string Build(
        string tenantId,
        string ownerType,
        Guid fileAssetId,
        string fileName,
        DateTimeOffset now,
        Visibility visibility)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var root = RootFor(visibility);

#pragma warning disable CA1308 // path segments are intentionally lower-case
        var lowerOwner = ownerType.ToLowerInvariant();
#pragma warning restore CA1308
        var safe = Sanitize(fileName);
        return $"{root}tenants/{tenantId}/{lowerOwner}/{now:yyyy}/{now:MM}/{fileAssetId:N}/{safe}";
    }

    /// <summary>
    /// The key <paramref name="storageKey"/> should live at for <paramref name="visibility"/>: its
    /// visibility root is swapped, or added when the key predates visibility roots. Returns the key
    /// unchanged when it is already under the right root.
    /// </summary>
    public static string ForVisibility(string storageKey, Visibility visibility)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var root = RootFor(visibility);
        return root + StripVisibilityRoot(storageKey);
    }

    /// <summary>True when <paramref name="storageKey"/> already sits under the root for <paramref name="visibility"/>.</summary>
    public static bool IsUnderVisibilityRoot(string storageKey, Visibility visibility)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        return storageKey.StartsWith(RootFor(visibility), StringComparison.Ordinal);
    }

    public static string Sanitize(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return UnsafeChars().Replace(fileName, "_");
    }

    private static string RootFor(Visibility visibility) => visibility switch
    {
        Visibility.Public => StorageVisibilityRoot.Public,
        Visibility.Private => StorageVisibilityRoot.Private,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, "Unknown visibility."),
    };

    private static string StripVisibilityRoot(string storageKey)
    {
        if (storageKey.StartsWith(StorageVisibilityRoot.Public, StringComparison.Ordinal))
        {
            return storageKey[StorageVisibilityRoot.Public.Length..];
        }

        if (storageKey.StartsWith(StorageVisibilityRoot.Private, StringComparison.Ordinal))
        {
            return storageKey[StorageVisibilityRoot.Private.Length..];
        }

        return storageKey;
    }
}
