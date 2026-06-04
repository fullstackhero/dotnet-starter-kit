using FSH.Modules.Webhooks.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Webhooks.Tests.Services;

/// <summary>
/// The signing secret is the HMAC key, so it must be stored encrypted-at-rest yet remain
/// recoverable for signing. These tests pin both properties: the protected value is never the
/// plaintext (no plaintext-at-rest), and it round-trips back exactly.
/// </summary>
public sealed class WebhookSecretProtectorTests
{
    private static WebhookSecretProtector CreateProtector() =>
        new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Protect_Should_Not_Return_Plaintext()
    {
        var protector = CreateProtector();
        const string secret = "super-secret-signing-key-123";

        var protectedValue = protector.Protect(secret);

        protectedValue.ShouldNotBeNull();
        protectedValue.ShouldNotBe(secret, "the secret must not be stored as plaintext at rest");
        protectedValue.ShouldNotContain(secret);
    }

    [Fact]
    public void Unprotect_Should_RoundTrip_To_Original_Secret()
    {
        var protector = CreateProtector();
        const string secret = "round-trip-secret-7f3a";

        var roundTripped = protector.Unprotect(protector.Protect(secret));

        roundTripped.ShouldBe(secret);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Protect_And_Unprotect_Should_Pass_Through_NullOrEmpty(string? value)
    {
        var protector = CreateProtector();

        protector.Protect(value).ShouldBeNull();
        protector.Unprotect(value).ShouldBeNull();
    }
}
