using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Identity.Tests.Data;

public class IdentityDbContextModelTests
{
    private static IdentityDbContext CreateContext()
    {
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo()));

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=arch;Database=arch;Username=arch;Password=arch")
            .Options;

        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });

        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");

        return new IdentityDbContext(accessor, options, settings, environment);
    }

    // User.Locale holds a BCP-47 tag, which is short and bounded. Unbounded `text`
    // invites arbitrary input at the storage layer for a value the write boundary
    // already restricts to SupportedCultures.Tags. 10 covers the longest form the
    // platform could offer (language-script-region, e.g. zh-Hant-TW).
    [Fact]
    public void User_Locale_Is_Bounded_To_A_Bcp47_Tag_Length()
    {
        using var context = CreateContext();

        var locale = context.Model.FindEntityType(typeof(FshUser))?.FindProperty(nameof(FshUser.Locale));

        locale.ShouldNotBeNull();
        locale!.GetMaxLength().ShouldBe(
            10,
            "an unbounded locale column accepts arbitrary input for a value that is always a short BCP-47 tag");
    }
}
