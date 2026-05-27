using Microsoft.Net.Http.Headers;

namespace FSH.Modules.Auditing;

internal static class ContentTypeHelper
{
    public static bool IsJsonLike(string? contentType, ISet<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;

        // Prefer robust parse; fallback to naive split if needed.
        if (MediaTypeHeaderValue.TryParse(contentType, out var mt))
            return allowed.Contains(mt.MediaType.Value ?? string.Empty);

        var semi = contentType.IndexOf(';', StringComparison.Ordinal);
        var type = semi >= 0 ? contentType[..semi] : contentType;
        return allowed.Contains(type.Trim());
    }
}