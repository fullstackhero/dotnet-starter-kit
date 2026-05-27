using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Handles the UserRegisteredEvent domain event by publishing an integration event
/// so other modules can react to new user registrations.
/// </summary>
public sealed class UserRegisteredHandler(
    IEventBus eventBus,
    ILogger<UserRegisteredHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    public async ValueTask Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "User registered: {UserId} ({Email})",
                notification.UserId,
                notification.Email);
        }

        var integrationEvent = new UserRegisteredIntegrationEvent(
            Id: notification.EventId,
            OccurredOnUtc: notification.OccurredOnUtc.UtcDateTime,
            TenantId: notification.TenantId,
            CorrelationId: notification.CorrelationId ?? notification.EventId.ToString(),
            Source: nameof(UserRegisteredHandler),
            UserId: notification.UserId,
            Email: notification.Email,
            FirstName: notification.FirstName ?? string.Empty,
            LastName: notification.LastName ?? string.Empty);

        await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }
}
