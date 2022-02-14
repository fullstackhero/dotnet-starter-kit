using FSH.WebApi.Shared.Events;

namespace FSH.WebApi.Application.Common.Events;

// This is just a shorthand to make it a bit easier to create event handlers for specific events.
public interface IEventNotificationHandler<TEvent> : INotificationHandler<EventNotification<TEvent>>
    where TEvent : IEvent
{
}