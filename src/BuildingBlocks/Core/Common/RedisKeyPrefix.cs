namespace FSH.Framework.Core.Common;

/// <summary>
/// The per-application namespace for everything the framework writes to Redis: cache entries,
/// the Data Protection key ring, idempotency locks, quota counters and the SignalR backplane
/// channels. Read from <see cref="ConfigurationKey"/>.
/// </summary>
/// <remarks>
/// <para>
/// Without it, every application pointed at one Redis server shares those keys. A database index
/// (<c>defaultDatabase=N</c>) only separates keys: Redis pub/sub is server-wide, so the SignalR
/// backplane of every application still meets on one channel — and the hub's groups
/// (<c>tenant:root</c>, <c>user:{id}</c>) exist in all of them. Give each application and
/// environment its own prefix, e.g. <c>MyApp:prod</c>.
/// </para>
/// <para>
/// The prefix is opt-in: empty keeps the historical, unprefixed layout. Turning it on for an
/// existing deployment moves the Data Protection key ring, so copy it first
/// (<c>COPY DataProtection-Keys MyApp:prod:DataProtection-Keys</c>) — otherwise everything protected
/// with the old ring, including stored webhook signing secrets, can no longer be unprotected.
/// </para>
/// </remarks>
public static class RedisKeyPrefix
{
    /// <summary>Configuration key holding the prefix, shared by every Redis consumer.</summary>
    public const string ConfigurationKey = "CachingOptions:KeyPrefix";

    /// <summary>
    /// Validates and normalizes a configured prefix to the form keys are built from: empty when
    /// unset (the historical, unprefixed key layout), otherwise the value ending in exactly one
    /// <c>:</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The prefix contains whitespace, a control character, or a <c>{</c> / <c>}</c>. Braces would
    /// turn the prefix into a Redis Cluster hash tag and pin every key to a single slot.
    /// </exception>
    public static string Normalize(string? configured)
    {
        if (string.IsNullOrEmpty(configured))
        {
            return string.Empty;
        }

        if (configured.Any(static c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '{' or '}'))
        {
            throw new InvalidOperationException(
                $"The Redis key prefix '{configured}' ({ConfigurationKey}) is invalid: it must not contain " +
                "whitespace, control characters, '{' or '}' (braces form a Redis Cluster hash tag and pin " +
                "every key to one slot).");
        }

        string value = configured.TrimEnd(':');
        return value.Length == 0 ? string.Empty : value + ":";
    }
}
