using FSH.Modules.Tickets.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Tickets.Events;

public sealed class TicketEventHandlers(ILogger<TicketEventHandlers> logger) :
    INotificationHandler<TicketAssignedDomainEvent>,
    INotificationHandler<TicketCommentAddedDomainEvent>,
    INotificationHandler<TicketCreatedDomainEvent>,
    INotificationHandler<TicketStatusChangedDomainEvent>
{
    public ValueTask Handle(TicketAssignedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling TicketAssignedDomainEvent for TicketId: {TicketId}", notification.TicketId);
        }
        return default;
    }

    public ValueTask Handle(TicketCommentAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling TicketCommentAddedDomainEvent for TicketId: {TicketId}", notification.TicketId);
        }
        return default;
    }

    public ValueTask Handle(TicketCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling TicketCreatedDomainEvent for TicketId: {TicketId}", notification.TicketId);
        }
        return default;
    }

    public ValueTask Handle(TicketStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling TicketStatusChangedDomainEvent for TicketId: {TicketId}", notification.TicketId);
        }
        return default;
    }
}
