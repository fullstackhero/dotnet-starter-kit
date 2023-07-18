using FL_CRMS_ERP_WEBAPI.Shared.Events;

namespace FL_CRMS_ERP_WEBAPI.Application.Common.Events;

public class EventNotification<TEvent> : INotification
    where TEvent : IEvent
{
    public EventNotification(TEvent @event) => Event = @event;

    public TEvent Event { get; }
}