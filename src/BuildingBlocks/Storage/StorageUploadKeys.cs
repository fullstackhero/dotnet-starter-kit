using System.Text.RegularExpressions;

namespace FSH.Framework.Storage;

/// <summary>
/// The key layout <see cref="Services.IStorageService.UploadAsync{T}"/> writes —
/// <c>uploads/{type}/{guid}_{sanitized-name}</c>, under the provider's prefix — and the test for whether
/// a stored value is one of those objects.
/// </summary>
/// <remarks>
/// A module that keeps the value <c>UploadAsync</c> returned (an avatar, a tenant logo) and later
/// deletes "the current file" must only delete objects it created itself. The same field can also hold
/// a URL the client supplied, and handing that to <c>RemoveAsync</c> lets the client choose what gets
/// deleted: another tenant's object, or with local storage any file the process can write.
/// <see cref="IsUploadOf{T}"/> is that ownership test.
/// </remarks>
public static partial class StorageUploadKeys
{
    /// <summary>The root every <c>UploadAsync</c> object lives under.</summary>
    public const string UploadBasePath = "uploads";

    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex FolderSanitizer();

    [GeneratedRegex(@"[^a-zA-Z0-9_\.-]")]
    private static partial Regex FileNameSanitizer();

    // The file-name segment UploadAsync produces: a 32-hex-digit GUID, an underscore, the sanitized name.
    [GeneratedRegex(@"^[0-9a-f]{32}_[a-zA-Z0-9_\.-]+$")]
    private static partial Regex UploadedFileName();

    /// <summary>The folder segment for uploads of type <typeparamref name="T"/>.</summary>
    public static string FolderFor<T>() where T : class
    {
#pragma warning disable CA1308 // folder names are intentionally lower-case for URLs/paths
        return FolderSanitizer().Replace(typeof(T).Name.ToLowerInvariant(), "_");
#pragma warning restore CA1308
    }

    /// <summary>Replaces every character outside <c>[a-zA-Z0-9_.-]</c>, so a name cannot carry a path.</summary>
    public static string SanitizeFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return FileNameSanitizer().Replace(fileName, "_");
    }

    /// <summary>The key (without provider prefix) for a new upload of type <typeparamref name="T"/>.</summary>
    public static string NewKey<T>(string fileName) where T : class =>
        $"{UploadBasePath}/{FolderFor<T>()}/{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";

    /// <summary>
    /// True when <paramref name="storedValue"/> — a value <c>UploadAsync&lt;T&gt;</c> returned, as a
    /// relative path, a provider key or an http(s) URL — names an object created by
    /// <c>UploadAsync&lt;T&gt;</c>. Anything else (a client-supplied URL, a path with traversal, a file
    /// URI, another type's upload) is false and must not be deleted on the caller's behalf.
    /// </summary>
    public static bool IsUploadOf<T>(string? storedValue) where T : class
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return false;
        }

        string path = storedValue;
        if (!storedValue.StartsWith('/') && Uri.TryCreate(storedValue, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            path = Uri.UnescapeDataString(uri.AbsolutePath);
        }

        if (path.Contains('\\', StringComparison.Ordinal) || path.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        string[] segments = path.Trim('/').Split('/');
        if (segments.Length < 3 || segments.Any(segment => segment is "" or "." or ".."))
        {
            return false;
        }

        int last = segments.Length - 1;
        return string.Equals(segments[last - 2], UploadBasePath, StringComparison.Ordinal)
            && string.Equals(segments[last - 1], FolderFor<T>(), StringComparison.Ordinal)
            && UploadedFileName().IsMatch(segments[last]);
    }
}
