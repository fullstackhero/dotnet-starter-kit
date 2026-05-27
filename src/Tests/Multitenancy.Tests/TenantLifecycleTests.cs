namespace Multitenancy.Tests;

//public class TenantLifecycleTests : IClassFixture<WebApplicationFactory<Program>>
//{
//    private readonly WebApplicationFactory<Program> _factory;

//    public TenantLifecycleTests(WebApplicationFactory<Program> factory)
//    {
//        _factory = factory;
//    }

//    [Fact(Skip = "Requires fully configured authentication and test tenant environment.")]
//    public async Task ChangeActivation_Should_ReturnLifecycleResult()
//    {
//        var client = _factory.CreateClient();

//        // This is a placeholder test structure to validate wiring once auth and tenants are seeded for tests.
//        var tenantId = "root";

//        var response = await client.PostAsJsonAsync(
//            $"/api/v1/tenants/{tenantId}/activation",
//            new { tenantId, isActive = true });

//        response.StatusCode.ShouldBe(HttpStatusCode.OK);

//        var result = await response.Content.ReadFromJsonAsync<TenantLifecycleResultDto>();
//        result.ShouldNotBeNull();
//        result.TenantId.ShouldBe(tenantId);
//    }
//}