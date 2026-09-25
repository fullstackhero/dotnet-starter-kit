using FSH.Framework.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Framework.Tests.Web;

/// <summary>
/// The sibling of <see cref="TrustedProxyOptionsBindingTests"/> for the one host shape that class cannot
/// reach. It builds through <c>Host.CreateApplicationBuilder</c>, which never runs
/// ConfigureWebDefaults, so the framework's loopback defaults are always still in place when
/// AddHeroPlatform looks at them. A web host started with FORWARDEDHEADERS_ENABLED registers
/// ForwardedHeadersOptionsSetup, which empties both trust lists — and an empty trust list is not
/// "trust nobody" in ForwardedHeadersMiddleware, it is "check nobody": the middleware only validates
/// the peer when at least one entry exists. That is the configuration this pins.
/// </summary>
public sealed class ForwardedHeadersHostDefaultsTests
{
    private static ForwardedHeadersOptions ResolveWithAspNetForwarding()
    {
        // Passed as a command-line arg rather than an environment variable: host configuration reads
        // both, and an env var would leak into every other test running in this process.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = ["--FORWARDEDHEADERS_ENABLED=true"],
            EnvironmentName = "Development",
        });
        builder.AddHeroPlatform();

        // The premise of this whole test: ASP.NET registered its own setup for these options. If the
        // flag ever stops reaching host configuration, the assertions below would pass for the wrong
        // reason — nothing cleared the lists, so nothing had to restore them.
        builder.Services.Any(d =>
                d.ServiceType == typeof(IConfigureOptions<ForwardedHeadersOptions>) &&
                d.ImplementationType?.Name == "ForwardedHeadersOptionsSetup")
            .ShouldBeTrue("FORWARDEDHEADERS_ENABLED did not reach host configuration");

        // Not builder.Build(): the host validates the whole container, and the modules that supply
        // ICurrentUser and friends are not registered here. Only the options matter.
        using var provider = builder.Services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Fact]
    public void ForwardedHeaders_Should_TrustSomeone_When_NothingConfiguredAndAspNetForwardingEnabled()
    {
        // Act
        var options = ResolveWithAspNetForwarding();

        // Assert — with both lists empty the middleware skips the peer check entirely and rewrites
        // RemoteIpAddress from X-Forwarded-For sent by anyone at all.
        (options.KnownProxies.Count + options.KnownIPNetworks.Count).ShouldBeGreaterThan(
            0,
            "an empty trust list makes ForwardedHeadersMiddleware accept X-Forwarded-For from any peer");

        // And what is restored is the framework's own default, not a trust policy of our own
        // invention: with nothing configured the app must trust exactly loopback, no wider.
        var frameworkDefaults = new ForwardedHeadersOptions();
        options.KnownProxies.ShouldBe(frameworkDefaults.KnownProxies);
        options.KnownIPNetworks.ShouldBe(frameworkDefaults.KnownIPNetworks);
    }
}
