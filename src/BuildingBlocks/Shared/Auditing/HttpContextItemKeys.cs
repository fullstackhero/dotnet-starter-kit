namespace FSH.Framework.Shared.Auditing;

/// <summary>
/// Well-known <c>HttpContext.Items</c> keys for cross-cutting middleware signals that the audit
/// pipeline enriches activity events with. Building-block middleware writes these flags; the audit
/// HTTP middleware reads them — keeping building blocks free of a dependency on the audit module.
/// </summary>
public static class HttpContextItemKeys
{
    /// <summary>Set to <c>true</c> when a request was rejected by quota enforcement (HTTP 429).</summary>
    public const string QuotaRejected = "fsh.audit.quota-rejected";
}
