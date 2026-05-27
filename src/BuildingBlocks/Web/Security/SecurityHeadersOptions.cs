namespace FSH.Framework.Web.Security;

public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Enables or disables the security headers middleware entirely.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Paths to bypass (e.g., OpenAPI/Scalar assets).
    /// </summary>
    public string[] ExcludedPaths { get; set; } = ["/scalar", "/openapi"];

    /// <summary>
    /// Whether to allow inline styles in CSP (default true for Scalar compatibility).
    /// </summary>
    public bool AllowInlineStyles { get; set; } = true;

    /// <summary>
    /// Additional script sources to append to CSP.
    /// </summary>
    public string[] ScriptSources { get; set; } = [];

    /// <summary>
    /// Additional style sources to append to CSP.
    /// </summary>
    public string[] StyleSources { get; set; } = [];
}