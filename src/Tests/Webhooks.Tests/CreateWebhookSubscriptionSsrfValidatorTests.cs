using System.Net;
using FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Services;
using Webhooks.Tests.Support;

namespace Webhooks.Tests;

/// <summary>
/// Regression for audit finding SEC-01 (SSRF: outbound webhook delivery must have an egress guard).
/// The create-boundary validator rejects URLs that point at loopback / link-local (cloud metadata) /
/// private ranges, and <see cref="WebhookUrlGuard.IsBlockedAddress"/> is the authoritative connect-time
/// gate applied to the resolved IP (defeats DNS rebinding) shared by both delivery sinks.
/// </summary>
public sealed class CreateWebhookSubscriptionSsrfValidatorTests
{
    private readonly CreateWebhookSubscriptionCommandValidator _validator = new(WebhooksResourcesLocalizerFactory.Create());

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")] // cloud instance metadata
    [InlineData("http://127.0.0.1/")]                          // loopback
    [InlineData("http://10.0.0.1/")]                           // RFC1918 private range
    [InlineData("http://localhost/hook")]                      // loopback by name
    public void Validate_Should_Reject_SsrfTargetUrl(string url)
    {
        var command = new CreateWebhookSubscriptionCommand(url, ["ticket.created"], Secret: null);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse(
            $"webhook target '{url}' resolves to a private/loopback/metadata address and must be rejected " +
            "at the create boundary to prevent SSRF.");
    }

    [Fact]
    public void Validate_Should_Accept_PublicHttpsUrl()
    {
        var command = new CreateWebhookSubscriptionCommand("https://hooks.example.com/receive", ["ticket.created"], Secret: null);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1", true)]     // loopback
    [InlineData("10.5.6.7", true)]      // RFC1918
    [InlineData("172.16.0.1", true)]    // RFC1918
    [InlineData("192.168.1.1", true)]   // RFC1918
    [InlineData("169.254.169.254", true)] // link-local / metadata
    [InlineData("100.64.0.1", true)]    // carrier-grade NAT
    [InlineData("::1", true)]           // IPv6 loopback
    [InlineData("fd00::1", true)]       // IPv6 unique-local
    [InlineData("8.8.8.8", false)]      // public
    [InlineData("1.1.1.1", false)]      // public
    public void IsBlockedAddress_Should_ScreenNonRoutableTargets(string ip, bool blocked) =>
        WebhookUrlGuard.IsBlockedAddress(IPAddress.Parse(ip)).ShouldBe(blocked);
}
