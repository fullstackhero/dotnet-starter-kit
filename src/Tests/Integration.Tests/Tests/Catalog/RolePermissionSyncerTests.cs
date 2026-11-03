using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

    // Target for the throwaway-role tests; never registered, so it cannot collide with a real grant.
    private static readonly FshPermission ProbePermission = new("Sync probe", "View", "SyncProbe");

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

        // Checked after two passes: a sync that adds and removes against different targets flips the claims on each run.
        for (int pass = 1; pass <= 2; pass++)
        {
            await RunSyncerAsync(rootTenant);

            var admin = await GetClaimsAsync(rootTenant, "Admin", ClaimConstants.Permission);
            var missing = PermissionConstants.Root.Select(p => p.Name).Where(p => !admin.Contains(p)).ToList();
            missing.ShouldBeEmpty(
                $"Sync pass {pass} stripped {missing.Count} root permission(s) from the root tenant's Admin: " +
                $"[{string.Join(", ", missing)}]");
        }
    }

    // The guards below run SyncRoleAsync against a throwaway role with an explicit target, so they
    // never touch Basic/Admin, which other tests and the startup sync share.

    [Fact]
    public async Task SyncRoleAsync_Should_Add_But_Not_Remove_When_RemoveRevoked_Is_Off()
    {
        var rootTenant = await GetRootTenantAsync();
        string roleName = await CreateProbeRoleAsync(rootTenant, RetiredPermission);

        try
        {
            int changed = await RunSyncRoleAsync(rootTenant, roleName, [ProbePermission], removeRevoked: false);

            changed.ShouldBe(1, "Only the missing permission should be written.");
            var claims = await GetClaimsAsync(rootTenant, roleName, ClaimConstants.Permission);
            claims.ShouldBe([RetiredPermission, ProbePermission.Name], ignoreOrder: true,
                "With RemoveRevoked off the sync must add the missing permission and keep the revoked one.");
        }
        finally
        {
            await DeleteRoleAsync(rootTenant, roleName);
        }
    }

    [Fact]
    public async Task SyncRoleAsync_Should_Not_Remove_Anything_When_The_Target_Is_Empty()
    {
        var rootTenant = await GetRootTenantAsync();
        string roleName = await CreateProbeRoleAsync(rootTenant, RetiredPermission);

        try
        {
            int changed = await RunSyncRoleAsync(rootTenant, roleName, [], removeRevoked: true);

            changed.ShouldBe(0);
            var claims = await GetClaimsAsync(rootTenant, roleName, ClaimConstants.Permission);
            claims.ShouldContain(RetiredPermission,
                "An empty target means a missing catalog; the sync must not strip the role.");
        }
        finally
        {
            await DeleteRoleAsync(rootTenant, roleName);
        }
    }

    [Fact]
    public async Task SyncRoleAsync_Should_Treat_A_Lost_Delete_Race_As_Done_And_Keep_Syncing()
    {
        var rootTenant = await GetRootTenantAsync();
        string racedRole = await CreateProbeRoleAsync(rootTenant, RetiredPermission);
        string nextRole = await CreateProbeRoleAsync(rootTenant, RetiredPermission);

        try
        {
            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            // Stands in for a second instance starting at the same time: just before this instance's
            // first save, it deletes the same row from its own connection.
            var otherInstance = new DeleteBeforeFirstSaveInterceptor(
                () => DeleteClaimAsync(rootTenant, racedRole, RetiredPermission));
            var options = new DbContextOptionsBuilder<IdentityDbContext>(
                    sp.GetRequiredService<DbContextOptions<IdentityDbContext>>())
                .AddInterceptors(otherInstance)
                .Options;
            await using var db = new IdentityDbContext(
                sp.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>(),
                options,
                sp.GetRequiredService<IOptions<DatabaseOptions>>(),
                sp.GetRequiredService<IHostEnvironment>());
            var syncer = ActivatorUtilities.CreateInstance<RolePermissionSyncer>(
                sp, db, Options.Create(new PermissionSyncOptions { RemoveRevoked = true }));

            int racedChanged = await syncer.SyncRoleAsync(racedRole, [ProbePermission], CancellationToken.None);
            int nextChanged = await syncer.SyncRoleAsync(nextRole, [ProbePermission], CancellationToken.None);

            otherInstance.Fired.ShouldBeTrue("Pre-condition: the competing delete must have run.");
            racedChanged.ShouldBe(0, "A lost race is reported as no change from this instance.");
            nextChanged.ShouldBe(2, "The next role in the same scope must still sync (one removal, one addition).");
            var next = await GetClaimsAsync(rootTenant, nextRole, ClaimConstants.Permission);
            next.ShouldBe([ProbePermission.Name], ignoreOrder: true);
        }
        finally
        {
            await DeleteRoleAsync(rootTenant, racedRole);
            await DeleteRoleAsync(rootTenant, nextRole);
        }
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private async Task<int> RunSyncRoleAsync(AppTenantInfo tenant, string roleName, IReadOnlyList<FshPermission> target, bool removeRevoked)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var syncer = ActivatorUtilities.CreateInstance<RolePermissionSyncer>(
            scope.ServiceProvider, Options.Create(new PermissionSyncOptions { RemoveRevoked = removeRevoked }));
        return await syncer.SyncRoleAsync(roleName, target, CancellationToken.None);
    }

    private async Task<string> CreateProbeRoleAsync(AppTenantInfo tenant, string permission)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        string roleName = $"sync-probe-{Guid.NewGuid():N}";
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        (await roleManager.CreateAsync(new FshRole(roleName))).Succeeded.ShouldBeTrue();

        await SeedClaimAsync(tenant, roleName, ClaimConstants.Permission, permission);
        return roleName;
    }

    private async Task DeleteRoleAsync(AppTenantInfo tenant, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        // Role claims cascade with the role.
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is not null)
        {
            (await roleManager.DeleteAsync(role)).Succeeded.ShouldBeTrue();
        }
    }

    private async Task DeleteClaimAsync(AppTenantInfo tenant, string roleName, string claimValue)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var role = await roleManager.Roles.SingleAsync(r => r.Name == roleName);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        int deleted = await db.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimValue == claimValue)
            .ExecuteDeleteAsync();
        deleted.ShouldBe(1);
    }

    private sealed class DeleteBeforeFirstSaveInterceptor(Func<Task> competingDelete) : SaveChangesInterceptor
    {
        public bool Fired { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Fired)
            {
                Fired = true;
                await competingDelete();
            }

            return result;
        }
    }

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
