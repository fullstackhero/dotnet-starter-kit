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
    /// Maximum allowed length for the idempotency key. Default: 128 characters.
    /// </summary>
    public int MaxKeyLength { get; set; } = 128;
}
