using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using FSH.Tests.Integration.Infrastructure;
using FSH.Tests.Shared.Infrastructure;
using Shouldly;
using Xunit;

namespace FSH.Tests.Integration.Tenancy;

public class Tenant_ShouldBeRetrieved_WhenExistsInDatabase_ViaMediator : BaseIntegrationTest
{
    public Tenant_ShouldBeRetrieved_WhenExistsInDatabase_ViaMediator(CustomWebApplicationFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task GetTenantStatus_ShouldReturnStatus_WhenTenantExists()
    {
        // Act: Send directly to Mediator, bypassing HTTP
        var query = new GetTenantStatusQuery("root");
        
        // Note: This will fail until the Mediator is properly registered with the Testcontainers DB
        // Assert: Ensure it throws or returns (Red phase)
        var result = await Mediator.Send(query);
        result.ShouldNotBeNull();
    }
}
