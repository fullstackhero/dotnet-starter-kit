using System.Net;
using FSH.Framework.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Web.Frontend;

internal sealed class FrontendOriginResolver(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontendOptions> options,
    ILogger<FrontendOriginResolver> logger) : IFrontendOriginResolver
{
    // Normalize the allow-list once at construction: parse to Uri so matching is component-wise
    // (scheme + host + port) instead of a raw string compare that an entry like ":443" or an IDN
    // form would silently fail.
    private readonly Uri[] _allowed = Normalize(options.Value.AllowedOrigins);
    private readonly string? _default = options.Value.DefaultOrigin?.TrimEnd('/');

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
            // single-SPA and reverse-proxy topologies. The shipped Production config reaches this
            // branch because the dev origins live in appsettings.Development.json: an empty array in
            // an environment overlay does NOT clear the base file's entries, so leaving them in
            // appsettings.json put localhost in every production trust list (ShippedConfigurationTests).
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
            logger.LogDebug(
                "Rejected front-end origin {Origin}: not in FrontendOptions:AllowedOrigins",
                SanitizeForLog(header));
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

        // No usable origin, and deliberately nothing to fall back on. The two tiers that used to
        // sit here — the API's own origin, then the request host — both produce a broken or unsafe
        // link now that these paths address the SPA (/confirm-email, /reset-password) rather than
        // the API route: the API origin returns 404 for them, and the request host is whatever the
        // caller put in the Host header, which turns a password-reset e-mail into a token delivered
        // to an attacker's domain. Failing here is the only outcome that is neither.
        //
        // Callers surface this as a 500, which is honest: the deployment is missing a setting,
        // the user's request was fine. Startup logs an Error naming the setting.
        throw new CustomException(
            "No front-end origin is configured: set FrontendOptions:DefaultOrigin to the URL of the app that should receive these links.",
            errors: null,
            HttpStatusCode.InternalServerError);
    }

    // The header is caller-controlled, so it is truncated and stripped of line breaks before it
    // reaches a text sink: a newline in it would otherwise forge log lines. Same treatment the
    // global exception handler gives the request path.
    private static string SanitizeForLog(string value)
    {
        var single = value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        return single.Length <= 200 ? single : single[..200];
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
