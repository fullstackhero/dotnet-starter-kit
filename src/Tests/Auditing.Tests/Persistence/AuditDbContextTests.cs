using Microsoft.EntityFrameworkCore;
using FSH.Modules.Auditing.Persistence;
using Shouldly;
using NSubstitute;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.Extensions.Options;
using FSH.Framework.Shared.Persistence;
using Microsoft.Extensions.Hosting;

namespace Auditing.Tests.Persistence;

public class AuditDbContextTests
{
    [Fact]
    public void OnModelCreating_ShouldApplyConfigurations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        var dbOptions = Substitute.For<IOptions<DatabaseOptions>>();
        var env = Substitute.For<IHostEnvironment>();

        // Act
        using var context = new AuditDbContext(tenantAccessor, options, dbOptions, env);
        var model = context.Model;

        // Assert
        // We verified that the AuditRecord entity is present.
        var entityType = model.FindEntityType("FSH.Modules.Auditing.AuditRecord");
        entityType.ShouldNotBeNull();
    }
}
