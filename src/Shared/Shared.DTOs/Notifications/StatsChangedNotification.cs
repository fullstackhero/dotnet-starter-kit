namespace DN.WebApi.Shared.DTOs.Notifications
{
    public class StatsChangedNotification : INotificationMessage
    {
        public string MessageType { get; set; } = typeof(JobNotification).Name;
        public string Message { get; set; }
    }
}