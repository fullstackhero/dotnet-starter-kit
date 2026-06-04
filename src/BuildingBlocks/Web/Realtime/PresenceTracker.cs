using System.Collections.Concurrent;

namespace FSH.Framework.Web.Realtime;

/// <summary>
/// Concurrent in-memory implementation of <see cref="IPresenceTracker"/>. Keys are user ids,
/// values are open-connection counts. Online transitions are reported back to the caller so
/// presence broadcasts can be triggered exactly once per change of state.
/// </summary>
public sealed class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);

    public bool Connect(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var transitioned = false;
        _counts.AddOrUpdate(
            userId,
            _ =>
            {
                transitioned = true;
                return 1;
            },
            // prev == 0 means a Disconnect set the count to 0 but hasn't removed the key yet —
            // this Connect resurrects it, which is still an offline→online transition.
            (_, prev) =>
            {
                if (prev == 0)
                {
                    transitioned = true;
                }
                return prev + 1;
            });
        return transitioned;
    }

    public bool Disconnect(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (!_counts.TryGetValue(userId, out var current)) return false;

        if (current <= 1)
        {
            // Use TryUpdate-then-remove to avoid removing while another thread is incrementing.
            if (_counts.TryUpdate(userId, 0, current))
            {
                _counts.TryRemove(new KeyValuePair<string, int>(userId, 0));
                return true;
            }
            return false;
        }

        _counts.TryUpdate(userId, current - 1, current);
        return false;
    }

    public bool IsOnline(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
        return _counts.TryGetValue(userId, out var count) && count > 0;
    }

    public IReadOnlyDictionary<string, bool> GetStatus(IEnumerable<string> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var map = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var id in userIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            map[id] = _counts.TryGetValue(id, out var count) && count > 0;
        }
        return map;
    }
}
