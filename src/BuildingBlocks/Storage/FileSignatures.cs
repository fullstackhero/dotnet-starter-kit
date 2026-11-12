namespace FSH.Framework.Storage;

/// <summary>
/// Checks that an upload's bytes start with the signature its extension promises.
/// </summary>
/// <remarks>
/// The extension allow-list decides how a stored file is served (its Content-Type), not what it
/// contains. Without this, <c>avatar.png</c> can carry HTML or script that a client, a proxy or a
/// misconfigured bucket ends up rendering. Only the extensions the framework accepts are listed; an
/// unknown extension does not match.
/// </remarks>
public static class FileSignatures
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Ico = [0x00, 0x00, 0x01, 0x00];
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D]; // "%PDF-"

    /// <summary>True when <paramref name="data"/> starts with the signature of <paramref name="extension"/>.</summary>
    public static bool Matches(string? extension, IReadOnlyList<byte>? data)
    {
        if (string.IsNullOrWhiteSpace(extension) || data is null)
        {
            return false;
        }

        byte[]? signature = extension.ToUpperInvariant() switch
        {
            ".PNG" => Png,
            ".JPG" or ".JPEG" => Jpeg,
            ".ICO" => Ico,
            ".PDF" => Pdf,
            _ => null,
        };

        if (signature is null || data.Count < signature.Length)
        {
            return false;
        }

        for (int i = 0; i < signature.Length; i++)
        {
            if (data[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}
