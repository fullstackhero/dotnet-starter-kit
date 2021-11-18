using DN.WebApi.Domain.Contracts;
using MediatR;

namespace DN.WebApi.Application.Event
{
    public class EventNotification<T> : INotification
    where T : DomainEvent
    {
        public EventNotification(T domainEvent)
        {
            DomainEvent = domainEvent;
        }

        public T DomainEvent { get; }
    }
}