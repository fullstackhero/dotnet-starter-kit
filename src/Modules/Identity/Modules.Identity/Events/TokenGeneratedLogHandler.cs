using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Identity.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Example handler that logs when a token is generated.
/// This is primarily intended to make it easier to test the integration event pipeline.
/// </summary>
public sealed class TokenGeneratedLogHandler(ILogger<TokenGeneratedLogHandler> logger)
        : IIntegrationEventHandler<TokenGeneratedIntegrationEvent>
{
    public Task HandleAsync(TokenGeneratedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Token generated for user {UserId} ({Email}) with client {ClientId}, IP {IpAddress}, UserAgent {UserAgent}, expires at {ExpiresAtUtc} (fingerprint: {Fingerprint})",
                @event.UserId,
                @event.Email,
                @event.ClientId,
                @event.IpAddress,
                @event.UserAgent,
                @event.AccessTokenExpiresAtUtc,
                @event.TokenFingerprint);
        }

        return Task.CompletedTask;
    }
}