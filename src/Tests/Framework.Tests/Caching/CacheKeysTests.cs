using FSH.Framework.Caching;

namespace Framework.Tests.Caching;

public sealed class CacheKeysTests
{
    #region Keys

    [Fact]
    public void UserPermissions_Should_ScopeByUserId_When_Built()
    {
        CacheKeys.UserPermissions("u-123").ShouldBe("perm:u:u-123");
    }

    [Fact]
    public void TenantTheme_Should_ScopeByTenantId_When_Built()
    {
        CacheKeys.TenantTheme("t-1").ShouldBe("theme:t:t-1");
    }

    [Fact]
    public void DefaultTheme_Should_BeStableConstant()
    {
        CacheKeys.DefaultTheme.ShouldBe("theme:default");
    }

    [Fact]
    public void IdempotencyEntry_Should_ScopeByTenantAndKey_When_Built()
    {
        CacheKeys.IdempotencyEntry("t-1", "abc").ShouldBe("idem:t:t-1:abc");
    }

    [Fact]
    public void ImpersonationGrantStatus_Should_IndexByJti_When_Built()
    {
        CacheKeys.ImpersonationGrantStatus("jti-9").ShouldBe("impgrant:jti-9");
    }

    #endregion

    #region Tags

    [Fact]
    public void Tags_Should_ExposeStableConstants()
    {
        CacheKeys.Tags.Permissions.ShouldBe("permissions");
        CacheKeys.Tags.Themes.ShouldBe("themes");
        CacheKeys.Tags.Idempotency.ShouldBe("idempotency");
    }

    [Fact]
    public void Tags_Should_ScopeTenantAndUser_When_Built()
    {
        CacheKeys.Tags.Tenant("t-1").ShouldBe("tenant:t-1");
        CacheKeys.Tags.User("u-1").ShouldBe("user:u-1");
    }

    #endregion
}
