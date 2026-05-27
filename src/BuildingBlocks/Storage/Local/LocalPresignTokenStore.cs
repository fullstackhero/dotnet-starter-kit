using System.Collections.Concurrent;

namespace FSH.Framework.Storage.Local;

/// <summary>
/// In-memory store of short-lived upload tokens for the local-storage development fallback.
/// Production deployments use S3 — this exists so dev/test setups without MinIO still work.
/// Singleton lifetime; tokens are one-shot.
/// </summary>
public sealed class LocalPresignTokenStore
{
    private readonly ConcurrentDictionary<string, LocalPresignToken> _tokens = new(StringComparer.Ordinal);

    public string Issue(string storageKey, string contentType, long maxBytes, TimeSpan ttl)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new LocalPresignToken(storageKey, contentType, maxBytes, DateTimeOffset.UtcNow.Add(ttl));
        return token;
    }

    public LocalPresignToken? Consume(string token)
    {
        if (!_tokens.TryRemove(token, out var entry))
        {
            return null;
        }
        return entry.ExpiresAt < DateTimeOffset.UtcNow ? null : entry;
    }
}

public sealed record LocalPresignToken(string StorageKey, string ContentType, long MaxBytes, DateTimeOffset ExpiresAt);
