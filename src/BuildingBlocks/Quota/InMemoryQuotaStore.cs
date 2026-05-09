using System.Collections.Concurrent;

namespace FSH.Framework.Quota;

/// <summary>
/// Singleton backing store for <see cref="InMemoryQuotaService"/> so counters survive request scopes.
/// Keyed by <c>quota:{tenantId}:{resource}:{period}</c> exactly like the Redis backend.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class InMemoryQuotaStore
{
    public ConcurrentDictionary<string, long> Counters { get; } = new();
}
