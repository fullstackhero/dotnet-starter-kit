using FL_CRMS_ERP_WEBAPI.Shared.Events;

namespace FL_CRMS_ERP_WEBAPI.Application.Common.Events;

// This is just a shorthand to make it a bit easier to create event handlers for specific events.
public interface IEventNotificationHandler<TEvent> : INotificationHandler<EventNotification<TEvent>>
    where TEvent : IEvent
{
}

public abstract class EventNotificationHandler<TEvent> : INotificationHandler<EventNotification<TEvent>>
    where TEvent : IEvent
{
    public Task Handle(EventNotification<TEvent> notification, CancellationToken cancellationToken) =>
        Handle(notification.Event, cancellationToken);

    public abstract Task Handle(TEvent @event, CancellationToken cancellationToken);
}