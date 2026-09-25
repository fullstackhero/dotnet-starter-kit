namespace FSH.Framework.Web.Idempotency;

/// <summary>
/// Configuration options for HTTP request idempotency.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>
    /// The header name to read the idempotency key from. Default: "Idempotency-Key".
    /// </summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>
    /// Default time-to-live for cached idempotent responses. Default: 24 hours.
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Time-to-live for the in-flight reservation that serializes concurrent duplicate keys.
    /// Decoupled from <see cref="DefaultTtl"/>: it must only outlast the handler's execution, so a
    /// crash between reserving and releasing frees the key in seconds instead of stranding it for the
    /// full response TTL (every retry would 409 until it expired). Must exceed the longest expected
    /// handler runtime — if it lapses mid-request a concurrent duplicate can slip through. Default: 1 minute.
    /// </summary>
    public TimeSpan ReservationTtl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum allowed length for the idempotency key. Default: 128 characters.
    /// </summary>
    public int MaxKeyLength { get; set; } = 128;
}
