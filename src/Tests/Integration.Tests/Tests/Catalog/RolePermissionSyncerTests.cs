using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Direct service-level test for <see cref="RolePermissionSyncer"/>. Verifies the
/// "permissions added in a release after tenant was already provisioned" path —
/// which the per-tenant <c>IDbInitializer.SeedAsync</c> flow does not run again
/// for already-provisioned tenants in production. This is the path that produced
/// 401s in dev when the Catalog module was added. Also verifies the reverse path:
/// a permission the catalog stops granting is revoked from the built-in roles.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RolePermissionSyncerTests
{
    // Well-formed but never registered: stands in for a permission a release removed from the catalog.
    private const string RetiredPermission = "Permissions.Retired.View";
    private const string NonPermissionClaimType = "fsh.test.marker";

    private readonly FshWebApplicationFactory _factory;

    public RolePermissionSyncerTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SyncAsync_Should_Restore_Missing_Permission_Claims_For_Admin_Role()
    {
        var rootTenant = await GetRootTenantAsync();

        var catalogPermissions = CatalogPermissions.All.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        // 1. Wipe Admin's Catalog.* claims directly. Simulates "perms registered in a
        //    later release that never made it into this tenant's RoleClaims table."
        await WipeClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission, catalogPermissions);

        var afterWipe = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);
        afterWipe.Intersect(catalogPermissions).ShouldBeEmpty(
            "Pre-condition: Admin must not have any Catalog claims after the wipe.");

        // 2. Run the syncer through the production scope/tenant pattern.
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            var syncer = scope.ServiceProvider.GetRequiredService<RolePermissionSyncer>();
            await syncer.SyncAsync(CancellationToken.None);
        }

        // 3. All Catalog perms should be back on the Admin role.
        var afterSync = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);
        var missing = catalogPermissions.Where(p => !afterSync.Contains(p)).ToList();
        missing.ShouldBeEmpty(
            $"Syncer failed to restore {missing.Count} catalog permission(s): " +
            $"[{string.Join(", ", missing)}]");
    }

    [Fact]
    public async Task SyncAsync_Should_Be_Idempotent_When_Claims_Already_Exist()
    {
        var rootTenant = await GetRootTenantAsync();
        var before = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);

        // Running the syncer twice in a row must not duplicate claims or throw.
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            var syncer = scope.ServiceProvider.GetRequiredService<RolePermissionSyncer>();
            await syncer.SyncAsync(CancellationToken.None);
            await syncer.SyncAsync(CancellationToken.None);
        }

        var after = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);
        after.Count.ShouldBe(before.Count, "Syncer must not duplicate existing permission claims.");
    }

    [Fact]
    public async Task SyncAsync_Should_Remove_Permission_Claims_The_Catalog_No_Longer_Grants()
    {
        var rootTenant = await GetRootTenantAsync();

        // An Admin-only permission left on Basic is what narrowing IsBasic leaves behind on existing tenants.
        const string narrowedPermission = IdentityPermissions.Roles.Delete;
        PermissionConstants.Basic.ShouldNotContain(p => p.Name == narrowedPermission);
        PermissionConstants.Admin.ShouldContain(p => p.Name == narrowedPermission);

        await SeedClaimAsync(rootTenant, "Basic", ClaimConstants.Permission, RetiredPermission);
        await SeedClaimAsync(rootTenant, "Basic", ClaimConstants.Permission, narrowedPermission);
        await SeedClaimAsync(rootTenant, "Basic", NonPermissionClaimType, "kept");
        await SeedClaimAsync(rootTenant, "Admin", ClaimConstants.Permission, RetiredPermission);

        try
        {
            await RunSyncerAsync(rootTenant);

            var basic = await GetClaimsAsync(rootTenant, "Basic", ClaimConstants.Permission);
            basic.ShouldBe(
                PermissionConstants.Basic.Select(p => p.Name).ToHashSet(StringComparer.Ordinal),
                ignoreOrder: true,
                "Basic must hold exactly the catalog's IsBasic permissions after the sync.");

            var admin = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);
            admin.ShouldNotContain(RetiredPermission);
            admin.ShouldContain(narrowedPermission);

            var nonPermission = await GetClaimsAsync(rootTenant, "Basic", NonPermissionClaimType);
            nonPermission.ShouldContain("kept", "The sync must only reconcile permission claims.");
        }
        finally
        {
            await WipeClaimsAsync(rootTenant, "Basic", ClaimConstants.Permission, [RetiredPermission, narrowedPermission]);
            await WipeClaimsAsync(rootTenant, "Basic", NonPermissionClaimType, ["kept"]);
            await WipeClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission, [RetiredPermission]);
        }
    }

    [Fact]
    public async Task SyncAsync_Should_Keep_Root_Permissions_On_Root_Tenant_Admin()
    {
        var rootTenant = await GetRootTenantAsync();
        PermissionConstants.Root.ShouldNotBeEmpty();

        await RunSyncerAsync(rootTenant);

        var admin = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);
        var missing = PermissionConstants.Root.Select(p => p.Name).Where(p => !admin.Contains(p)).ToList();
        missing.ShouldBeEmpty(
            $"Syncer stripped {missing.Count} root permission(s) from the root tenant's Admin: " +
            $"[{string.Join(", ", missing)}]");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private async Task<AppTenantInfo> GetRootTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(MultitenancyConstants.Root.Id);
        tenant.ShouldNotBeNull("Root tenant must be provisioned before this test runs.");
        return tenant;
    }

    private async Task RunSyncerAsync(AppTenantInfo tenant)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var syncer = scope.ServiceProvider.GetRequiredService<RolePermissionSyncer>();
        await syncer.SyncAsync(CancellationToken.None);
    }

    private async Task SeedClaimAsync(AppTenantInfo tenant, string roleName, string claimType, string claimValue)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.Roles.SingleAsync(r => r.Name == roleName);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.RoleClaims.Add(new FshRoleClaim { RoleId = role.Id, ClaimType = claimType, ClaimValue = claimValue });
        await db.SaveChangesAsync();
    }

    private async Task WipeClaimsAsync(AppTenantInfo tenant, string roleName, string claimType, IReadOnlyCollection<string> claimValues)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.Roles.SingleAsync(r => r.Name == roleName);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var values = claimValues.ToHashSet(StringComparer.Ordinal);

        // ExecuteDeleteAsync would be cleaner but the in-memory enumeration is
        // negligible for a handful of claims and avoids parameter-list edge cases.
        var toRemove = await db.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == claimType)
            .ToListAsync();

        db.RoleClaims.RemoveRange(toRemove.Where(rc => rc.ClaimValue is not null && values.Contains(rc.ClaimValue)));
        await db.SaveChangesAsync();
    }

    private async Task<HashSet<string>> GetClaimsAsync(AppTenantInfo tenant, string roleName, string claimType)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.Roles.SingleAsync(r => r.Name == roleName);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var claims = await db.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == claimType)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync();

        return claims.ToHashSet(StringComparer.Ordinal);
    }
}
