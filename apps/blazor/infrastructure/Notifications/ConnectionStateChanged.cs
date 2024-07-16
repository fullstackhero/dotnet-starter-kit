
using FSH.Blazor.Shared.Notifications;

namespace FSH.Blazor.Infrastructure.Notifications;

public record ConnectionStateChanged(ConnectionState State, string? Message) : INotificationMessage;