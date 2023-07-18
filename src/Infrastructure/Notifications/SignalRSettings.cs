namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Notifications;

public class SignalRSettings
{
    public class Backplane
    {
        public string? Provider { get; set; }
        public string? StringConnection { get; set; }
    }

    public bool UseBackplane { get; set; }
}