using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Web.Realtime;

/// <summary>
/// Single shared SignalR hub for app-wide realtime: chat messages, typing indicators, presence,
/// notifications. Modules don't depend on this hub directly — they emit through
/// <see cref="IHubContext{AppHub}"/> and target the well-known SignalR groups.
///
/// Group naming convention:
/// <list type="bullet">
///   <item><c>user:{userId}</c> — every connection a user has open. Used for cross-channel pushes
///   (notifications, channel-added, etc.).</item>
///   <item><c>channel:{channelId}</c> — every connection of every member of that channel. Used
///   for chat message broadcasts.</item>
/// </list>
/// </summary>
[Authorize]
public sealed class AppHub : Hub
{
    /// <summary>Throttle window for typing indicators per (channel, user).</summary>
    private static readonly TimeSpan TypingThrottle = TimeSpan.FromSeconds(3);

    private readonly IChannelMembershipChecker _membership;
    private readonly IDistributedCache _cache;
    private readonly IUserChannelLookup _channels;
    private readonly IPresenceTracker _presence;
    private readonly ILogger<AppHub> _logger;

    public AppHub(
        IChannelMembershipChecker membership,
        IDistributedCache cache,
        IUserChannelLookup channels,
        IPresenceTracker presence,
        ILogger<AppHub> logger)
    {
        _membership = membership;
        _cache = cache;
        _channels = channels;
        _presence = presence;
        _logger = logger;
    }

    /// <summary>
    /// Reads the authenticated user id off the connection's principal. Cannot use
    /// <c>ICurrentUser</c> here because it resolves through <c>IHttpContextAccessor</c> — the
    /// originating negotiate <c>HttpContext</c> is not pinned to subsequent hub method invocations,
    /// so any indirection through it returns nulls.
    /// </summary>
    private string? GetUserId()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true) return null;
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("uid");
    }

    /// <summary>
    /// Reads the tenant id off the principal — used to scope cross-tenant
    /// broadcasts (presence) to a single tenant group so a 1000-user tenant
    /// doesn't broadcast every connect to other tenants.
    /// </summary>
    private string? GetTenantId()
    {
        var user = Context.User;
        if (user is null) return null;
        return user.FindFirstValue("tenant")
            ?? user.FindFirstValue("tid")
            ?? user.FindFirstValue("tenantId");
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId) || userId == Guid.Empty.ToString())
            {
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}", Context.ConnectionAborted)
                .ConfigureAwait(false);

            // Join the tenant group — scopes cross-tenant broadcasts (presence) so a 1000-user
            // tenant doesn't broadcast every connect to other tenants.
            var tenantId = GetTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}", Context.ConnectionAborted)
                    .ConfigureAwait(false);
            }

            var channelIds = await _channels
                .ListMyChannelIdsAsync(userId, Context.ConnectionAborted)
                .ConfigureAwait(false);

            foreach (var channelId in channelIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}", Context.ConnectionAborted)
                    .ConfigureAwait(false);
            }

            AppHubLog.Connected(_logger, Context.ConnectionId, userId, channelIds.Count);

            // On the user's first open connection, broadcast PresenceChanged so clients flip the dot.
            // Scoped to the tenant group, not Clients.All, to avoid global fan-out.
            if (_presence.Connect(userId))
            {
                var target = string.IsNullOrEmpty(tenantId)
                    ? Clients.All
                    : Clients.Group($"tenant:{tenantId}");
                await target.SendAsync(
                        "PresenceChanged",
                        new { userId, online = true },
                        Context.ConnectionAborted)
                    .ConfigureAwait(false);
            }

            await base.OnConnectedAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
        {
            // Client disconnected mid-connect (fast reconnect, page navigation, negotiate/connect
            // churn). The aborting token cancels the in-flight group joins / channel lookup. There's
            // no connection left to set up, so this is expected — swallow it rather than let it
            // surface as a hub-dispatch error in the logs.
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId) && _presence.Disconnect(userId))
        {
            var tenantId = GetTenantId();
            var target = string.IsNullOrEmpty(tenantId)
                ? Clients.All
                : Clients.Group($"tenant:{tenantId}");
            await target.SendAsync(
                    "PresenceChanged",
                    new { userId, online = false })
                .ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    /// <summary>
    /// Client invokes <c>Typing(channelId)</c> while composing. Throttled to once per 3s per
    /// (channel, user) via the distributed cache so chatty UIs don't flood the wire.
    /// </summary>
    public async Task Typing(Guid channelId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        if (!await _membership.IsMemberAsync(channelId, userId, Context.ConnectionAborted).ConfigureAwait(false))
        {
            return;
        }

        var key = $"typing:{channelId}:{userId}";
        var existing = await _cache.GetStringAsync(key, Context.ConnectionAborted).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(existing)) return;

        await _cache.SetStringAsync(
                key,
                "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TypingThrottle },
                Context.ConnectionAborted)
            .ConfigureAwait(false);

        await Clients.OthersInGroup($"channel:{channelId}")
            .SendAsync("ChatTypingStarted", new { channelId, userId }, Context.ConnectionAborted)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Client invokes <c>JoinChannel(channelId)</c> when it opens a conversation.
    /// <see cref="OnConnectedAsync"/> only pre-joins the channels that existed — and that the user
    /// was already a member of — at connect time. A DM/channel created, or a membership granted,
    /// <em>after</em> the socket is live would otherwise never receive <c>channel:{id}</c> broadcasts
    /// until the page reloads and a fresh connection re-enumerates memberships. This joins the group
    /// on demand, gated by the same membership check used for typing. Idempotent — re-joining a group
    /// you're already in is a no-op, so the client can call it freely on open and on reconnect.
    /// </summary>
    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        if (!await _membership.IsMemberAsync(channelId, userId, Context.ConnectionAborted).ConfigureAwait(false))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}", Context.ConnectionAborted)
            .ConfigureAwait(false);
    }
}

internal static partial class AppHubLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "AppHub connection {ConnectionId} for user {UserId} pre-joined {ChannelCount} channel groups")]
    public static partial void Connected(ILogger logger, string connectionId, string userId, int channelCount);
}
