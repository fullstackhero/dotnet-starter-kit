using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Caching;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.Identity.Services;

internal sealed class UserPermissionService(
    UserManager<FshUser> userManager,
    RoleManager<FshRole> roleManager,
    IdentityDbContext db,
    IGlobalCacheService cache) : IUserPermissionService
{
    // Hoisted to avoid per-call allocations. Small payload (< 4 KB after base64), so compression
    // CPU beats the marginal network savings — disable it for this hot path.
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
        Flags = HybridCacheEntryFlags.DisableCompression,
    };

    private static readonly string[] Tags = [CacheKeys.Tags.Permissions];

    public async Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        var set = await GetOrLoadAsync(userId, cancellationToken).ConfigureAwait(false);

        // Materialize a new List<string> on the way out to preserve the existing public contract.
        // The copy is ~50 ns for a typical permission set — negligible vs the JSON deserialization
        // we'd otherwise pay on every L1 hit without the [ImmutableObject] optimization.
        return [.. set.Values];
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        // Fast path: use the cached PermissionSet directly to avoid materializing a List<string>
        // just to check a single permission. Shares the cache entry with GetPermissionsAsync.
        var set = await GetOrLoadAsync(userId, cancellationToken).ConfigureAwait(false);
        return set.Contains(permission);
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken cancellationToken)
        => cache.RemoveAsync(CacheKeys.UserPermissions(userId), cancellationToken).AsTask();

    private ValueTask<PermissionSet> GetOrLoadAsync(string userId, CancellationToken cancellationToken)
    {
        // Stateless factory overload — the factory is a static method group, so the runtime
        // reuses a cached delegate and no closure is allocated per call (including L1 hits).
        var state = new FactoryState(userManager, roleManager, db, userId);

        return cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId),
            state,
            LoadPermissionsAsync,
            options: EntryOptions,
            tags: Tags,
            cancellationToken: cancellationToken);
    }

    private static async ValueTask<PermissionSet> LoadPermissionsAsync(FactoryState s, CancellationToken ct)
    {
        var user = await s.UserManager.FindByIdAsync(s.UserId).ConfigureAwait(false);
        _ = user ?? throw new UnauthorizedException();

        var userRoles = await s.UserManager.GetRolesAsync(user).ConfigureAwait(false);

        var roleIds = await s.RoleManager.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct).ConfigureAwait(false);

        if (roleIds.Count == 0)
        {
            return PermissionSet.Empty;
        }

        // Single query across all role IDs — cheaper than the old N+1 loop.
        var perms = await s.Db.RoleClaims
            .Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimType == ClaimConstants.Permission)
            .Select(rc => rc.ClaimValue!)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

        return perms.Count == 0
            ? PermissionSet.Empty
            : new PermissionSet([.. perms]);
    }

    // Struct state flows through HybridCache's TState parameter — avoids closure allocation.
    private readonly record struct FactoryState(
        UserManager<FshUser> UserManager,
        RoleManager<FshRole> RoleManager,
        IdentityDbContext Db,
        string UserId);
}
