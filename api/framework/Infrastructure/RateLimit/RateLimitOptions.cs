namespace FSH.Framework.Infrastructure.RateLimit;

public class RateLimitOptions
{
    public bool EnableRateLimiting { get; init; }
    public int PermitLimit { get; init; }
    public int WindowInSeconds { get; init; }
    public int RejectionStatusCode { get; init; }
}
