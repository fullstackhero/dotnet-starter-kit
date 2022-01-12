namespace FSH.WebApi.Infrastructure.SecurityHeaders;

public class SecurityHeaderSettings
{
    public bool Enable { get; set; }

    /// <summary>
    /// X-Frame-Options.
    /// </summary>
    public string? XFrameOptions { get; set; }

    /// <summary>
    /// X-Content-Type-Options.
    /// </summary>
    public string? XContentTypeOptions { get; set; }

    /// <summary>
    /// Referrer-Policy.
    /// </summary>
    public string? ReferrerPolicy { get; set; }

    /// <summary>
    /// Permissions-Policy.
    /// </summary>
    public string? PermissionsPolicy { get; set; }

    public string? SameSite { get; set; }

    /// <summary>
    /// X-XSS-Protection.
    /// </summary>
    public string? XXSSProtection { get; set; }

    ///// <summary>
    ///// TODO
    ///// </summary>
    ////public List<string> ContentPolicy { get; set; }.
}