using FSH.Starter.Blazor.Shared.Notifications;

namespace FSH.Starter.Blazor.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(INotificationMessage notification);
}