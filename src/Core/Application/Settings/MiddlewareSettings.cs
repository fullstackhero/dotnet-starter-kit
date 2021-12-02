namespace DN.WebApi.Application.Settings;

public class MiddlewareSettings
{
    public bool EnableHttpsLogging { get; set; }
    public bool EnableLocalization { get; set; }
    public ushort MaxAuthFailed { get; set; }
    public ushort BlackListMinutes { get; set; }
}