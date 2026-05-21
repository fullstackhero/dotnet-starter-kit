using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Multitenancy;

[Collection(FshCollectionDefinition.Name)]
public sealed class TenantCreationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TenantCreationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CreateTenant_Should_Return201WithId_When_DataIsValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"t-{uniqueId}";

        var response = await client.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Test Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail = $"admin-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = "test.issuer"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.DeserializeAsync<CreateTenantResult>();
        result.Id.ShouldBe(tenantId);
    }

    [Fact]
    public async Task CreateTenant_Should_Reject_When_IdAlreadyExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            id = $"dup-{uniqueId}",
            name = $"Dup Tenant {uniqueId}",
            connectionString = (string?)null,
            adminEmail = $"dupadmin-{uniqueId}@tenant.com",
            adminPassword = TestConstants.DefaultPassword,
            issuer = "dup.issuer"
        };

        var firstResponse = await client.PostAsJsonAsync(TestConstants.TenantsBasePath, payload);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync(TestConstants.TenantsBasePath, payload);

        secondResponse.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateTenant_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = "noauth",
            name = "No Auth Tenant",
            connectionString = (string?)null,
            adminEmail = "noauth@tenant.com",
            issuer = "noauth.issuer"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenants_Should_ReturnOk_When_AuthenticatedAsRootAdmin()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.TenantsBasePath}?pageNumber=1&pageSize=50");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTenantStatus_Should_ReturnOk_When_TenantExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.GetAsync($"{TestConstants.TenantsBasePath}/{TestConstants.RootTenantId}/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
