namespace FSH.Framework.Infrastructure.SecurityHeaders;

public class SecurityHeaderOptions
{
    public bool Enable { get; set; }
    public SecurityHeaders Headers { get; set; } = default!;
}
