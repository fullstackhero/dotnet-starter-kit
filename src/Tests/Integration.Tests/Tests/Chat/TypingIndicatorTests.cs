using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class TypingIndicatorTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string HubPath = "/api/v1/realtime/hub";
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TypingIndicatorTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Typing_Should_Broadcast_ChatTypingStarted_To_Other_Channel_Members()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (peer, peerToken) = await RegisterAndSignInAsync(adminClient, "peer");

        var channelId = await CreateChannelAsync(adminClient, Unique("Typing"));
        await AddMemberAsync(adminClient, channelId, peer.Id);

        var adminToken = await _auth.GetRootAdminTokenAsync();

        await using var adminHub = await ConnectAsync(adminToken.AccessToken);
        await using var peerHub = await ConnectAsync(peerToken);

        using var peerInbox = new EventInbox<TypingPayload>(peerHub, "ChatTypingStarted");

        // Admin types — peer (other member) should receive ChatTypingStarted.
        await adminHub.InvokeAsync("Typing", channelId);

        var received = await peerInbox.WaitForFirstAsync(p => p.ChannelId == channelId, EventTimeout);
        received.ShouldNotBeNull("Expected ChatTypingStarted to arrive at the other member's hub");
    }

    [Fact]
    public async Task Typing_Should_Throttle_To_OneEventPer3Seconds()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (peer, peerToken) = await RegisterAndSignInAsync(adminClient, "peer");

        var channelId = await CreateChannelAsync(adminClient, Unique("Throttle"));
        await AddMemberAsync(adminClient, channelId, peer.Id);

        var adminToken = await _auth.GetRootAdminTokenAsync();

        await using var adminHub = await ConnectAsync(adminToken.AccessToken);
        await using var peerHub = await ConnectAsync(peerToken);

        using var peerInbox = new EventInbox<TypingPayload>(peerHub, "ChatTypingStarted");

        // Rapid-fire 5 invokes — distributed-cache throttle should collapse to exactly one event.
        for (int i = 0; i < 5; i++)
        {
            await adminHub.InvokeAsync("Typing", channelId);
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500));

        var received = await peerInbox.DrainAsync(TimeSpan.FromSeconds(1));
        var matchingHits = received.Count(p => p.ChannelId == channelId);
        matchingHits.ShouldBe(1,
            "typing throttle should collapse rapid calls into a single ChatTypingStarted within the 3s window");
    }

    [Fact]
    public async Task Typing_Should_Not_Broadcast_To_NonMembers()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (peer, peerToken) = await RegisterAndSignInAsync(adminClient, "peer");

        var channelId = await CreateChannelAsync(adminClient, Unique("Solo"));
        // peer is NOT added as a member.

        var adminToken = await _auth.GetRootAdminTokenAsync();
        await using var adminHub = await ConnectAsync(adminToken.AccessToken);
        await using var peerHub = await ConnectAsync(peerToken);

        using var peerInbox = new EventInbox<TypingPayload>(peerHub, "ChatTypingStarted");

        await adminHub.InvokeAsync("Typing", channelId);

        var received = await peerInbox.WaitForFirstAsync(p => p.ChannelId == channelId, TimeSpan.FromSeconds(2));
        received.ShouldBeNull("non-members must not receive typing broadcasts");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string Unique(string prefix) => $"chat-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private async Task<HubConnection> ConnectAsync(string accessToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"http://localhost{HubPath}?access_token={Uri.EscapeDataString(accessToken)}",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.WebSocketFactory = (_, _) => throw new NotSupportedException();
                    options.SkipNegotiation = false;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                    options.Headers["tenant"] = TestConstants.RootTenantId;
                })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    private static async Task<Guid> CreateChannelAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels", new
        {
            name,
            description = (string?)null,
            isPrivate = false,
        });
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task AddMemberAsync(HttpClient client, Guid channelId, string userId)
    {
        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/members", new
        {
            channelId,
            userIds = new[] { userId },
        });
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);
    }

    private async Task<(UserCredentials user, string accessToken)> RegisterAndSignInAsync(HttpClient adminClient, string prefix)
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
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        var registered = await response.DeserializeAsync<RegisterResult>();

        // Bypass /register's email-confirmation gate.
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByIdAsync(registered.UserId);
        user.ShouldNotBeNull();
        if (!user!.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        }

        var token = await _auth.GetTokenAsync(email, password);
        return (new UserCredentials(registered.UserId, userName, email), token.AccessToken);
    }

    private sealed record UserCredentials(string Id, string UserName, string Email);
    private sealed record TypingPayload(Guid ChannelId, string UserId);

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
            using var cts = new CancellationTokenSource(timeout);
            if (TryFindMatch(predicate, out var prearrived))
            {
                return prearrived;
            }
            try
            {
                while (await _signal.WaitAsync(timeout, cts.Token).ConfigureAwait(false))
                {
                    if (TryFindMatch(predicate, out var hit)) return hit;
                }
            }
            catch (OperationCanceledException) { /* timeout — return whatever we drained */ }
            return default;
        }

        /// <summary>
        /// Drain every event that arrived within <paramref name="window"/>, returning a snapshot.
        /// Used to count events under a throttle window — we don't want to stop at the first match.
        /// </summary>
        public async Task<IReadOnlyList<T>> DrainAsync(TimeSpan window)
        {
            var deadline = DateTimeOffset.UtcNow + window;
            var snapshot = new List<T>();
            while (DateTimeOffset.UtcNow < deadline)
            {
                while (_items.TryDequeue(out var item)) snapshot.Add(item);
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                try { await _signal.WaitAsync(remaining).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
            while (_items.TryDequeue(out var late)) snapshot.Add(late);
            return snapshot;
        }

        private bool TryFindMatch(Func<T, bool> predicate, out T match)
        {
            var snapshot = new List<T>();
            while (_items.TryDequeue(out var item)) snapshot.Add(item);
            int idx = snapshot.FindIndex(p => predicate(p));
            if (idx < 0)
            {
                foreach (var item in snapshot) _items.Enqueue(item);
                match = default!;
                return false;
            }
            match = snapshot[idx];
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (i != idx) _items.Enqueue(snapshot[i]);
            }
            return true;
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _signal.Dispose();
        }
    }
}
