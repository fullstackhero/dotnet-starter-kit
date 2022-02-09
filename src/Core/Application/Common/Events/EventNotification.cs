namespace FSH.WebApi.Application.Common.Events;

public class EventNotification<TDomainEvent> : INotification
    where TDomainEvent : DomainEvent
{
    public EventNotification(TDomainEvent domainEvent) => Event = domainEvent;

    public TDomainEvent Event { get; }
}