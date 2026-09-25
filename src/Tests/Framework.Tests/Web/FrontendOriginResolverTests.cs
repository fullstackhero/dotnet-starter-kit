using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Frontend;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Framework.Tests.Web;

/// <summary>
/// Tests for FrontendOriginResolver — resolves the SPA origin for user-facing links, validating the
/// request Origin header against the allow-list and falling back to the configured default. There
/// is no tier below that: with neither an allow-list match nor a default, resolution fails.
/// </summary>
public sealed class FrontendOriginResolverTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    private FrontendOriginResolver CreateResolver(string[] allowedOrigins, string? defaultOrigin = null)
    {
        var options = Options.Create(new FrontendOptions
        {
            AllowedOrigins = allowedOrigins,
            DefaultOrigin = defaultOrigin,
        });
        return new FrontendOriginResolver(_httpContextAccessor, options, NullLogger<FrontendOriginResolver>.Instance);
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
    public void ResolveDefault_Should_Throw_When_DefaultIsEmptyString()
    {
        // appsettings.Production.json ships "DefaultOrigin": "" — the empty string takes the same
        // path as an absent key, and there is no longer an API-origin tier under it to catch it.
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var resolver = CreateResolver([], defaultOrigin: "");

        var ex = Should.Throw<CustomException>(() => resolver.ResolveDefault());
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ResolveDefault_Should_NotFallBackToRequestHost_When_NothingConfigured()
    {
        // The link paths address the SPA (/confirm-email, /reset-password), so the request host is
        // not a serviceable substitute — and it is caller-supplied: honouring it mails a live reset
        // token to whatever domain the attacker put in the Host header. Fail instead, and do not
        // name the attacker's host in the message that reaches the caller.
        SetRequestHost("https", "evil.example.com");
        var resolver = CreateResolver([], defaultOrigin: null);

        var ex = Should.Throw<CustomException>(() => resolver.ResolveDefault());
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        ex.Message.ShouldNotContain("evil.example.com");
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
    public void ResolveForCurrentRequest_Should_Throw_When_NoHeaderAndNoDefault()
    {
        // The no-header path routes through ResolveDefault, so it inherits the same failure.
        SetOriginHeader(null);
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: null);

        var ex = Should.Throw<CustomException>(() => resolver.ResolveForCurrentRequest());
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ResolveForCurrentRequest_Should_StillReject_ForgedHeader_When_NoDefault()
    {
        // A present-but-unlisted Origin is a 400 — the caller's fault — and stays a 400 even when
        // the deployment is also missing its default. The two failures must not blur into one.
        SetOriginHeader("https://evil.example.com");
        var resolver = CreateResolver(["http://localhost:5173"], defaultOrigin: null);

        var ex = Should.Throw<CustomException>(() => resolver.ResolveForCurrentRequest());
        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ResolveDefault_Should_Throw_When_DefaultOriginHasNoScheme()
    {
        // Startup logs a value like this as the same failure as an unset one. That has to hold at
        // run time too: returning it would put a relative URL in the e-mail, which no mail client
        // turns into a link, and nothing would report a problem.
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var resolver = CreateResolver([], defaultOrigin: "app.example.com");

        var ex = Should.Throw<CustomException>(() => resolver.ResolveDefault());
        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ResolveDefault_Should_KeepBasePath_When_TheDefaultCarriesOne()
    {
        // A deployment serving the SPA under a sub-path configures it here. Validation must not
        // collapse the value to its authority: /reset-password would then 404.
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var resolver = CreateResolver([], defaultOrigin: "https://example.com/app/");

        resolver.ResolveDefault().ShouldBe("https://example.com/app");
    }
}
