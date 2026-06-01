using Microsoft.AspNetCore.DataProtection;

namespace FSH.Modules.Webhooks.Services;

/// <summary>
/// Encrypts/decrypts webhook signing secrets at rest. The secret is the HMAC key used to sign
/// outbound payloads (<see cref="WebhookPayloadSigner"/>), so it must be recoverable — a one-way
/// hash would make signing impossible. We therefore protect it with ASP.NET Data Protection
/// (key ring persisted to Redis in production) rather than hashing it.
/// </summary>
public interface IWebhookSecretProtector
{
    string? Protect(string? plaintext);
    string? Unprotect(string? ciphertext);
}

public sealed class WebhookSecretProtector : IWebhookSecretProtector
{
    private readonly IDataProtector _protector;

    public WebhookSecretProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector("FSH.Webhooks.SubscriptionSecret.v1");
    }

    public string? Protect(string? plaintext) =>
        string.IsNullOrEmpty(plaintext) ? null : _protector.Protect(plaintext);

    public string? Unprotect(string? ciphertext) =>
        string.IsNullOrEmpty(ciphertext) ? null : _protector.Unprotect(ciphertext);
}
