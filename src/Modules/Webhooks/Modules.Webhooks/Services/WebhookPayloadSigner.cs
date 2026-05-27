using System.Security.Cryptography;
using System.Text;

namespace FSH.Modules.Webhooks.Services;

public static class WebhookPayloadSigner
{
    public static string Sign(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
