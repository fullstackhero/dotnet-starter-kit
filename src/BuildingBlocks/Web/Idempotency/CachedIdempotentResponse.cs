using System.ComponentModel;

namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// A cached HTTP response for idempotent replay.
/// </summary>
/// <remarks>
/// Marked <see cref="ImmutableObjectAttribute"/> + <c>sealed</c> so HybridCache can reuse the
/// in-process instance across requests without re-deserializing on every L1 hit.
/// </remarks>
[ImmutableObject(true)]
public sealed record CachedIdempotentResponse
{
    public int StatusCode { get; init; }
    public string? ContentType { get; init; }
    public byte[] Body { get; init; } = [];
}
