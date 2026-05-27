using System.Net.Http.Json;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Contracts.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// Regression coverage for <c>GET /api/v1/identity/permissions/catalog</c>. The endpoint is
/// the SPA's source of truth for which permissions exist in the host — a stale or missing
/// catalog is the root cause of the "N enabled / M total" drift bug the editor used to
/// show before the endpoint was added.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PermissionCatalogTests
{
    private const string CatalogPath = TestConstants.IdentityBasePath + "/permissions/catalog";
    private readonly AuthHelper _auth;
    private readonly FshWebApplicationFactory _factory;

    public PermissionCatalogTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_Should_ReturnOk_When_CallerHasRolesView()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync(CatalogPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Catalog_Should_ReturnEveryRegisteredAdminPermission_When_TenantIsRoot()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var catalog = await client.GetFromJsonAsync<List<PermissionCatalogEntryDto>>(CatalogPath);

        catalog.ShouldNotBeNull();

        // Root tenant sees the full Admin set plus the platform Root set. This is the
        // same union RolePermissionSyncer applies, so if the two diverge the editor and
        // the syncer would disagree on what's grantable.
        var expected = PermissionConstants.Admin
            .Concat(PermissionConstants.Root)
            .DistinctBy(p => p.Name)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        var actual = catalog.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        actual.ShouldBe(expected, ignoreOrder: true);

        // The drift bug we're guarding against: prior to the endpoint, the SPA had only
        // ~25 Identity permissions while the server held 70+. A canary count check catches
        // a regression that quietly returns just one module's slice.
        catalog.Count.ShouldBeGreaterThan(25);
    }

    [Fact]
    public async Task Catalog_Should_ShapeEachEntry_With_AllRequiredFields()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var catalog = await client.GetFromJsonAsync<List<PermissionCatalogEntryDto>>(CatalogPath);

        catalog.ShouldNotBeNull();
        catalog.ShouldNotBeEmpty();
        foreach (var entry in catalog)
        {
            entry.Name.ShouldNotBeNullOrWhiteSpace();
            entry.Description.ShouldNotBeNullOrWhiteSpace();
            entry.Resource.ShouldNotBeNullOrWhiteSpace();
            entry.Action.ShouldNotBeNullOrWhiteSpace();
            entry.Name.ShouldBe(
                $"Permissions.{entry.Resource}.{entry.Action}",
                "DTO Name must equal `Permissions.{Resource}.{Action}` so the SPA's grouping logic stays correct.");
        }
    }

    [Fact]
    public async Task Catalog_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync(CatalogPath);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
