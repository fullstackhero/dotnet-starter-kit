using FSH.Starter.Blazor.Shared.Notifications;

namespace FSH.Starter.Blazor.Infrastructure.Notifications;

public record ConnectionStateChanged(ConnectionState State, string? Message) : INotificationMessage;