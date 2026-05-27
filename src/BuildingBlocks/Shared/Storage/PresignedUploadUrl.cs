namespace FSH.Framework.Shared.Storage;

/// <summary>
/// A short-lived presigned URL the browser can use to PUT bytes directly to S3-compatible storage,
/// along with any headers the signature requires the browser to include verbatim.
/// </summary>
public sealed record PresignedUploadUrl(
    Uri Url,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);
