using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Domain.Entities.Catalog.Events;
using DN.WebApi.Shared.DTOs.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Application.Event.Handlers.Catalog
{
    public class ProductCreatedEventHandler : INotificationHandler<EventNotification<ProductCreatedEvent>>
    {
        private readonly ILogger<ProductCreatedEventHandler> _logger;

        public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(EventNotification<ProductCreatedEvent> notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
            return Task.CompletedTask;
        }
    }
}