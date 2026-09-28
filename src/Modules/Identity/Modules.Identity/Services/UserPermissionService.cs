using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Caching;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Identity.Services;

internal sealed class UserPermissionService(
    UserManager<FshUser> userManager,
    RoleManager<FshRole> roleManager,
    IdentityDbContext db,
    HybridCache cache,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory) : IUserPermissionService
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

        // Copy to a new List<string> to preserve the public contract; ~50 ns is negligible vs the
        // JSON deserialization we'd otherwise pay per L1 hit without the [ImmutableObject] optimization.
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
        var state = new FactoryState(userManager, roleManager, db, CallerTenantOtherThanResolved(userId), scopeFactory, userId);

        return cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId),
            state,
            LoadPermissionsAsync,
            options: EntryOptions,
            tags: Tags,
            cancellationToken: cancellationToken);
    }

    // The cache key carries no tenant, and a root operator's request can resolve to another tenant through
    // the tenant-header override. Loading under the request's tenant would not find the user there, so a cold
    // entry turned a valid cross-tenant request into a 401. When the checked user is the caller and the caller's
    // tenant claim differs from the resolved tenant, the set is loaded under the claim's tenant instead.
    private string? CallerTenantOtherThanResolved(string userId)
    {
        var caller = httpContextAccessor.HttpContext?.User;
        if (caller?.Identity?.IsAuthenticated != true
            || !string.Equals(caller.GetUserId(), userId, StringComparison.Ordinal)
            || caller.GetTenant() is not { Length: > 0 } claimTenant)
        {
            return null;
        }

        return string.Equals(claimTenant, tenantAccessor.MultiTenantContext.TenantInfo?.Id, StringComparison.Ordinal)
            ? null
            : claimTenant;
    }

    private static async ValueTask<PermissionSet> LoadPermissionsAsync(FactoryState s, CancellationToken ct)
    {
        if (s.HomeTenantId is not { } homeTenantId)
        {
            return await LoadInCurrentTenantAsync(s.UserManager, s.RoleManager, s.Db, s.UserId, ct).ConfigureAwait(false);
        }

        await using var scope = s.ScopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var homeTenant = await services.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(homeTenantId).ConfigureAwait(false)
            ?? throw new UnauthorizedException();
        services.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(homeTenant);

        return await LoadInCurrentTenantAsync(
            services.GetRequiredService<UserManager<FshUser>>(),
            services.GetRequiredService<RoleManager<FshRole>>(),
            services.GetRequiredService<IdentityDbContext>(),
            s.UserId,
            ct).ConfigureAwait(false);
    }

    private static async Task<PermissionSet> LoadInCurrentTenantAsync(
        UserManager<FshUser> userManager,
        RoleManager<FshRole> roleManager,
        IdentityDbContext db,
        string userId,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        _ = user ?? throw new UnauthorizedException();

        var userRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        var directRoleIds = await roleManager.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct).ConfigureAwait(false);

        // Group-derived roles confer permissions too — the JWT already unions them
        // (IdentityService.AddRoleClaimsAsync) and every group mutation invalidates this
        // cache entry, so the effective set must include roles reachable via UserGroups.
        var groupRoleIds = await db.GroupRoles
            .Where(gr => db.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => ug.GroupId)
                .Contains(gr.GroupId))
            .Select(gr => gr.RoleId)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

        var roleIds = directRoleIds.Union(groupRoleIds, StringComparer.Ordinal).ToList();

        if (roleIds.Count == 0)
        {
            return PermissionSet.Empty;
        }

        // Single query across all role IDs — cheaper than the old N+1 loop.
        var perms = await db.RoleClaims
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
        string? HomeTenantId,
        IServiceScopeFactory ScopeFactory,
        string UserId);
}
