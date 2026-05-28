using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Shared.Multitenancy;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// Verifies subscription-expiry enforcement in the post-auth tenant guard: a tenant past
/// <c>ValidUpto</c> but within the grace window still passes (so requests/logins keep working during
/// dunning), while a tenant past <c>ValidUpto + grace</c> is hard-blocked with 403 — mirroring the
/// deactivation guard. Grace defaults to 7 days.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantExpiryEnforcementTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TenantExpiryEnforcementTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ExpiredTenant_PastGrace_Should_Be_Blocked()
    {
        var tenantId = await CreateTenantAsync();

        // While valid, the guard does not short-circuit — the token handler runs and fails on creds.
        (await TryIssueTokenAsync(tenantId)).ShouldNotBe(HttpStatusCode.Forbidden);

        // Lapse well past the 7-day grace window.
        await SetTenantValidityAsync(tenantId, DateTime.UtcNow.AddDays(-30));

        (await TryIssueTokenAsync(tenantId)).ShouldBe(HttpStatusCode.Forbidden,
            "a tenant past ValidUpto + grace must be blocked before the token handler runs");
    }

    [Fact]
    public async Task LapsedTenant_WithinGrace_Should_Not_Be_Blocked()
    {
        var tenantId = await CreateTenantAsync();

        // One day past expiry is still inside the 7-day grace window.
        await SetTenantValidityAsync(tenantId, DateTime.UtcNow.AddDays(-1));

        (await TryIssueTokenAsync(tenantId)).ShouldNotBe(HttpStatusCode.Forbidden,
            "a lapsed tenant within the grace window must still be allowed through the guard");
    }

    private async Task<string> CreateTenantAsync()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"expiry-{uniqueId}";

        var createResponse = await adminClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Expiry Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail = $"expiry-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = "expiry.issuer"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        return tenantId;
    }

    // Writes ValidUpto straight to the EF tenant store and pushes the change into the distributed
    // cache store so the guard sees it on the next request (the cache otherwise serves a 60-min copy).
    private async Task SetTenantValidityAsync(string tenantId, DateTime validUpto)
    {
        using var scope = _factory.Services.CreateScope();
        var stores = scope.ServiceProvider.GetServices<IMultiTenantStore<AppTenantInfo>>().ToList();

        var efStore = stores.First(s => s.GetType().Name.StartsWith("EFCoreStore", StringComparison.Ordinal));
        var tenant = await efStore.GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        tenant!.SetValidity(DateTime.SpecifyKind(validUpto, DateTimeKind.Utc));
        await efStore.UpdateAsync(tenant);

        var cacheStore = stores.FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<AppTenantInfo>));
        if (cacheStore is not null)
        {
            await cacheStore.UpdateAsync(tenant);
        }
    }

    private async Task<HttpStatusCode> TryIssueTokenAsync(string tenantId)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", tenantId);
        request.Content = JsonContent.Create(new { email = "nobody@example.com", password = "Wrong-Password-1!" });
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
