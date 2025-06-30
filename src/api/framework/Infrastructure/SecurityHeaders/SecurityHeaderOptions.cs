namespace FSH.Framework.Infrastructure.SecurityHeaders;

public class SecurityHeaderOptions
{
    public bool Enable { get; set; }
    public AppSecurityHeaders Headers { get; set; } = default!;
}
