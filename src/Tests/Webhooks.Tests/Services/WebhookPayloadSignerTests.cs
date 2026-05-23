using System.Security.Cryptography;
using System.Text;
using FSH.Modules.Webhooks.Services;

namespace Webhooks.Tests.Services;

public sealed class WebhookPayloadSignerTests
{
    #region Happy Path

    [Fact]
    public void Sign_Should_Prefix_With_Sha256_And_Lowercase_Hex()
    {
        string signature = WebhookPayloadSigner.Sign("{\"id\":1}", "secret");

        signature.ShouldStartWith("sha256=");
        string hex = signature["sha256=".Length..];
        hex.ShouldBe(hex.ToLowerInvariant());
        hex.Length.ShouldBe(64); // SHA-256 => 32 bytes => 64 hex chars
    }

    [Fact]
    public void Sign_Should_Match_Independently_Computed_Hmac()
    {
        const string payload = "{\"event\":\"user.created\"}";
        const string secret = "top-secret-key";

        string signature = WebhookPayloadSigner.Sign(payload, secret);

        byte[] expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(payload));
        signature.ShouldBe($"sha256={Convert.ToHexString(expected).ToLowerInvariant()}");
    }

    [Fact]
    public void Sign_Should_Be_Deterministic_For_Same_Inputs()
    {
        string a = WebhookPayloadSigner.Sign("payload", "secret");
        string b = WebhookPayloadSigner.Sign("payload", "secret");

        a.ShouldBe(b);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Sign_Should_Produce_Valid_Signature_For_Empty_Payload()
    {
        string signature = WebhookPayloadSigner.Sign(string.Empty, "secret");

        signature.ShouldStartWith("sha256=");
        signature["sha256=".Length..].Length.ShouldBe(64);
    }

    [Fact]
    public void Sign_Should_Produce_Valid_Signature_For_Empty_Secret()
    {
        string signature = WebhookPayloadSigner.Sign("payload", string.Empty);

        signature.ShouldStartWith("sha256=");
        signature["sha256=".Length..].Length.ShouldBe(64);
    }

    [Fact]
    public void Sign_Should_Handle_Large_Payload()
    {
        string largePayload = new('x', 1_000_000);

        string signature = WebhookPayloadSigner.Sign(largePayload, "secret");

        signature.ShouldStartWith("sha256=");
        signature["sha256=".Length..].Length.ShouldBe(64);
    }

    [Fact]
    public void Sign_Should_Differ_When_Secret_Differs()
    {
        string a = WebhookPayloadSigner.Sign("payload", "key-1");
        string b = WebhookPayloadSigner.Sign("payload", "key-2");

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Sign_Should_Differ_When_Payload_Differs()
    {
        string a = WebhookPayloadSigner.Sign("payload-a", "secret");
        string b = WebhookPayloadSigner.Sign("payload-b", "secret");

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Sign_Should_Handle_Unicode_Payload()
    {
        // UTF-8 encoding path: multi-byte chars must hash without throwing.
        string signature = WebhookPayloadSigner.Sign("{\"name\":\"日本語 🚀\"}", "secret");

        signature.ShouldStartWith("sha256=");
        signature["sha256=".Length..].Length.ShouldBe(64);
    }

    #endregion
}
