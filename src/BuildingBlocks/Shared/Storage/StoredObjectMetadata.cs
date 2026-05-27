namespace FSH.Framework.Shared.Storage;

/// <summary>
/// Metadata returned by a HEAD against an object in storage. Used by the Files module on finalize
/// to verify size + content-type vs values declared at upload-url time.
/// </summary>
public sealed record StoredObjectMetadata(
    long SizeBytes,
    string ContentType,
    DateTimeOffset LastModified,
    string? ETag);
