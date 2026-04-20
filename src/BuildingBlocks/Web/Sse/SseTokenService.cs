using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace FSH.Framework.Web.Sse;

public sealed record SsePrincipal(string UserId, string? TenantId);

public interface ISseTokenService
{
    Task<Guid> IssueAsync(string userId, string? tenantId, CancellationToken cancellationToken);

    Task<SsePrincipal?> ConsumeAsync(Guid token, CancellationToken cancellationToken);
}

/// <summary>
/// Short-lived single-use token for authenticating SSE streams. Browsers' EventSource API cannot
/// attach Authorization headers, so clients exchange their JWT at /sse/token for an opaque token,
/// then open the stream at /sse/stream?token=&lt;guid&gt;. The token is deleted on first consume and
/// expires in 30 seconds otherwise. Backed by IDistributedCache (Redis in production) — single-use
/// tokens don't benefit from HybridCache's L1, and IDistributedCache is the right primitive since
/// we need true read-or-miss semantics without factory-populated nulls.
/// </summary>
internal sealed class SseTokenService(IDistributedCache cache) : ISseTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromSeconds(30);

    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TokenLifetime,
    };

    public async Task<Guid> IssueAsync(string userId, string? tenantId, CancellationToken cancellationToken)
    {
        var token = Guid.CreateVersion7();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new SsePrincipal(userId, tenantId));
        await cache.SetAsync(KeyFor(token), payload, EntryOptions, cancellationToken).ConfigureAwait(false);
        return token;
    }

    public async Task<SsePrincipal?> ConsumeAsync(Guid token, CancellationToken cancellationToken)
    {
        var key = KeyFor(token);
        var payload = await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (payload is null || payload.Length == 0)
        {
            return null;
        }

        await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SsePrincipal>(payload);
    }

    private static string KeyFor(Guid token) => $"sse:tok:{token:N}";
}
