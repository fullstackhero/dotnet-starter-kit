namespace FSH.Framework.Storage;

/// <summary>
/// Root prefixes that encode an object's visibility in its storage key. Every stack's bucket policy
/// grants anonymous <c>s3:GetObject</c> on <see cref="Public"/> only, so an object is publicly readable
/// exactly when its key starts with it; anything under <see cref="Private"/> (or under no root, for keys
/// written before visibility lived in the key) needs a presigned URL.
/// </summary>
public static class StorageVisibilityRoot
{
    /// <summary>Anonymously readable. Bucket policies grant <c>s3:GetObject</c> on <c>public/*</c>.</summary>
    public const string Public = "public/";

    /// <summary>Readable only through presigned URLs.</summary>
    public const string Private = "private/";
}
