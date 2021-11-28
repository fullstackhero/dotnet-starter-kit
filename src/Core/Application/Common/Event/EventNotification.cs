using DN.WebApi.Domain.Common.Contracts;
using MediatR;

namespace DN.WebApi.Application.Common.Event;

public class EventNotification<T> : INotification
where T : DomainEvent
{
    public EventNotification(T domainEvent)
    {
        DomainEvent = domainEvent;
    }

    public T DomainEvent { get; }
}