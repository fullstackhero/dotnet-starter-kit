using FSH.Framework.Web;
using FSH.Framework.Web.TrustedProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Framework.Tests.Web;

/// <summary>
/// Pins the TrustedProxyOptions -> ForwardedHeadersOptions binding that AddHeroPlatform registers: which
/// upstreams end up trusted, that an unconfigured section keeps the framework's loopback-only default, and
/// that a malformed entry surfaces a message naming the offending setting rather than a bare FormatException.
/// </summary>
public sealed class TrustedProxyOptionsBindingTests
{
    private const string ProxyIp = "192.0.2.10";

    private static ForwardedHeadersOptions Resolve(Dictionary<string, string?> settings)
    {
        // DisableDefaults keeps the host's environment-variable and appsettings providers out, so an ambient
        // TrustedProxyOptions__* on the machine or CI runner can't change what "nothing configured" resolves to.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
        });
        builder.Configuration.AddInMemoryCollection(settings);
        builder.AddHeroPlatform();

        using var provider = builder.Services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    #region Trust boundary

    [Fact]
    public void ForwardedHeaders_Should_KeepFrameworkLoopbackDefault_When_NothingConfigured()
    {
        // Act
        var options = Resolve([]);

        // Assert - clearing the framework default here would make every caller a trusted proxy.
        options.KnownProxies.ShouldNotBeEmpty();
        options.KnownIPNetworks.ShouldNotBeEmpty();
    }

    [Fact]
    public void ForwardedHeaders_Should_TrustOnlyConfiguredProxy_When_KnownProxiesSet()
    {
        // Act
        var options = Resolve(new Dictionary<string, string?>
        {
            [$"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.KnownProxies)}:0"] = ProxyIp,
            [$"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.ForwardLimit)}"] = "2",
        });

        // Assert
        options.KnownProxies.ShouldBe([System.Net.IPAddress.Parse(ProxyIp)]);
        options.KnownIPNetworks.ShouldBeEmpty();
        options.ForwardLimit.ShouldBe(2);
    }

    #endregion

    #region Malformed configuration

    [Fact]
    public void ForwardedHeaders_Should_NameTheSetting_When_KnownProxyMalformed()
    {
        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Resolve(new Dictionary<string, string?>
        {
            [$"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.KnownProxies)}:0"] = "not-an-ip",
        }));

        // Assert
        exception.Message.ShouldContain("TrustedProxyOptions:KnownProxies");
        exception.Message.ShouldContain("not-an-ip");
    }

    [Fact]
    public void ForwardedHeaders_Should_NameTheSetting_When_KnownNetworkMalformed()
    {
        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Resolve(new Dictionary<string, string?>
        {
            [$"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.KnownNetworks)}:0"] = "10.0.0.0/999",
        }));

        // Assert
        exception.Message.ShouldContain("TrustedProxyOptions:KnownNetworks");
        exception.Message.ShouldContain("10.0.0.0/999");
    }

    [Theory]
    [InlineData("-1")]  // negative — overflows the middleware's buffer allocation, 500s every request
    [InlineData("0")]   // zero — truncates the unwind loop, forwarded headers silently stop being read
    public void ForwardedHeaders_Should_NameTheSetting_When_ForwardLimitBelowOne(string forwardLimit)
    {
        // Act - no proxies or networks configured, so this has to be rejected before the trust-boundary block.
        var exception = Should.Throw<InvalidOperationException>(() => Resolve(new Dictionary<string, string?>
        {
            [$"{nameof(TrustedProxyOptions)}:{nameof(TrustedProxyOptions.ForwardLimit)}"] = forwardLimit,
        }));

        // Assert
        exception.Message.ShouldContain("TrustedProxyOptions:ForwardLimit");
        exception.Message.ShouldContain($"is {forwardLimit}");
    }

    #endregion
}
