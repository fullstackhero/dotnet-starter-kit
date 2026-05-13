namespace FSH.Framework.Web.Realtime;

/// <summary>
/// In-memory presence tracker — keeps a connection-count per user id so the
/// hub can broadcast online/offline transitions and an HTTP endpoint can
/// report a snapshot. Per-host (no Redis backplane for now); good enough
/// for single-replica deployments and dev. Multi-host scale-out would
/// require a shared store (Redis pub/sub on connect/disconnect events).
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Register a new connection for the user. Returns true when the user
    /// transitioned from offline → online (first connection), so the caller
    /// can broadcast PresenceChanged once.
    /// </summary>
    bool Connect(string userId);

    /// <summary>
    /// Drop a connection for the user. Returns true when the user
    /// transitioned from online → offline (last connection closed).
    /// </summary>
    bool Disconnect(string userId);

    /// <summary>
    /// True if the user has at least one open connection.
    /// </summary>
    bool IsOnline(string userId);

    /// <summary>Snapshot the online status for a batch of user ids.</summary>
    IReadOnlyDictionary<string, bool> GetStatus(IEnumerable<string> userIds);
}
