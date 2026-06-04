using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Coverage for the admin <c>GET /billing/usage</c> query (GetUsageSnapshotsQueryHandler) which the
/// existing UsageMeteringTests never read back: happy path, the tenantId / periodYear / periodMonth
/// filters, the empty-result path, authentication, and the documented cross-tenant admin posture
/// (this endpoint mirrors GetInvoices — it is intentionally a platform-wide list narrowed only by the
/// optional tenantId filter, NOT auto-scoped to the caller's tenant). Snapshots are seeded directly
/// via the DbContext so each test owns a unique (tenant, period) island and never collides on the
/// ux_usage_snapshots_tenant_period_resource unique index.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class UsageSnapshotQueryTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    // Snapshots are unique-indexed on (TenantId, PeriodYear, PeriodMonth, Resource). The factory is
    // shared across the whole collection, so a process-wide counter hands every test a private period
    // window well away from the 2030/2031/2080+ ranges other Billing tests use.
    private static int s_periodCounter;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UsageSnapshotQueryTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task GetUsageSnapshots_Should_Return_Seeded_Snapshots_With_Expected_Shape()
    {
        // Arrange
        var (year, month) = NextPeriod();
        await SeedSnapshotAsync(TestConstants.RootTenantId, year, month, QuotaResource.ApiCalls, used: 150, limit: 100);
        await SeedSnapshotAsync(TestConstants.RootTenantId, year, month, QuotaResource.Users, used: 3, limit: 10);
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var snaps = await GetSnapshotsAsync(client, periodYear: year, periodMonth: month);

        // Assert
        snaps.Count.ShouldBe(2);
        snaps.ShouldAllBe(s => s.TenantId == TestConstants.RootTenantId);
        snaps.ShouldAllBe(s => s.PeriodYear == year && s.PeriodMonth == month);

        var apiCalls = snaps.Single(s => s.Resource == QuotaResource.ApiCalls);
        apiCalls.UsedUnits.ShouldBe(150);
        apiCalls.LimitUnits.ShouldBe(100);
        apiCalls.Overage.ShouldBe(50, "Overage = Used - Limit when Used exceeds Limit");

        var users = snaps.Single(s => s.Resource == QuotaResource.Users);
        users.Overage.ShouldBe(0, "Overage is clamped to zero when usage is under the limit");
    }

    [Fact]
    public async Task GetUsageSnapshots_Should_Order_By_Period_Descending()
    {
        // Arrange — two periods for the same tenant; newer period must sort first.
        var (olderYear, olderMonth) = NextPeriod();
        var (newerYear, newerMonth) = NextPeriod();
        var tenantId = $"snap-order-{Guid.NewGuid().ToString("N")[..8]}";
        await SeedSnapshotAsync(tenantId, olderYear, olderMonth, QuotaResource.ApiCalls, used: 1, limit: 10);
        await SeedSnapshotAsync(tenantId, newerYear, newerMonth, QuotaResource.ApiCalls, used: 2, limit: 10);
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act — filter by tenant so ordering is deterministic regardless of other rows.
        var snaps = await GetSnapshotsAsync(client, tenantId: tenantId);

        // Assert
        snaps.Count.ShouldBe(2);
        var first = snaps[0];
        (first.PeriodYear, first.PeriodMonth).ShouldBe((newerYear, newerMonth),
            "OrderByDescending(Year).ThenByDescending(Month) must surface the newest period first");
    }

    #endregion

    #region Filters

    [Fact]
    public async Task GetUsageSnapshots_Should_Filter_By_TenantId()
    {
        // Arrange — same period, two tenants; the tenantId filter must isolate one of them.
        var (year, month) = NextPeriod();
        var tenantA = $"snap-a-{Guid.NewGuid().ToString("N")[..8]}";
        var tenantB = $"snap-b-{Guid.NewGuid().ToString("N")[..8]}";
        await SeedSnapshotAsync(tenantA, year, month, QuotaResource.ApiCalls, used: 5, limit: 10);
        await SeedSnapshotAsync(tenantB, year, month, QuotaResource.ApiCalls, used: 9, limit: 10);
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var onlyA = await GetSnapshotsAsync(client, tenantId: tenantA);

        // Assert
        onlyA.ShouldNotBeEmpty();
        onlyA.ShouldAllBe(s => s.TenantId == tenantA);
        onlyA.ShouldNotContain(s => s.TenantId == tenantB);
    }

    [Fact]
    public async Task GetUsageSnapshots_Should_Filter_By_Year_And_Month_Independently()
    {
        // Arrange — same tenant, two distinct periods.
        var tenantId = $"snap-period-{Guid.NewGuid().ToString("N")[..8]}";
        var (year1, month1) = NextPeriod();
        var (year2, month2) = NextPeriod();
        await SeedSnapshotAsync(tenantId, year1, month1, QuotaResource.ApiCalls, used: 1, limit: 10);
        await SeedSnapshotAsync(tenantId, year2, month2, QuotaResource.ApiCalls, used: 2, limit: 10);
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act — month filter alone narrows to the matching period for this tenant.
        var byMonth = await GetSnapshotsAsync(client, tenantId: tenantId, periodMonth: month1);

        // Assert
        byMonth.ShouldAllBe(s => s.PeriodMonth == month1);
        byMonth.ShouldContain(s => s.PeriodYear == year1 && s.PeriodMonth == month1);

        // Act — year + month together pin the exact period.
        var byYearMonth = await GetSnapshotsAsync(client, tenantId: tenantId, periodYear: year2, periodMonth: month2);

        // Assert
        byYearMonth.Count.ShouldBe(1);
        byYearMonth[0].PeriodYear.ShouldBe(year2);
        byYearMonth[0].PeriodMonth.ShouldBe(month2);
    }

    [Fact]
    public async Task GetUsageSnapshots_Should_Return_Empty_When_No_Match()
    {
        // Arrange — a tenant id that no test ever seeds.
        var ghostTenant = $"snap-ghost-{Guid.NewGuid():N}";
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var snaps = await GetSnapshotsAsync(client, tenantId: ghostTenant);

        // Assert
        snaps.ShouldBeEmpty("an unmatched tenant filter must yield an empty list, not an error");
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task GetUsageSnapshots_Should_Return401_When_Unauthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        using var response = await client.GetAsync($"{BillingBasePath}/usage");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsageSnapshots_Should_Return200_For_NonAdmin_User_With_Basic_View_Permission()
    {
        // The View Billing permission is IsBasic, so a freshly-registered user (granted RoleConstants.Basic)
        // can read the usage list. This proves the .RequirePermission(Billing.View) gate admits an
        // authenticated non-admin caller rather than only the seeded root admin.
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (email, password) = await RegisterBasicUserAsync(adminClient, "snap-basic");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(email, password);

        // Act
        using var response = await basicClient.GetAsync($"{BillingBasePath}/usage?pageNumber=1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Billing.View is a Basic permission, so an authenticated Basic-role user must be admitted");
    }

    #endregion

    #region Cross-tenant (documented admin posture)

    [Fact]
    public async Task GetUsageSnapshots_Is_PlatformWide_And_Surfaces_OtherTenant_Rows_To_Admin()
    {
        // GetUsageSnapshots mirrors GetInvoices: it is an administrative, platform-wide list that is
        // NOT auto-scoped to the caller's tenant — the only narrowing is the optional tenantId filter.
        // This test pins that documented behavior: a second tenant's snapshot is visible in the
        // unfiltered admin result, and the tenantId filter is what isolates a single tenant. (Asserting
        // hard isolation here would contradict the handler; see findings note on the Basic-View posture.)
        // Arrange
        var (year, month) = NextPeriod();
        var otherTenant = $"snap-cross-{Guid.NewGuid().ToString("N")[..8]}";
        var otherSnapshotId = await SeedSnapshotAsync(otherTenant, year, month, QuotaResource.StorageBytes, used: 500, limit: 100);
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act — unfiltered admin read for this period.
        var all = await GetSnapshotsAsync(client, periodYear: year, periodMonth: month);

        // Assert — the other tenant's row is present in the platform-wide list.
        all.ShouldContain(s => s.Id == otherSnapshotId && s.TenantId == otherTenant,
            "the admin usage list is platform-wide; another tenant's snapshot must be visible");

        // Act — the tenantId filter narrows to exactly that tenant.
        var filtered = await GetSnapshotsAsync(client, tenantId: otherTenant, periodYear: year, periodMonth: month);

        // Assert
        filtered.ShouldAllBe(s => s.TenantId == otherTenant);
        filtered.ShouldContain(s => s.Id == otherSnapshotId);
    }

    #endregion

    #region Helpers

    private static (int Year, int Month) NextPeriod()
    {
        int n = Interlocked.Increment(ref s_periodCounter);
        int yearOffset = (n - 1) / 12;
        int month = ((n - 1) % 12) + 1;
        // 2060-block: distinct from BillingEndpointTests (2080+) and UsageMeteringTests (2030/2031).
        return (2060 + yearOffset, month);
    }

    private static async Task<List<UsageSnapshotDto>> GetSnapshotsAsync(
        HttpClient client, string? tenantId = null, int? periodYear = null, int? periodMonth = null)
    {
        var query = new List<string>();
        if (tenantId is not null)
        {
            query.Add($"tenantId={Uri.EscapeDataString(tenantId)}");
        }
        if (periodYear is not null)
        {
            query.Add($"periodYear={periodYear}");
        }
        if (periodMonth is not null)
        {
            query.Add($"periodMonth={periodMonth}");
        }
        var url = $"{BillingBasePath}/usage";
        if (query.Count > 0)
        {
            url += "?" + string.Join('&', query);
        }

        using var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"GET {url} failed: {(int)response.StatusCode} {response.StatusCode}. Body: {json}");
        }
        return JsonSerializer.Deserialize<List<UsageSnapshotDto>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize snapshot list. Body: {json}");
    }

    private async Task<Guid> SeedSnapshotAsync(
        string tenantId, int year, int month, QuotaResource resource, long used, long limit)
    {
        Guid id = Guid.Empty;
        await SeedDirectAsync(async db =>
        {
            var snap = UsageSnapshot.Capture(tenantId, year, month, resource, used, limit);
            db.UsageSnapshots.Add(snap);
            await db.SaveChangesAsync();
            id = snap.Id;
        });
        return id;
    }

    private async Task<(string Email, string Password)> RegisterBasicUserAsync(HttpClient adminClient, string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        const string password = "Test@1234!";

        using var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = prefix,
            lastName = "Test",
            email,
            userName = $"{prefix}{unique}",
            password,
            confirmPassword = password,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"register failed: {body}");
        var registered = JsonSerializer.Deserialize<RegisterResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("register returned no body");

        // The user is created EmailConfirmed=false; flip it so the user can sign in (loginable user
        // needs EmailConfirmed=true AND IsActive=true; registration already sets IsActive=true).
        await ConfirmEmailAsync(registered.UserId);
        return (email, password);
    }

    private async Task ConfirmEmailAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<FSH.Modules.Identity.Domain.FshUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.ShouldNotBeNull();
        if (!user!.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        }
    }

    private async Task SeedDirectAsync(Func<BillingDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await action(db);
    }

    #endregion
}
