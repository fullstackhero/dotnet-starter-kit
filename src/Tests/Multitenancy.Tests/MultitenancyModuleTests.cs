using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Multitenancy.Tests;

public class MultitenancyModuleTests
{
    [Fact]
    public void ConfigureServices_ShouldRegisterITenantServiceExactlyOnce()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var module = new MultitenancyModule();

        // Act
        module.ConfigureServices(builder);

        // Assert
        var tenantServiceDescriptors = builder.Services
            .Where(sd => sd.ServiceType == typeof(ITenantService))
            .ToList();

        tenantServiceDescriptors.Count.ShouldBe(1);
    }
}
