namespace FSH.Framework.Caching;

/// <summary>
/// Cache key conventions and tag constants used across the FullStackHero starter kit.
/// Keys should be tenant-scoped where applicable; tags enable bulk invalidation via
/// <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache.RemoveByTagAsync(string, System.Threading.CancellationToken)"/>.
/// </summary>
public static class CacheKeys
{
    /// <summary>Well-known tag values for bulk invalidation.</summary>
    public static class Tags
    {
        /// <summary>Tag applied to every permission entry.</summary>
        public const string Permissions = "permissions";

        /// <summary>Tag applied to every tenant theme entry.</summary>
        public const string Themes = "themes";

        /// <summary>Tag applied to every idempotency replay entry.</summary>
        public const string Idempotency = "idempotency";

        /// <summary>Per-tenant tag — invalidates all entries scoped to a tenant.</summary>
        public static string Tenant(string tenantId) => $"tenant:{tenantId}";

        /// <summary>Per-user tag — invalidates all entries scoped to a user.</summary>
        public static string User(string userId) => $"user:{userId}";
    }

    /// <summary>Key for the permission list of a given user.</summary>
    public static string UserPermissions(string userId) => $"perm:u:{userId}";

    /// <summary>Key for a tenant-specific theme.</summary>
    public static string TenantTheme(string tenantId) => $"theme:t:{tenantId}";

    /// <summary>Key for the system-wide default theme.</summary>
    public const string DefaultTheme = "theme:default";

    /// <summary>Key for an idempotency replay entry, scoped by tenant.</summary>
    public static string IdempotencyEntry(string tenantId, string key) => $"idem:t:{tenantId}:{key}";

    /// <summary>
    /// Key for the impersonation-grant revocation marker, indexed by JWT id.
    /// Read on every authenticated request that carries an act_sub claim.
    /// </summary>
    public static string ImpersonationGrantStatus(string jti) => $"impgrant:{jti}";
}
