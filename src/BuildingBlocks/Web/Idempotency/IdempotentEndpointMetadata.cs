namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// Marks an endpoint as idempotent. Applied by <c>WithIdempotency()</c> so the wiring can be asserted
/// from the endpoint map: an endpoint filter itself is invisible in metadata.
/// </summary>
public sealed class IdempotentEndpointMetadata
{
    public static IdempotentEndpointMetadata Instance { get; } = new();

    private IdempotentEndpointMetadata()
    {
    }
}
