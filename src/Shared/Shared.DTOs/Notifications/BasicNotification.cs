namespace DN.WebApi.Shared.DTOs.Notifications
{
    public class BasicNotification : INotificationMessage
    {
        public string TypeMessage { get; set; } = typeof(BasicNotification).Name;
        public string Message { get; set; }
    }
}
