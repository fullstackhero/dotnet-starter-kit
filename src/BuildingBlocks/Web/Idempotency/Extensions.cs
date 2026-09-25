using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Web.Idempotency;

public static class Extensions
{
    /// <summary>
    /// Registers idempotency options for use by IdempotencyEndpointFilter.
    /// Apply to specific endpoints via .WithIdempotency() extension.
    /// </summary>
    public static IServiceCollection AddHeroIdempotency(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Validated at startup, like every other options block here. A misconfigured TTL fails
        // SILENTLY otherwise: a zero or negative DefaultTtl throws inside the best-effort cache
        // write, which logs a warning and moves on, so no response is ever stored and replay never
        // engages — the exact silent failure this filter exists to have stopped having. A
        // ReservationTtl of zero expires the in-flight lock the moment it is taken, so concurrent
        // duplicates both run the handler.
        services.AddOptions<IdempotencyOptions>()
            .BindConfiguration(nameof(IdempotencyOptions))
            .Validate(o => !string.IsNullOrWhiteSpace(o.HeaderName), "IdempotencyOptions.HeaderName is required.")
            .Validate(o => o.DefaultTtl > TimeSpan.Zero, "IdempotencyOptions.DefaultTtl must be greater than zero.")
            .Validate(o => o.ReservationTtl > TimeSpan.Zero, "IdempotencyOptions.ReservationTtl must be greater than zero.")
            .Validate(o => o.ReservationTtl <= o.DefaultTtl, "IdempotencyOptions.ReservationTtl must not exceed DefaultTtl — the reservation only has to outlast the handler.")
            .Validate(o => o.MaxKeyLength > 0, "IdempotencyOptions.MaxKeyLength must be greater than zero.")
            .ValidateOnStart();

        return services;
    }
}
