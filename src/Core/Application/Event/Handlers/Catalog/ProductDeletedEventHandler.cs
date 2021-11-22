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
    public class ProductDeletedEventHandler : INotificationHandler<EventNotification<ProductDeletedEvent>>
    {
        private readonly ILogger<ProductDeletedEventHandler> _logger;

        public ProductDeletedEventHandler(ILogger<ProductDeletedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(EventNotification<ProductDeletedEvent> notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{event} Triggered", notification.DomainEvent.GetType().Name);
            return Task.CompletedTask;
        }
    }
}