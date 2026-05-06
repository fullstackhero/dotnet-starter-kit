namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Excludes an endpoint from HTTP audit capture. Apply to endpoints that
/// must not have request/response bodies recorded for compliance or
/// privacy reasons (password reset, payment forms, MFA enrollment).
///
/// Two modes:
/// <list type="bullet">
///   <item><description><see cref="BodyOnly"/> = false (default): skip the
///   audit entirely — no activity record is written.</description></item>
///   <item><description><see cref="BodyOnly"/> = true: still record the
///   activity (timing, status, source, tenant, user) but omit the request
///   and response body previews.</description></item>
/// </list>
///
/// Apply via metadata:
/// <code>
/// endpoints.MapPost("/reset-password", ...)
///     .WithMetadata(new NoAuditAttribute())
///     .RequirePermission(...);
/// </code>
/// or via the convenience extension:
/// <code>endpoints.MapPost(...).NoAudit();</code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class NoAuditAttribute : Attribute
{
    /// <summary>
    /// When true, the endpoint is still audited but request/response
    /// previews are omitted. When false (default), the audit is skipped
    /// entirely.
    /// </summary>
    public bool BodyOnly { get; init; }
}
