namespace FSH.Modules.Auditing.Contracts;

public sealed class AuditHttpOptions
{
    public bool CaptureBodies { get; set; } = true;
    public int MaxRequestBytes { get; set; } = 8_192;
    public int MaxResponseBytes { get; set; } = 16_384;

    public HashSet<string> AllowedContentTypes { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "application/json",
            "application/problem+json"
        };

    public HashSet<string> ExcludePathStartsWith { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "/health", "/metrics", "/_framework", "/swagger", "/scalar", "/openapi"
        };

    public AuditSeverity MinExceptionSeverity { get; set; } = AuditSeverity.Error;
}