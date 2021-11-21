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
        private readonly INotificationService _notificationService;

        public ProductDeletedEventHandler(ILogger<ProductDeletedEventHandler> logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task Handle(EventNotification<ProductDeletedEvent> notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendMessageAsync(new StatsChangedNotification());
            _logger.LogInformation("Handling Event: {event}", notification.DomainEvent.GetType().Name);
        }
    }
}