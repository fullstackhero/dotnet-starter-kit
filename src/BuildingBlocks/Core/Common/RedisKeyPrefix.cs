namespace FSH.Framework.Core.Common;

/// <summary>
/// The per-application namespace for everything the framework writes to Redis: cache entries,
/// the Data Protection key ring, idempotency locks, quota counters and the SignalR backplane
/// channels. Read from <see cref="ConfigurationKey"/>.
/// </summary>
/// <remarks>
/// Without it, every application pointed at one Redis server shares those keys. A database index
/// (<c>defaultDatabase=N</c>) only separates keys: Redis pub/sub is server-wide, so the SignalR
/// backplane of every application still meets on one channel — and the hub's groups
/// (<c>tenant:root</c>, <c>user:{id}</c>) exist in all of them. Give each application and
/// environment its own prefix, e.g. <c>MyApp:prod</c>.
/// </remarks>
public static class RedisKeyPrefix
{
    /// <summary>Configuration key holding the prefix, shared by every Redis consumer.</summary>
    public const string ConfigurationKey = "CachingOptions:KeyPrefix";

    /// <summary>
    /// Normalizes a configured prefix to the form keys are built from: empty when unset (the
    /// historical, unprefixed key layout), otherwise the trimmed value ending in exactly one <c>:</c>.
    /// </summary>
    public static string Normalize(string? configured)
    {
        string value = configured?.Trim().TrimEnd(':') ?? string.Empty;
        return value.Length == 0 ? string.Empty : value + ":";
    }
}
