using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace FSH.Framework.Web.HttpResilience;

public static class Extensions
{
    /// <summary>
    /// Adds a standard resilience handler (retry, circuit breaker, timeout) to the HTTP client builder.
    /// Configuration is read from the "HttpResilienceOptions" section.
    /// </summary>
    public static IHttpClientBuilder AddHeroResilience(this IHttpClientBuilder builder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(nameof(HttpResilienceOptions)).Get<HttpResilienceOptions>() ?? new HttpResilienceOptions();

        if (!options.Enabled)
        {
            return builder;
        }

        builder.AddStandardResilienceHandler(pipeline =>
        {
            pipeline.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
            pipeline.Retry.Delay = options.MedianFirstRetryDelay;

            pipeline.TotalRequestTimeout.Timeout = options.TotalTimeout;
            pipeline.AttemptTimeout.Timeout = options.AttemptTimeout;

            pipeline.CircuitBreaker.BreakDuration = options.CircuitBreakerBreakDuration;
            pipeline.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
            pipeline.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
        });

        return builder;
    }
}
