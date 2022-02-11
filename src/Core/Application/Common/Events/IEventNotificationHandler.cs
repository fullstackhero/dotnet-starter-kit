using FSH.WebApi.Shared.Events;

namespace FSH.WebApi.Application.Common.Events;

public interface IEventNotificationHandler<TEvent> : INotificationHandler<EventNotification<TEvent>>
    where TEvent : IEvent
{
}