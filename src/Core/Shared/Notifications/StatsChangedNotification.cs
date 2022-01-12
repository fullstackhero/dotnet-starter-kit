namespace FSH.WebApi.Shared.Notifications;

public class StatsChangedNotification : INotificationMessage
{
    public string MessageType { get; set; } = typeof(StatsChangedNotification).Name;
    public string? Message { get; set; }
}