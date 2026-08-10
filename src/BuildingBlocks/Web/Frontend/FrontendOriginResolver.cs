using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Origin;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Web.Frontend;

internal sealed class FrontendOriginResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontendOptions> options,
    IOptions<OriginOptions> originOptions,
    ILogger<FrontendOriginResolver> logger) : IFrontendOriginResolver
{
    // Normalize the allow-list once at construction: parse to Uri so matching is component-wise
    // (scheme + host + port) instead of a raw string compare that an entry like ":443" or an IDN
    // form would silently fail.
    private readonly Uri[] _allowed = Normalize(options.Value.AllowedOrigins);
    private readonly string? _default = options.Value.DefaultOrigin?.TrimEnd('/');
    // IsAbsoluteUri guard: OriginUrl is operator-supplied, and only an absolute Uri has an
    // AbsoluteUri to read.
    private readonly string? _apiOrigin = originOptions.Value.OriginUrl is { IsAbsoluteUri: true } api
        ? api.AbsoluteUri.TrimEnd('/')
        : null;

    public string ResolveForCurrentRequest()
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            // Non-browser caller (curl, mobile, server-to-server) sends no Origin. Fall back to the
            // configured default rather than failing an otherwise valid flow. Note the Scalar
            // try-it UI is NOT in this group: it fetches from the browser, so it sends the API's
            // own origin and needs that origin allow-listed to exercise these two endpoints.
            return ResolveDefault();
        }

        if (_allowed.Length == 0)
        {
            // No allow-list configured: there is nothing to validate the header against, so trust
            // the server-side default instead of rejecting. Browsers attach Origin to these POSTs
            // even same-origin, so matching an empty list would 400 every legitimate reset on the
            // single-SPA and reverse-proxy topologies — and on the shipped Production config.
            // The header is discarded, never echoed, so this cannot leak a client-chosen origin.
            return ResolveDefault();
        }

        var canonical = MatchAllowed(header);
        if (canonical is not null)
        {
            return canonical;
        }

        // A present-but-unlisted Origin is a forged or misconfigured client, not a server fault:
        // surface a 4xx so error-rate alerting doesn't page on bot traffic to anonymous endpoints.
        // Logged at Debug, not Warning: these endpoints are anonymous, so bot/forged traffic would
        // flood the aggregator at Warning. A genuine deployer misconfig (a real SPA origin missing
        // from the list) already surfaces loudly as a 400 to that SPA's own users.
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Rejected front-end origin {Origin}: not in FrontendOptions:AllowedOrigins", header);
        }
        throw new CustomException(
            "The request origin is not an allowed front-end origin.",
            errors: null,
            HttpStatusCode.BadRequest);
    }

    public string ResolveDefault()
    {
        if (!string.IsNullOrWhiteSpace(_default))
        {
            return _default;
        }

        // No DefaultOrigin: fall back to the API's own origin rather than taking the host down at
        // boot over a setting a deployment may never exercise. Links then land on the API — which
        // is where register / self-register / resend derived them from before the resolver existed
        // — and startup logs a single Warning naming what degrades. The configured value first, the
        // request host second: appsettings.Production.json ships OriginUrl empty too, and a
        // deployment that set neither must still send a usable link.
        //
        // Note this is the API's own host, never the caller's Origin header: an operator-driven
        // link must not point at the admin SPA the request came from, which is the whole reason
        // ResolveDefault exists apart from ResolveForCurrentRequest.
        if (!string.IsNullOrWhiteSpace(_apiOrigin))
        {
            return _apiOrigin;
        }

        var request = httpContextAccessor.HttpContext?.Request;
        if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
        {
            return $"{request.Scheme}://{request.Host.Value}{request.PathBase}".TrimEnd('/');
        }

        // Nothing configured and no request to derive from (a background job): there is no origin
        // to build a link out of.
        throw new CustomException(
            "No front-end origin is configured: set FrontendOptions:DefaultOrigin (or OriginOptions:OriginUrl as a fallback).",
            errors: null,
            HttpStatusCode.InternalServerError);
    }

    private string? MatchAllowed(string header)
    {
        if (!Uri.TryCreate(header.TrimEnd('/'), UriKind.Absolute, out var candidate))
        {
            return null;
        }

        // Return the canonical configured entry, never the client-supplied casing.
        return _allowed.FirstOrDefault(allowed => IsSameOrigin(candidate, allowed))
            ?.GetLeftPart(UriPartial.Authority);
    }

    // Scheme + host + port, port exact. Compared through IdnHost so a list entry written in Unicode
    // ("https://bücher.example") matches the punycode form the browser actually sends; Uri.Port
    // supplies the scheme's default, so ":443" and the bare host are the same origin.
    private static bool IsSameOrigin(Uri candidate, Uri allowed)
    {
        return string.Equals(candidate.Scheme, allowed.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.IdnHost, allowed.IdnHost, StringComparison.OrdinalIgnoreCase)
            && candidate.Port == allowed.Port;
    }

    // Internal so the startup warning reports the list the resolver will actually match against,
    // not the raw config array: an entry that fails to parse is dropped here and would otherwise
    // leave a fully malformed list looking configured while every link silently used the default.
    internal static Uri[] Normalize(string[] origins)
    {
        var list = new List<Uri>(origins.Length);
        foreach (var origin in origins)
        {
            if (Uri.TryCreate(origin.TrimEnd('/'), UriKind.Absolute, out var uri))
            {
                list.Add(uri);
            }
        }

        return [.. list];
    }
}
