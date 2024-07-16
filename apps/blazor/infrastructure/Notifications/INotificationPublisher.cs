using FSH.Blazor.Shared.Notifications;

namespace FSH.Blazor.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(INotificationMessage notification);
}