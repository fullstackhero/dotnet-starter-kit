namespace FSH.WebApi.Application.Dashboard;

// TODO: handle registerd users and registered roles create/delete
public class SendStatsChangedNotificationHandler :
    INotificationHandler<EventNotification<BrandCreatedEvent>>,
    INotificationHandler<EventNotification<BrandDeletedEvent>>,
    INotificationHandler<EventNotification<ProductCreatedEvent>>,
    INotificationHandler<EventNotification<ProductDeletedEvent>>
{
    private readonly ILogger<SendStatsChangedNotificationHandler> _logger;
    private readonly INotificationService _notificationService;

    public SendStatsChangedNotificationHandler(ILogger<SendStatsChangedNotificationHandler> logger, INotificationService notificationService) =>
        (_logger, _notificationService) = (logger, notificationService);

    public Task Handle(EventNotification<BrandCreatedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.DomainEvent, cancellationToken);

    public Task Handle(EventNotification<BrandDeletedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.DomainEvent, cancellationToken);

    public Task Handle(EventNotification<ProductCreatedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.DomainEvent, cancellationToken);

    public Task Handle(EventNotification<ProductDeletedEvent> notification, CancellationToken cancellationToken) =>
        SendStatsChangedNotification(notification.DomainEvent, cancellationToken);

    private Task SendStatsChangedNotification(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{event} Triggered", domainEvent.GetType().Name);

        return _notificationService.SendMessageAsync(new StatsChangedNotification(), cancellationToken);
    }
}