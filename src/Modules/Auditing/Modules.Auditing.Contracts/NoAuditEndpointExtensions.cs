using Microsoft.AspNetCore.Builder;

namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Convenience builders for tagging endpoints with <see cref="NoAuditAttribute"/>.
/// </summary>
public static class NoAuditEndpointExtensions
{
    /// <summary>
    /// Suppresses the entire HTTP audit for this endpoint. Use for routes
    /// where the existence of the call itself is sensitive.
    /// </summary>
    public static TBuilder NoAudit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithMetadata(new NoAuditAttribute { BodyOnly = false });
    }

    /// <summary>
    /// Records the activity but omits request/response body previews. Use
    /// when you need timing/status visibility but the bodies contain PII.
    /// </summary>
    public static TBuilder NoAuditBody<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithMetadata(new NoAuditAttribute { BodyOnly = true });
    }
}
