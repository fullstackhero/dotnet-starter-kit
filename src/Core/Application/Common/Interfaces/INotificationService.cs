namespace FSH.WebApi.Application.Common.Interfaces;

public interface INotificationService : ITransientService
{
    Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken);

    Task BroadcastMessageAsync(INotificationMessage notification, CancellationToken cancellationToken);

    Task SendMessageAsync(INotificationMessage notification, CancellationToken cancellationToken);

    Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken);

    Task SendMessageToGroupAsync(INotificationMessage notification, string group, CancellationToken cancellationToken);

    Task SendMessageToGroupExceptAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds, CancellationToken cancellationToken);

    Task SendMessageToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames, CancellationToken cancellationToken);

    Task SendMessageToUserAsync(string userId, INotificationMessage notification, CancellationToken cancellationToken);

    Task SendMessageToUsersAsync(IEnumerable<string> userIds, INotificationMessage notification, CancellationToken cancellationToken);
}