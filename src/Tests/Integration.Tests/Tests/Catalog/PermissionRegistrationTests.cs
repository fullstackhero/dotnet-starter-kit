using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts.Authorization;
using FSH.Modules.Billing.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Multitenancy.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.Authorization;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Catalog;

/// <summary>
/// Regression coverage for the per-module permission registry. Each module owns its
/// permissions in its Contracts project and registers them via <see cref="PermissionConstants.Register"/>
/// during <c>ConfigureServices</c>. These tests catch two classes of bug:
///   1. A new permission added to a module is not registered (registry drift).
///   2. The Admin role's claim seeding is not propagating new permissions to existing tenants.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PermissionRegistrationTests
{
    private readonly AuthHelper _auth;

    public PermissionRegistrationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public void Registry_Should_Contain_All_Module_Permissions()
    {
        var registered = PermissionConstants.All.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        // Each module's static `All` list is the single source of truth.
        AssertAllRegistered(registered, IdentityPermissions.All.Select(p => p.Name), nameof(IdentityPermissions));
        AssertAllRegistered(registered, MultitenancyPermissions.All.Select(p => p.Name), nameof(MultitenancyPermissions));
        AssertAllRegistered(registered, AuditingPermissions.All.Select(p => p.Name), nameof(AuditingPermissions));
        AssertAllRegistered(registered, BillingPermissions.All.Select(p => p.Name), nameof(BillingPermissions));
        AssertAllRegistered(registered, CatalogPermissions.All.Select(p => p.Name), nameof(CatalogPermissions));
        AssertAllRegistered(registered, TicketsPermissions.All.Select(p => p.Name), nameof(TicketsPermissions));
        AssertAllRegistered(registered, SystemPermissions.All.Select(p => p.Name), nameof(SystemPermissions));
    }

    [Fact]
    public async Task RootAdmin_Should_Have_All_Catalog_Permissions_After_Seed()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/permissions");
        var permissions = await response.DeserializeAsync<string[]>();

        var expected = new[]
        {
            CatalogPermissions.Brands.View,
            CatalogPermissions.Brands.Create,
            CatalogPermissions.Brands.Update,
            CatalogPermissions.Brands.Delete,
            CatalogPermissions.Brands.Restore,
            CatalogPermissions.Categories.View,
            CatalogPermissions.Categories.Create,
            CatalogPermissions.Categories.Update,
            CatalogPermissions.Categories.Delete,
            CatalogPermissions.Categories.Restore,
            CatalogPermissions.Products.View,
            CatalogPermissions.Products.Create,
            CatalogPermissions.Products.Update,
            CatalogPermissions.Products.Delete,
            CatalogPermissions.Products.Restore,
            CatalogPermissions.Products.AdjustStock,
            TicketsPermissions.Tickets.View,
            TicketsPermissions.Tickets.Create,
            TicketsPermissions.Tickets.Restore,
            TicketsPermissions.Tickets.Assign,
            TicketsPermissions.Tickets.Resolve,
            TicketsPermissions.Tickets.Reopen,
            TicketsPermissions.Tickets.Comment,
        };

        var permSet = permissions.ToHashSet(StringComparer.Ordinal);
        foreach (var perm in expected)
        {
            permSet.ShouldContain(perm, $"Admin role missing expected catalog permission '{perm}'");
        }
    }

    [Fact]
    public async Task RootAdmin_Should_Have_Cross_Module_Permissions()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/permissions");
        var permissions = await response.DeserializeAsync<string[]>();
        var permSet = permissions.ToHashSet(StringComparer.Ordinal);

        // Spot-check one perm from each module — covers the "new module added but seeding not run" case.
        permSet.ShouldContain(IdentityPermissions.Users.View);
        permSet.ShouldContain(BillingPermissions.View);
        permSet.ShouldContain(AuditingPermissions.AuditTrails.View);
        permSet.ShouldContain(SystemPermissions.Dashboard.View);

        // Tenants permissions are root-only — admin@root.com on the root tenant gets them.
        permSet.ShouldContain(MultitenancyPermissions.Tenants.View);
    }

    private static void AssertAllRegistered(HashSet<string> registered, IEnumerable<string> expected, string moduleName)
    {
        var missing = expected.Where(name => !registered.Contains(name)).ToList();
        missing.ShouldBeEmpty(
            $"{moduleName}: {missing.Count} permission(s) not in PermissionConstants registry — " +
            $"missing: [{string.Join(", ", missing)}]. " +
            $"Make sure the module's ConfigureServices calls PermissionConstants.Register({moduleName}.All).");
    }
}
