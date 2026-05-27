using System.Collections.Immutable;
using System.ComponentModel;

namespace FSH.Modules.Identity.Caching;

/// <summary>
/// Immutable container for a user's permission set. Used as the cache value type in
/// <see cref="Services.UserPermissionService"/> so HybridCache can reuse the in-process
/// instance across requests without re-deserializing on every L1 hit.
/// </summary>
/// <remarks>
/// Must stay <c>sealed</c> + <see cref="ImmutableObjectAttribute"/> — removing either
/// silently degrades HybridCache L1 reads back to per-call JSON deserialization.
/// </remarks>
[ImmutableObject(true)]
internal sealed record PermissionSet(ImmutableArray<string> Values)
{
    public static PermissionSet Empty { get; } = new(ImmutableArray<string>.Empty);

    public bool Contains(string permission) => Values.Contains(permission);
}
