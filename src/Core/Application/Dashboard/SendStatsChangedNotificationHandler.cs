using FSH.WebApi.Domain.Common.Events;
using FSH.WebApi.Domain.Identity;

namespace FSH.WebApi.Application.Dashboard;

public class SendStatsChangedNotificationHandler :
    INotificationHandler<EventNotification<EntityCreatedEvent<Brand>>>,
    INotificationHandler<EventNotification<EntityDeletedEvent<Brand>>>,
    INotificationHandler<EventNotification<EntityCreatedEvent<Product>>>,
    INotificationHandler<EventNotification<EntityDeletedEvent<Product>>>,
    INotificationHandler<EventNotification<ApplicationRoleCreatedEvent>>,
    INotificationHandler<EventNotification<ApplicationRoleDeletedEvent>>,
    INotificationHandler<EventNotification<ApplicationUserCreatedEvent>>
{
    private readonly ILogger<SendStatsChangedNotificationHandler> _logger;
    private readonly INotificationService _notificationService;

    public SendStatsChangedNotificationHandler(ILogger<SendStatsChangedNotificationHandler> logger, INotificationService notificationService) =>
        (_logger, _notificationService) = (logger, notificationService);

    public Task Handle(EventNotification<EntityCreatedEvent<Brand>> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);
    public Task Handle(EventNotification<EntityDeletedEvent<Brand>> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);
    public Task Handle(EventNotification<EntityCreatedEvent<Product>> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);
    public Task Handle(EventNotification<EntityDeletedEvent<Product>> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);
    public Task Handle(EventNotification<ApplicationRoleCreatedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);
    public Task Handle(EventNotification<ApplicationRoleDeletedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);
    public Task Handle(EventNotification<ApplicationUserCreatedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.Event, cancellationToken);

    private Task SendStatsChangedNotification(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", domainEvent.GetType().Name);

        return _notificationService.SendMessageAsync(new StatsChangedNotification(), cancellationToken);
    }
}