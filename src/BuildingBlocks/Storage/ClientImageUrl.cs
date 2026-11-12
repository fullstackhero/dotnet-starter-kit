namespace FSH.Framework.Storage;

/// <summary>
/// The shape of an image URL a client may store directly (an avatar or logo it already hosts, or a
/// public URL the Files module handed it).
/// </summary>
/// <remarks>
/// Such a value is rendered by browsers, and code near it also reads and deletes stored files, so only
/// two forms are accepted: an absolute <c>http(s)</c> URL, or a server-relative path (<c>/…</c>) with no
/// <c>..</c> segment, backslash or colon. That excludes file paths (<c>C:\…</c>, <c>\\server\share</c>,
/// <c>../web.config</c>), <c>file:</c>, <c>javascript:</c> and <c>data:</c> URIs, and protocol-relative
/// <c>//host</c> URLs. Whether a value may be <em>deleted</em> is a separate question — see
/// <see cref="StorageUploadKeys.IsUploadOf{T}"/>.
/// </remarks>
public static class ClientImageUrl
{
    /// <summary>True when <paramref name="value"/> is an acceptable client-supplied image URL.</summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048)
        {
            return false;
        }

        if (value.StartsWith('/'))
        {
            return !value.StartsWith("//", StringComparison.Ordinal)
                && !value.Contains('\\', StringComparison.Ordinal)
                && !value.Contains(':', StringComparison.Ordinal)
                && !value.Split('/', '?', '#').Any(segment => segment is "." or "..");
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrEmpty(uri.Host);
    }
}
