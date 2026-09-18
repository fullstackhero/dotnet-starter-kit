namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// Marks an endpoint as idempotent. Applied by <c>WithIdempotency()</c> so the wiring can be asserted
/// from the endpoint map: an endpoint filter itself is invisible in metadata.
/// </summary>
public sealed class IdempotentEndpointMetadata
{
    /// <summary>The endpoint takes the configured <c>IdempotencyOptions.DefaultTtl</c>.</summary>
    public static IdempotentEndpointMetadata Instance { get; } = new(ttl: null);

    public IdempotentEndpointMetadata(TimeSpan? ttl)
    {
        Ttl = ttl;
    }

    /// <summary>
    /// How long a replay of this endpoint stays valid, or <c>null</c> to take the configured default.
    /// An endpoint whose response goes stale on its own (a presigned URL, a short-lived token) must
    /// not out-live it here: replaying a dead payload with a 200 is worse than running again.
    /// </summary>
    public TimeSpan? Ttl { get; }
}
