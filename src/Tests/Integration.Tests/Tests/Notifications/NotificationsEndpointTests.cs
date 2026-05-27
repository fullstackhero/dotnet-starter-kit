using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Notifications;

/// <summary>
/// End-to-end coverage for the four Notifications inbox endpoints. The integration-event ingress
/// path (Chat @-mention → Notification row + SignalR push) is covered by MentionAndNotificationTests
/// — this file seeds rows directly via the DbContext and focuses on the read/mark-read surface so
/// edge cases (cross-user MarkRead, idempotency, unread filter, paging) are explicit.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class NotificationsEndpointTests
{
    private const string NotificationsBasePath = "/api/v1/notifications";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public NotificationsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task ListNotifications_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync($"{NotificationsBasePath}/");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnreadCount_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.GetAsync($"{NotificationsBasePath}/unread-count");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkNotificationRead_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsync(
            $"{NotificationsBasePath}/{Guid.NewGuid()}/read", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsync($"{NotificationsBasePath}/read-all", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── empty inbox baseline ────────────────────────────────────────

    [Fact]
    public async Task ListNotifications_Should_Return_Empty_For_FreshUser()
    {
        var user = await RegisterFreshUserAsync();
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        var inbox = await ReadInboxAsync(client);

        // The shared factory may have unrelated notifications for other users, but a freshly
        // registered user's own inbox must be empty.
        inbox.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetUnreadCount_Should_Return_Zero_For_FreshUser()
    {
        var user = await RegisterFreshUserAsync();
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var response = await client.GetAsync($"{NotificationsBasePath}/unread-count");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var count = await response.DeserializeAsync<int>();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_Should_Return_Zero_Updated_For_FreshUser()
    {
        var user = await RegisterFreshUserAsync();
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var response = await client.PostAsync($"{NotificationsBasePath}/read-all", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.DeserializeAsync<UpdatedCountPayload>();
        payload.Updated.ShouldBe(0);
    }

    // ─── single-notification lifecycle ───────────────────────────────

    [Fact]
    public async Task ListNotifications_Should_Return_SeededRow_With_Expected_Shape()
    {
        var user = await RegisterFreshUserAsync();
        var seeded = await SeedNotificationsAsync(user.Id, count: 1, type: "test.shape");
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        var inbox = await ReadInboxAsync(client);

        inbox.Count.ShouldBe(1);
        var row = inbox[0];
        row.Id.ShouldBe(seeded[0]);
        row.Type.ShouldBe("test.shape");
        row.Title.ShouldNotBeNullOrWhiteSpace();
        row.ReadAtUtc.ShouldBeNull("seeded rows must start unread");
        row.CreatedAtUtc.ShouldNotBe(default);
        row.MetadataJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetUnreadCount_Should_Match_SeededRow_Count()
    {
        var user = await RegisterFreshUserAsync();
        await SeedNotificationsAsync(user.Id, count: 3);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var response = await client.GetAsync($"{NotificationsBasePath}/unread-count");
        var count = await response.DeserializeAsync<int>();

        count.ShouldBe(3);
    }

    [Fact]
    public async Task MarkNotificationRead_Should_Set_ReadAtUtc_And_Decrement_Count()
    {
        var user = await RegisterFreshUserAsync();
        var seeded = await SeedNotificationsAsync(user.Id, count: 2);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var markResponse = await client.PostAsync(
            $"{NotificationsBasePath}/{seeded[0]}/read", content: null);
        markResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var inbox = await ReadInboxAsync(client);
        var markedRow = inbox.Single(n => n.Id == seeded[0]);
        markedRow.ReadAtUtc.ShouldNotBeNull("MarkRead must stamp ReadAtUtc");

        var unread = await GetUnreadCountAsync(client);
        unread.ShouldBe(1, "Only the second seeded row should remain unread");
    }

    [Fact]
    public async Task MarkNotificationRead_Should_Be_Idempotent_On_Second_Call()
    {
        // The domain `MarkRead()` uses `ReadAtUtc ??= DateTime.UtcNow;` — calling it again must not
        // mutate the timestamp. This guards against the "MarkRead is a setter" misreading.
        var user = await RegisterFreshUserAsync();
        var seeded = await SeedNotificationsAsync(user.Id, count: 1);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var first = await client.PostAsync($"{NotificationsBasePath}/{seeded[0]}/read", content: null);
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var inboxAfterFirst = await ReadInboxAsync(client);
        var stampedAt = inboxAfterFirst.Single(n => n.Id == seeded[0]).ReadAtUtc;
        stampedAt.ShouldNotBeNull();

        // Wait a beat so any spurious re-stamp would produce a clearly different timestamp.
        await Task.Delay(20);

        using var second = await client.PostAsync($"{NotificationsBasePath}/{seeded[0]}/read", content: null);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var inboxAfterSecond = await ReadInboxAsync(client);
        var restamped = inboxAfterSecond.Single(n => n.Id == seeded[0]).ReadAtUtc;
        restamped.ShouldBe(stampedAt, "Second MarkRead must not overwrite ReadAtUtc");
    }

    [Fact]
    public async Task MarkNotificationRead_Should_Return404_When_Notification_Belongs_To_Another_User()
    {
        // The handler filters by (Id AND UserId == currentUser) so attempts to mark someone else's
        // row come back 404 — we deliberately don't leak whether the id exists.
        var alice = await RegisterFreshUserAsync("alice");
        var bob = await RegisterFreshUserAsync("bob");
        var bobsNotification = (await SeedNotificationsAsync(bob.Id, count: 1))[0];
        using var aliceClient = await _auth.CreateAuthenticatedClientAsync(alice.Email, alice.Password);

        using var response = await aliceClient.PostAsync(
            $"{NotificationsBasePath}/{bobsNotification}/read", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Bob's row must remain unread — Alice's request mustn't be able to mark it as a side effect.
        using var bobClient = await _auth.CreateAuthenticatedClientAsync(bob.Email, bob.Password);
        var bobInbox = await ReadInboxAsync(bobClient);
        bobInbox.Single(n => n.Id == bobsNotification).ReadAtUtc.ShouldBeNull(
            "Alice's failed MarkRead must not mutate Bob's notification");
    }

    [Fact]
    public async Task MarkNotificationRead_Should_Return404_When_Notification_DoesNotExist()
    {
        var user = await RegisterFreshUserAsync();
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var response = await client.PostAsync(
            $"{NotificationsBasePath}/{Guid.NewGuid()}/read", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── bulk read ───────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllNotificationsRead_Should_Mark_All_Unread_And_Return_UpdatedCount()
    {
        var user = await RegisterFreshUserAsync();
        await SeedNotificationsAsync(user.Id, count: 4);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var response = await client.PostAsync($"{NotificationsBasePath}/read-all", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.DeserializeAsync<UpdatedCountPayload>();
        payload.Updated.ShouldBe(4);

        (await GetUnreadCountAsync(client)).ShouldBe(0);

        var inbox = await ReadInboxAsync(client);
        inbox.ShouldAllBe(n => n.ReadAtUtc != null,
            "Every notification must be marked read after read-all");
    }

    [Fact]
    public async Task MarkAllNotificationsRead_Should_Return_Zero_When_All_Already_Read()
    {
        var user = await RegisterFreshUserAsync();
        await SeedNotificationsAsync(user.Id, count: 2);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var firstCall = await client.PostAsync($"{NotificationsBasePath}/read-all", content: null);
        (await firstCall.DeserializeAsync<UpdatedCountPayload>()).Updated.ShouldBe(2);

        using var secondCall = await client.PostAsync($"{NotificationsBasePath}/read-all", content: null);
        secondCall.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await secondCall.DeserializeAsync<UpdatedCountPayload>();
        second.Updated.ShouldBe(0, "Second read-all must update nothing");
    }

    [Fact]
    public async Task MarkAllNotificationsRead_Should_Only_Touch_Callers_Rows()
    {
        // Cross-user safety: Alice's read-all must leave Bob's notifications alone.
        var alice = await RegisterFreshUserAsync("alice");
        var bob = await RegisterFreshUserAsync("bob");
        await SeedNotificationsAsync(alice.Id, count: 2);
        await SeedNotificationsAsync(bob.Id, count: 3);
        using var aliceClient = await _auth.CreateAuthenticatedClientAsync(alice.Email, alice.Password);

        using var response = await aliceClient.PostAsync($"{NotificationsBasePath}/read-all", content: null);
        var payload = await response.DeserializeAsync<UpdatedCountPayload>();
        payload.Updated.ShouldBe(2, "read-all must scope strictly to the caller");

        using var bobClient = await _auth.CreateAuthenticatedClientAsync(bob.Email, bob.Password);
        (await GetUnreadCountAsync(bobClient)).ShouldBe(3, "Bob's inbox must be untouched by Alice's read-all");
    }

    // ─── filter / order / page ───────────────────────────────────────

    [Fact]
    public async Task ListNotifications_With_UnreadOnly_Should_Exclude_Read_Rows()
    {
        var user = await RegisterFreshUserAsync();
        var seeded = await SeedNotificationsAsync(user.Id, count: 3);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Mark the first as read; the other two stay unread.
        await client.PostAsync($"{NotificationsBasePath}/{seeded[0]}/read", content: null);

        using var unreadOnlyResp = await client.GetAsync($"{NotificationsBasePath}/?unreadOnly=true");
        var unreadList = await unreadOnlyResp.DeserializeAsync<IReadOnlyList<NotificationDto>>();
        unreadList.Count.ShouldBe(2);
        unreadList.ShouldAllBe(n => n.ReadAtUtc == null);
        unreadList.ShouldNotContain(n => n.Id == seeded[0]);

        using var allResp = await client.GetAsync($"{NotificationsBasePath}/");
        var allList = await allResp.DeserializeAsync<IReadOnlyList<NotificationDto>>();
        allList.Count.ShouldBe(3, "Default list (no unreadOnly) must include both read and unread");
    }

    [Fact]
    public async Task ListNotifications_Should_Return_NewestFirst()
    {
        var user = await RegisterFreshUserAsync();
        var seeded = await SeedNotificationsAsync(user.Id, count: 3, spaceCreationByMs: 50);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        var inbox = await ReadInboxAsync(client);
        inbox.Count.ShouldBe(3);

        // SeedNotificationsAsync stamps CreatedAtUtc in ascending order, so the newest is the last
        // seeded id. The list should reverse that order.
        var expectedOrder = seeded.AsEnumerable().Reverse().ToList();
        inbox.Select(n => n.Id).ShouldBe(expectedOrder);
    }

    [Fact]
    public async Task ListNotifications_Should_Respect_Page_And_PageSize()
    {
        var user = await RegisterFreshUserAsync();
        var seeded = await SeedNotificationsAsync(user.Id, count: 4, spaceCreationByMs: 50);
        using var client = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var page1Resp = await client.GetAsync($"{NotificationsBasePath}/?page=1&pageSize=2");
        var page1 = await page1Resp.DeserializeAsync<IReadOnlyList<NotificationDto>>();
        page1.Count.ShouldBe(2, "Page 1 with pageSize=2 must return the first 2 rows");
        page1[0].Id.ShouldBe(seeded[3], "Newest first across pages");
        page1[1].Id.ShouldBe(seeded[2]);

        using var page2Resp = await client.GetAsync($"{NotificationsBasePath}/?page=2&pageSize=2");
        var page2 = await page2Resp.DeserializeAsync<IReadOnlyList<NotificationDto>>();
        page2.Count.ShouldBe(2);
        page2[0].Id.ShouldBe(seeded[1]);
        page2[1].Id.ShouldBe(seeded[0]);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private sealed record UpdatedCountPayload(int Updated);

    private sealed record UserCredentials(string Id, string Email, string Password);

    private async Task<UserCredentials> RegisterFreshUserAsync(string prefix = "user")
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        var userName = $"{prefix}{unique}";
        const string password = "Test@1234!";

        using var response = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register",
            new
            {
                firstName = prefix,
                lastName = "Test",
                email,
                userName,
                password,
                confirmPassword = password,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await response.DeserializeAsync<RegisterResult>();

        // /token/issue rejects unconfirmed users with 401; force-confirm in-process so the test
        // can sign in immediately.
        await ConfirmEmailAsync(registered.UserId);

        return new UserCredentials(registered.UserId, email, password);
    }

    private async Task ConfirmEmailAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.ShouldNotBeNull();
        if (!user!.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Seeds <paramref name="count"/> unread notifications for <paramref name="userId"/> directly
    /// via NotificationsDbContext. Returns the new IDs in *creation order* so callers can reason
    /// about ordering. When <paramref name="spaceCreationByMs"/> is set, the helper sleeps between
    /// inserts so CreatedAtUtc is strictly monotonic — needed for ordering/paging assertions.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> SeedNotificationsAsync(
        string userId,
        int count,
        string type = "test.poke",
        int spaceCreationByMs = 0)
    {
        // userId comes from /register as the raw Guid string Identity assigned; the handler reads
        // currentUser.GetUserId().ToString() against the same column, so the canonical Guid format
        // is what we need to match. Normalize through Guid.Parse to be sure.
        var canonical = Guid.Parse(userId).ToString();
        var ids = new List<Guid>(count);

        for (int i = 0; i < count; i++)
        {
            using var scope = _factory.Services.CreateScope();
            var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
            var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
                new MultiTenantContext<AppTenantInfo>(tenant);

            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var notification = Notification.Create(
                userId: canonical,
                type: type,
                title: $"Test notification {i + 1}",
                body: $"Seeded by NotificationsEndpointTests (#{i + 1})",
                link: "/test/seed",
                source: "Tests",
                metadata: new { seq = i });
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            ids.Add(notification.Id);

            if (spaceCreationByMs > 0 && i < count - 1)
            {
                await Task.Delay(spaceCreationByMs);
            }
        }

        return ids;
    }

    private static async Task<IReadOnlyList<NotificationDto>> ReadInboxAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{NotificationsBasePath}/?pageSize=200");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<IReadOnlyList<NotificationDto>>();
    }

    private static async Task<int> GetUnreadCountAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{NotificationsBasePath}/unread-count");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<int>();
    }
}
