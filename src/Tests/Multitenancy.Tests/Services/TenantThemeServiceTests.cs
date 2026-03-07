using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Domain;
using FSH.Modules.Multitenancy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Multitenancy.Tests.Services;

public class TenantThemeServiceTests
{
    [Fact]
    public async Task ResetThemeAsync_ShouldInvalidateDefaultThemeCache()
    {
        // Arrange
        var cache = Substitute.For<ICacheService>();
        
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        var tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenantAccessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("test-tenant", "Test", null, "test@test.com", null)));
        
        using var dbContext = new TenantDbContext(options);
        // Seed a theme
        dbContext.TenantThemes.Add(TenantTheme.Create("test-tenant"));
        await dbContext.SaveChangesAsync();

        var storageService = Substitute.For<IStorageService>();
        var logger = Substitute.For<ILogger<TenantThemeService>>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(Guid.NewGuid());

        var service = new TenantThemeService(cache, dbContext, tenantAccessor, storageService, logger, currentUser);

        // Act
        await service.ResetThemeAsync("test-tenant", CancellationToken.None);

        // Assert
        // CACHE-2: Assert that DefaultThemeCacheKey ("theme:default") was invalidated
        await cache.Received(1).RemoveItemAsync("theme:default", Arg.Any<CancellationToken>());
    }
}
