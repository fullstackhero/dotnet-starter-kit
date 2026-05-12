using Mediator;

namespace FSH.Modules.Notifications.Contracts.v1.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand<Unit>;
