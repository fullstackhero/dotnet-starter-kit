namespace DN.WebApi.Infrastructure.Middleware;

public class MiddlewareSettings
{
    public bool EnableHttpsLogging { get; set; } = false;
    public bool EnableLocalization { get; set; } = false;
    public ushort BlackListMinutes { get; set; }
    public ushort MaxAuthFailed { get; set; }
}