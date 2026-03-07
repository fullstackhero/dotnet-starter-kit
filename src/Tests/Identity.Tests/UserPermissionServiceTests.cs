using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Identity.Tests.Services;

public class UserPermissionServiceTests
{
    [Fact]
    public async Task InvalidatePermissionCacheAsync_ShouldIncludeTenantIdInCacheKey()
    {
        // Arrange
        var userStore = Substitute.For<IUserStore<FshUser>>();
        var userManager = Substitute.For<UserManager<FshUser>>(userStore, null, null, null, null, null, null, null, null);
        
        var roleStore = Substitute.For<IRoleStore<FshRole>>();
        var roleManager = Substitute.For<RoleManager<FshRole>>(roleStore, null, null, null, null);
        
        var cache = Substitute.For<ICacheService>();
        var tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        
        var tenantInfo = new AppTenantInfo("test-tenant", "Test", null, "admin", null);
        tenantAccessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(tenantInfo));

        // Using reflection to instantiate internal service, or just let internal visible to Tests project work.
        // Assuming InternalsVisibleTo is configured.
        var service = new UserPermissionService(userManager, roleManager, null!, cache, tenantAccessor);

        // Act
        await service.InvalidatePermissionCacheAsync("user-1", CancellationToken.None);

        // Assert
        await cache.Received(1).RemoveItemAsync("perm:test-tenant:user-1", Arg.Any<CancellationToken>());
    }
}
