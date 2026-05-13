using System.Net;
using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class PresenceTests
{
    private const string HubPath = "/api/v1/realtime/hub";
    private const string PresencePath = "/api/v1/realtime/presence";
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PresenceTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Presence_Endpoint_Should_Report_Offline_For_User_Without_Open_Connection()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (alice, _, _) = await RegisterUserAsync(adminClient, "alice");

        using var response = await adminClient.GetAsync($"{PresencePath}?userIds={alice.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entries = await response.DeserializeAsync<IReadOnlyList<PresenceEntry>>();

        entries.Count.ShouldBe(1);
        entries[0].UserId.ShouldBe(alice.Id);
        entries[0].Online.ShouldBeFalse();
    }

    [Fact]
    public async Task Presence_Endpoint_Should_Report_Online_While_Hub_Is_Connected()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (bob, bobToken, _) = await RegisterUserAsync(adminClient, "bob");

        await using var bobHub = await ConnectHubAsync(bobToken);

        // After OnConnectedAsync runs through, presence flips to online.
        await Eventually(async () =>
        {
            using var response = await adminClient.GetAsync($"{PresencePath}?userIds={bob.Id}");
            var entries = await response.DeserializeAsync<IReadOnlyList<PresenceEntry>>();
            return entries.Count == 1 && entries[0].UserId == bob.Id && entries[0].Online;
        });
    }

    [Fact]
    public async Task Presence_Endpoint_Should_Drop_Back_To_Offline_When_Hub_Disconnects()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (carol, carolToken, _) = await RegisterUserAsync(adminClient, "carol");

        var carolHub = await ConnectHubAsync(carolToken);
        await Eventually(async () => await IsOnline(adminClient, carol.Id));

        await carolHub.StopAsync();
        await carolHub.DisposeAsync();

        await Eventually(async () => !await IsOnline(adminClient, carol.Id));
    }

    [Fact]
    public async Task Presence_Endpoint_Should_Accept_Multiple_UserIds_And_Return_All()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (dave, daveToken, _) = await RegisterUserAsync(adminClient, "dave");
        var (eve, _, _) = await RegisterUserAsync(adminClient, "eve");

        await using var daveHub = await ConnectHubAsync(daveToken);
        // No hub for eve — she stays offline.

        await Eventually(async () =>
        {
            using var response = await adminClient.GetAsync($"{PresencePath}?userIds={dave.Id},{eve.Id}");
            var entries = await response.DeserializeAsync<IReadOnlyList<PresenceEntry>>();
            return entries.Count == 2
                && entries.Any(e => e.UserId == dave.Id && e.Online)
                && entries.Any(e => e.UserId == eve.Id && !e.Online);
        });
    }

    [Fact]
    public async Task PresenceChanged_Should_Fire_When_User_Connects()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (frank, frankToken, _) = await RegisterUserAsync(adminClient, "frank");

        // Admin holds an open hub connection to observe PresenceChanged events.
        var adminToken = (await _auth.GetRootAdminTokenAsync()).AccessToken;
        await using var adminHub = await ConnectHubAsync(adminToken);
        using var inbox = new EventInbox<PresenceChangedPayload>(adminHub, "PresenceChanged");

        // Frank connects.
        await using var frankHub = await ConnectHubAsync(frankToken);

        var received = await inbox.WaitForFirstAsync(p => p.UserId == frank.Id && p.Online, EventTimeout);
        received.ShouldNotBeNull("Expected PresenceChanged { online: true } for Frank");
    }

    [Fact]
    public async Task PresenceChanged_Should_Fire_When_LastConnection_Closes()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (gina, ginaToken, _) = await RegisterUserAsync(adminClient, "gina");

        var adminToken = (await _auth.GetRootAdminTokenAsync()).AccessToken;
        await using var adminHub = await ConnectHubAsync(adminToken);
        using var inbox = new EventInbox<PresenceChangedPayload>(adminHub, "PresenceChanged");

        var ginaHub = await ConnectHubAsync(ginaToken);
        // Drain any "online" event so the next assertion observes the offline-only transition.
        _ = await inbox.WaitForFirstAsync(p => p.UserId == gina.Id && p.Online, EventTimeout);

        await ginaHub.StopAsync();
        await ginaHub.DisposeAsync();

        var received = await inbox.WaitForFirstAsync(p => p.UserId == gina.Id && !p.Online, EventTimeout);
        received.ShouldNotBeNull("Expected PresenceChanged { online: false } when Gina disconnects");
    }

    [Fact]
    public async Task Presence_Endpoint_Should_Require_Authentication()
    {
        using var anonymous = _factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await anonymous.GetAsync($"{PresencePath}?userIds={Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task Eventually(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + EventTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(100);
        }
        throw new TimeoutException("Condition not satisfied within EventTimeout.");
    }

    private static async Task<bool> IsOnline(HttpClient client, string userId)
    {
        using var response = await client.GetAsync($"{PresencePath}?userIds={userId}");
        var entries = await response.DeserializeAsync<IReadOnlyList<PresenceEntry>>();
        return entries.FirstOrDefault(e => e.UserId == userId)?.Online ?? false;
    }

    private async Task<HubConnection> ConnectHubAsync(string accessToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"http://localhost{HubPath}?access_token={Uri.EscapeDataString(accessToken)}",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.WebSocketFactory = (_, _) => throw new NotSupportedException();
                    options.SkipNegotiation = false;
                    options.Transports = HttpTransportType.LongPolling;
                    options.Headers["tenant"] = TestConstants.RootTenantId;
                })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    private async Task<(UserCredentials user, string accessToken, string refreshToken)> RegisterUserAsync(
        HttpClient adminClient, string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        var userName = $"{prefix}{unique}";
        const string password = "Test@1234!";

        using var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
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

        await ConfirmEmailAsync(registered.UserId);

        var token = await _auth.GetTokenAsync(email, password);
        return (new UserCredentials(registered.UserId, userName, email, password), token.AccessToken, token.RefreshToken);
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

    private sealed record UserCredentials(string Id, string UserName, string Email, string Password);

    private sealed record PresenceEntry(string UserId, bool Online);

    private sealed record PresenceChangedPayload(string UserId, bool Online);

    private sealed class EventInbox<T> : IDisposable
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<T> _items = new();
        private readonly System.Threading.SemaphoreSlim _signal = new(0);
        private readonly IDisposable _subscription;

        public EventInbox(HubConnection connection, string eventName)
        {
            _subscription = connection.On<T>(eventName, payload =>
            {
                _items.Enqueue(payload);
                _signal.Release();
            });
        }

        public async Task<T?> WaitForFirstAsync(Func<T, bool> predicate, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                while (_items.TryDequeue(out var item))
                {
                    if (predicate(item)) return item;
                }
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                await _signal.WaitAsync(remaining);
            }
            return default;
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _signal.Dispose();
        }
    }
}
