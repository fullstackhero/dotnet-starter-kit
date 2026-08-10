using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Frontend;
using FSH.Framework.Web.Origin;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Framework.Tests.Web;

/// <summary>
/// Tests for FrontendOriginResolver — resolves the SPA origin for user-facing links, validating the
/// request Origin header against the allow-list, falling back to the configured default and, when
/// that is unset, to the API's own origin.
/// </summary>
public sealed class FrontendOriginResolverTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    private FrontendOriginResolver CreateResolver(string[] allowedOrigins, string? defaultOrigin = null, string? apiOrigin = null)
    {
        var options = Options.Create(new FrontendOptions
        {
            AllowedOrigins = allowedOrigins,
            DefaultOrigin = defaultOrigin,
        });
        var originOptions = Options.Create(new OriginOptions
        {
            OriginUrl = apiOrigin is null ? null : new Uri(apiOrigin, UriKind.RelativeOrAbsolute),
        });
        return new FrontendOriginResolver(_httpContextAccessor, options, originOptions, NullLogger<FrontendOriginResolver>.Instance);
    }

    private void SetOriginHeader(string? origin)
    {
        var context = new DefaultHttpContext();
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        _httpContextAccessor.HttpContext.Returns(context);
    }

    private void SetRequestHost(string scheme, string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        _httpContextAccessor.HttpContext.Returns(context);
    }

    // ── ResolveForCurrentRequest ────────────────────────────────────────────

    [Fact]
    public void ResolveForCurrentRequest_Should_ReturnCanonicalEntry_When_HeaderInAllowList()
    {
        SetOriginHeader("http://localhost:5173");
        var resolver = CreateResolver(["http://localhost:5173", "http://localhost:5174"]);

        resolver.ResolveForCurrentRequest().ShouldBe("http://localhost:5173");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_MatchIgnoringTrailingSlash()
    {
        SetOriginHeader("http://localhost:5173/");
        var resolver = CreateResolver(["http://localhost:5173"]);

        resolver.ResolveForCurrentRequest().ShouldBe("http://localhost:5173");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_ReturnCanonicalCasing_When_HeaderCasingDiffers()
    {
        // A client sending uppercased scheme/host must not steer the emitted link's casing:
        // the resolver returns the canonical allow-list entry, not the raw header.
        SetOriginHeader("HTTP://LOCALHOST:5173");
        var resolver = CreateResolver(["http://localhost:5173"]);

        resolver.ResolveForCurrentRequest().ShouldBe("http://localhost:5173");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_Reject_When_PortDiffers()
    {
        // :5174 must never match the :5173 allow-list entry (port compared exactly).
        SetOriginHeader("http://localhost:5174");
        var resolver = CreateResolver(["http://localhost:5173"]);

        var ex = Should.Throw<CustomException>(() => resolver.ResolveForCurrentRequest());
        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_Reject_When_HeaderForged()
    {
        SetOriginHeader("https://evil.example.com");
        var resolver = CreateResolver(["http://localhost:5173"]);

        var ex = Should.Throw<CustomException>(() => resolver.ResolveForCurrentRequest());
        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_FallBackToDefault_When_AllowListEmpty()
    {
        // appsettings.Production.json ships AllowedOrigins empty, and browsers attach Origin to
        // these POSTs even same-origin: matching an empty list would 400 every legitimate reset.
        SetOriginHeader("https://app.example.com");
        var resolver = CreateResolver([], defaultOrigin: "https://tenant.example.com");

        resolver.ResolveForCurrentRequest().ShouldBe("https://tenant.example.com");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_FallBackToDefault_When_EveryEntryIsUnparseable()
    {
        // Entries that are not absolute URLs are dropped at construction, so a list of nothing but
        // typos behaves as the empty list it effectively is. The startup warning counts the same way.
        SetOriginHeader("https://app.example.com");
        var resolver = CreateResolver(["https;//app.example.com"], defaultOrigin: "https://tenant.example.com");

        resolver.ResolveForCurrentRequest().ShouldBe("https://tenant.example.com");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_Reject_When_OnlyOtherEntriesParse()
    {
        // One good entry keeps the list live, so an origin that is not on it is still a 400 —
        // a partly-malformed list must not silently widen into the empty-list fallback.
        SetOriginHeader("https://app.example.com");
        var resolver = CreateResolver(["https;//app.example.com", "https://admin.example.com"], defaultOrigin: "https://tenant.example.com");

        var ex = Should.Throw<CustomException>(() => resolver.ResolveForCurrentRequest());
        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_ReturnConfiguredEntry_When_HeaderCarriesUserInfo()
    {
        // "http://evil.com@localhost:5173" compares equal on scheme+host+port, so the guarantee
        // that holds is returning the configured entry rather than anything the client sent.
        SetOriginHeader("http://evil.com@localhost:5173");
        var resolver = CreateResolver(["http://localhost:5173"]);

        resolver.ResolveForCurrentRequest().ShouldBe("http://localhost:5173");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_MatchIdnEntry_Against_PunycodeHeader()
    {
        // A list entry written in Unicode must match the punycode form the browser actually sends,
        // otherwise a valid IDN deployment fails closed. The emitted value stays the configured
        // entry, so an operator who writes Unicode gets Unicode in the link.
        SetOriginHeader("https://xn--bcher-kva.example");
        var resolver = CreateResolver(["https://bücher.example"]);

        resolver.ResolveForCurrentRequest().ShouldBe("https://bücher.example");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_MatchDefaultPort_Written_Explicitly()
    {
        // ":443" is the same origin as the bare host; an entry carrying it must not fail closed.
        SetOriginHeader("https://app.example.com");
        var resolver = CreateResolver(["https://app.example.com:443"]);

        resolver.ResolveForCurrentRequest().ShouldBe("https://app.example.com");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_FallBackToDefault_When_NoHeader()
    {
        // Non-browser callers (curl, mobile, server-to-server) send no Origin — use the default.
        SetOriginHeader(null);
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: "https://app.example.com");

        resolver.ResolveForCurrentRequest().ShouldBe("https://app.example.com");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_FallBackToDefault_When_NoHttpContext()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: "https://app.example.com");

        resolver.ResolveForCurrentRequest().ShouldBe("https://app.example.com");
    }

    // ── ResolveDefault ──────────────────────────────────────────────────────

    [Fact]
    public void ResolveDefault_Should_ReturnConfiguredDefault_TrailingSlashTrimmed()
    {
        var resolver = CreateResolver([], defaultOrigin: "https://app.example.com/");

        resolver.ResolveDefault().ShouldBe("https://app.example.com");
    }

    [Fact]
    public void ResolveDefault_Should_FallBackToApiOrigin_When_DefaultNotConfigured()
    {
        // An upgrader who never sets DefaultOrigin must keep booting and keep sending links: they
        // land on the API's own origin instead of the SPA, and startup warns about the degradation.
        var resolver = CreateResolver([], defaultOrigin: null, apiOrigin: "https://api.example.com");

        resolver.ResolveDefault().ShouldBe("https://api.example.com");
    }

    [Fact]
    public void ResolveDefault_Should_FallBackToApiOrigin_When_DefaultIsEmptyString()
    {
        // appsettings.Production.json ships "DefaultOrigin": "" — the empty string must take the
        // same fallback path as an absent key, not resolve to an empty link.
        var resolver = CreateResolver([], defaultOrigin: "", apiOrigin: "https://api.example.com/");

        resolver.ResolveDefault().ShouldBe("https://api.example.com");
    }

    [Fact]
    public void ResolveDefault_Should_PreferConfiguredDefault_Over_ApiOrigin()
    {
        var resolver = CreateResolver([], defaultOrigin: "https://app.example.com", apiOrigin: "https://api.example.com");

        resolver.ResolveDefault().ShouldBe("https://app.example.com");
    }

    [Fact]
    public void ResolveDefault_Should_IgnoreApiOrigin_When_NotAbsolute()
    {
        // OriginOptions:OriginUrl also ships as "" in Production, which binds to a relative Uri.
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var resolver = CreateResolver([], defaultOrigin: null, apiOrigin: "");

        var ex = Should.Throw<CustomException>(() => resolver.ResolveDefault());
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ResolveDefault_Should_FallBackToRequestHost_When_NothingConfigured()
    {
        // The both-empty upgrade case: appsettings.Production.json ships DefaultOrigin AND
        // OriginUrl empty, so the link still has to resolve — to the API's own host, which is
        // where register / self-register / resend built their links before this resolver existed.
        SetRequestHost("https", "api.example.com");
        var resolver = CreateResolver([], defaultOrigin: null);

        resolver.ResolveDefault().ShouldBe("https://api.example.com");
    }

    [Fact]
    public void ResolveDefault_Should_PreferApiOrigin_Over_RequestHost()
    {
        SetRequestHost("https", "internal.cluster.local");
        var resolver = CreateResolver([], defaultOrigin: null, apiOrigin: "https://api.example.com");

        resolver.ResolveDefault().ShouldBe("https://api.example.com");
    }

    [Fact]
    public void ResolveDefault_Should_Throw_When_NothingConfiguredAndNoRequest()
    {
        // A background job: nothing configured and no request to derive a host from.
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: null);

        var ex = Should.Throw<CustomException>(() => resolver.ResolveDefault());
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_FallBackToApiOrigin_When_NoHeaderAndNoDefault()
    {
        // The no-header path routes through ResolveDefault, so it inherits the same fallback.
        SetOriginHeader(null);
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: null, apiOrigin: "https://api.example.com");

        resolver.ResolveForCurrentRequest().ShouldBe("https://api.example.com");
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_StillReject_ForgedHeader_When_NoDefault()
    {
        // The boot-safety fallback must not soften the security contract: a present-but-unlisted
        // Origin is still a 400, never quietly swapped for the API origin.
        SetOriginHeader("https://evil.example.com");
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: null, apiOrigin: "https://api.example.com");

        var ex = Should.Throw<CustomException>(() => resolver.ResolveForCurrentRequest());
        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
