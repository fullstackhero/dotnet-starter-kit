namespace FSH.Framework.Web.HttpResilience;

/// <summary>
/// Configuration options for HTTP client resilience pipelines (retry, circuit breaker, timeout).
/// </summary>
public sealed class HttpResilienceOptions
{
    /// <summary>
    /// Whether resilience handlers are enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts. Default: 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Median delay for the first retry (exponential backoff). Default: 1 second.
    /// </summary>
    public TimeSpan MedianFirstRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Total timeout for the entire request including all retries. Default: 30 seconds.
    /// </summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for each individual attempt. Default: 10 seconds.
    /// </summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Duration the circuit breaker stays open after tripping. Default: 5 seconds.
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Failure ratio that trips the circuit breaker. Default: 0.5 (50%).
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Minimum throughput before circuit breaker evaluates. Default: 10 requests.
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
}
