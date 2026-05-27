using FSH.Framework.Caching;

namespace Caching.Tests;

/// <summary>
/// Verifies the cache key and tag conventions stay stable — keys are persisted to Redis,
/// so unintentional format changes would silently invalidate every running instance's entries.
/// </summary>
public sealed class CacheKeysTests
{
    [Fact]
    public void UserPermissions_Should_UseStablePrefix()
    {
        CacheKeys.UserPermissions("abc-123").ShouldBe("perm:u:abc-123");
    }

    [Fact]
    public void TenantTheme_Should_UseStablePrefix()
    {
        CacheKeys.TenantTheme("root").ShouldBe("theme:t:root");
    }

    [Fact]
    public void DefaultTheme_Should_BeConstant()
    {
        CacheKeys.DefaultTheme.ShouldBe("theme:default");
    }

    [Fact]
    public void IdempotencyEntry_Should_ScopeByTenant()
    {
        CacheKeys.IdempotencyEntry("t1", "req-42").ShouldBe("idem:t:t1:req-42");
    }

    [Fact]
    public void Tags_Tenant_Should_UseTenantPrefix()
    {
        CacheKeys.Tags.Tenant("t1").ShouldBe("tenant:t1");
    }

    [Fact]
    public void Tags_User_Should_UseUserPrefix()
    {
        CacheKeys.Tags.User("u1").ShouldBe("user:u1");
    }

    [Fact]
    public void Tags_Constants_Should_BeStable()
    {
        CacheKeys.Tags.Permissions.ShouldBe("permissions");
        CacheKeys.Tags.Themes.ShouldBe("themes");
        CacheKeys.Tags.Idempotency.ShouldBe("idempotency");
    }
}
