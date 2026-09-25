namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// A cached HTTP response for idempotent replay.
/// </summary>
public sealed record CachedIdempotentResponse
{
    public int StatusCode { get; init; }

    public string? ContentType { get; init; }

    public byte[] Body { get; init; } = [];

    /// <summary>
    /// Response headers replayed alongside the body. Only headers that carry meaning for the caller
    /// are captured (see the filter's allow-list) — the host sets the transport ones itself, and
    /// replaying a stale <c>Content-Length</c> or <c>Transfer-Encoding</c> would corrupt the response.
    /// Defaults to empty so entries written before headers were captured still deserialize.
    /// Iterate it, do not look a header up by name: deserialization replaces this instance with a
    /// plain case-SENSITIVE dictionary, so the initializer's comparer only holds on the write path.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
