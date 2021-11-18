using System;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Event;
using DN.WebApi.Domain.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class EventService : IEventService
    {
        private readonly ILogger<EventService> _logger;
        private readonly IPublisher _mediator;

        public EventService(ILogger<EventService> logger, IPublisher mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task PublishAsync(DomainEvent @event)
        {
            _logger.LogInformation("Publishing Event : {event}", @event.GetType().Name);
            await _mediator.Publish(GetEventNotification(@event));
        }

        private INotification GetEventNotification(DomainEvent @event)
        {
            return (INotification)Activator.CreateInstance(
                typeof(EventNotification<>).MakeGenericType(@event.GetType()), @event)!;
        }
    }
}