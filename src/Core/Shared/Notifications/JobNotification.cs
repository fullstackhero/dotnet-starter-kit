namespace FSH.WebApi.Shared.Notifications;

public class JobNotification : INotificationMessage
{
    public string MessageType { get; set; } = typeof(JobNotification).Name;
    public string? Message { get; set; }
    public string? JobId { get; set; }
    public decimal Progress { get; set; }
}