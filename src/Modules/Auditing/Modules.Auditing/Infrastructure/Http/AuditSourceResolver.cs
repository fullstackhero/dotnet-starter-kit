using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing;

/// <summary>
/// Builds a stable, dashboard-friendly source key for HTTP audits.
///
/// Format: <c>api.{module}.{routeName}</c> — e.g. <c>api.identity.RegisterUser</c>.
/// The module slug comes from the URL segment after the version prefix
/// (<c>/api/v{n}/{module}/...</c>) and the route name is the explicit
/// <c>.WithName(...)</c> set on the endpoint. Either component falls back
/// gracefully — a missing route name yields <c>api.{module}</c>; a path
/// outside the versioned <c>/api/v{n}/...</c> shape yields just <c>api</c>.
///
/// Stable keys make Source-faceted filters in the audits dashboard usable
/// (you can pin "show me identity.RegisterUser failures" without the
/// long-form endpoint display name drifting on every refactor).
/// </summary>
internal static class AuditSourceResolver
{
    public static string Resolve(HttpContext ctx)
    {
        var endpoint = ctx.GetEndpoint();
        var routeName = endpoint?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
        var module = ExtractModuleSlug(ctx.Request.Path.Value);

        return (module, routeName) switch
        {
            (null, null) => "api",
            (null, _) => $"api.{routeName}",
            (_, null) => $"api.{module}",
            _ => $"api.{module}.{routeName}",
        };
    }

    private static string? ExtractModuleSlug(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Pattern: /api/v{n}/{module}/...
        // Skip the leading slash; we need at least three non-empty segments.
        var segments = path.AsSpan().Trim('/');
        int firstSlash = segments.IndexOf('/');
        if (firstSlash <= 0) return null;
        var seg0 = segments[..firstSlash];

        if (!seg0.Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rest = segments[(firstSlash + 1)..];
        int secondSlash = rest.IndexOf('/');
        if (secondSlash <= 0) return null;
        var seg1 = rest[..secondSlash];

        if (seg1.IsEmpty || (seg1[0] != 'v' && seg1[0] != 'V'))
        {
            return null;
        }

        var afterVersion = rest[(secondSlash + 1)..];
        int thirdSlash = afterVersion.IndexOf('/');
        var moduleSlug = thirdSlash < 0 ? afterVersion : afterVersion[..thirdSlash];

        return moduleSlug.IsEmpty ? null : moduleSlug.ToString().ToLowerInvariant();
    }
}
