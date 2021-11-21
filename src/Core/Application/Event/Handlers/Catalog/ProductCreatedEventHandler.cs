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
        private readonly INotificationService _notificationService;
        private readonly ITenantService _tenantService;

        public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger, INotificationService notificationService, ITenantService tenantService)
        {
            _logger = logger;
            _notificationService = notificationService;
            _tenantService = tenantService;
        }

        public async Task Handle(EventNotification<ProductCreatedEvent> notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendMessageToGroupAsync(new StatsChangedNotification(), _tenantService.GetCurrentTenant().Key);
            _logger.LogInformation("Handling Event: {event}", notification.DomainEvent.GetType().Name);
        }
    }
}