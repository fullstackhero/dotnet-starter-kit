namespace FSH.Framework.Infrastructure.SecurityHeaders;

public class AppSecurityHeaders
{
    public string? XContentTypeOptions { get; set; }
    public string? ReferrerPolicy { get; set; }
    public string? XXSSProtection { get; set; }
    public string? XFrameOptions { get; set; }
    public string? ContentSecurityPolicy { get; set; }
    public string? PermissionsPolicy { get; set; }
    public string? StrictTransportSecurity { get; set; }
}
