namespace FL_CRMS_ERP_WEBAPI.Infrastructure.SecurityHeaders;

public class SecurityHeaderSettings
{
    public bool Enable { get; set; }
    public SecurityHeaders Headers { get; set; } = default!;
}