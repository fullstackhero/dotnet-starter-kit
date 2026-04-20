using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Web.Sse;

/// <summary>
/// Manages active SSE connections keyed by a per-connection <see cref="Guid"/> so a single user with
/// multiple tabs keeps every stream open. Supports targeted sends (by userId — fans out to all of the
/// user's active connections) and tenant-wide broadcasts. Thread-safe via ConcurrentDictionary.
/// </summary>
public sealed class SseConnectionManager
{
    private readonly ConcurrentDictionary<Guid, Connection> _connections = new();
    private readonly ILogger<SseConnectionManager> _logger;

    public SseConnectionManager(ILogger<SseConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a new connection and returns a stable connectionId plus the channel reader the
    /// endpoint will consume.
    /// </summary>
    public (Guid ConnectionId, ChannelReader<SseEvent> Reader) Connect(string userId, string? tenantId = null)
    {
        var connectionId = Guid.CreateVersion7();
        var channel = Channel.CreateBounded<SseEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        _connections[connectionId] = new Connection(userId, tenantId, channel);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("SSE client connected: connection={ConnectionId} user={UserId} tenant={TenantId}",
                connectionId, userId, tenantId ?? "none");
        }

        return (connectionId, channel.Reader);
    }

    /// <summary>
    /// Disconnects a specific connection and completes its channel.
    /// </summary>
    public void Disconnect(Guid connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            connection.Channel.Writer.TryComplete();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("SSE client disconnected: connection={ConnectionId} user={UserId}",
                    connectionId, connection.UserId);
            }
        }
    }

    /// <summary>
    /// Sends an event to every connection owned by the given user (all tabs, all devices).
    /// Returns the number of channels the event was written to.
    /// </summary>
    public int TrySend(string userId, SseEvent sseEvent)
    {
        var sent = 0;
        foreach (var (_, connection) in _connections)
        {
            if (string.Equals(connection.UserId, userId, StringComparison.Ordinal)
                && connection.Channel.Writer.TryWrite(sseEvent))
            {
                sent++;
            }
        }

        return sent;
    }

    /// <summary>
    /// Broadcasts an event to all connections in the specified tenant.
    /// </summary>
    public int Broadcast(string tenantId, SseEvent sseEvent)
    {
        var sent = 0;
        foreach (var (_, connection) in _connections)
        {
            if (string.Equals(connection.TenantId, tenantId, StringComparison.Ordinal)
                && connection.Channel.Writer.TryWrite(sseEvent))
            {
                sent++;
            }
        }

        return sent;
    }

    /// <summary>
    /// Broadcasts an event to every connected client (cross-tenant).
    /// </summary>
    public int BroadcastAll(SseEvent sseEvent)
    {
        var sent = 0;
        foreach (var (_, connection) in _connections)
        {
            if (connection.Channel.Writer.TryWrite(sseEvent))
            {
                sent++;
            }
        }

        return sent;
    }

    /// <summary>Number of active connections across all users.</summary>
    public int ActiveConnections => _connections.Count;

    private sealed record Connection(string UserId, string? TenantId, Channel<SseEvent> Channel);
}
