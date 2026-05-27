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

/// <summary>
/// Covers the <c>JoinChannel</c> hub method: <see cref="FSH.Framework.Web.Realtime.AppHub.OnConnectedAsync"/>
/// only pre-joins channels that existed (and the user was a member of) at connect time, so a channel that
/// becomes relevant *after* the socket is live needs an on-demand join or its group broadcasts never arrive
/// until the page reloads. This is the root cause of "the recipient doesn't see the message until refresh".
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class JoinChannelTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string HubPath = "/api/v1/realtime/hub";
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public JoinChannelTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task JoinChannel_Should_Deliver_Messages_For_A_Channel_Joined_After_Connect()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (peer, peerToken) = await RegisterAndSignInAsync(adminClient, "joiner");

        // Channel exists, but the peer connects BEFORE being added — so OnConnectedAsync
        // does not pre-join them to channel:{id}. This is the live-conversation scenario.
        var channelId = await CreateChannelAsync(adminClient, Unique("Join"));
        await using var peerHub = await ConnectAsync(peerToken);
        using var peerInbox = new EventInbox<MessageDto>(peerHub, "ChatMessageCreated");

        await AddMemberAsync(adminClient, channelId, peer.Id);

        // The fix: the client joins the group on demand once the channel is open.
        await peerHub.InvokeAsync("JoinChannel", channelId);

        await SendMessageAsync(adminClient, channelId, "live to a late joiner");

        var received = await peerInbox.WaitForFirstAsync(m => m.ChannelId == channelId, EventTimeout);
        received.ShouldNotBeNull("a member that joined the channel after connecting should receive live messages");
        received.Body.ShouldBe("live to a late joiner");
    }

    [Fact]
    public async Task JoinChannel_Should_Not_Add_NonMembers_To_The_Group()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (_, peerToken) = await RegisterAndSignInAsync(adminClient, "outsider");

        // Peer is never added as a member.
        var channelId = await CreateChannelAsync(adminClient, Unique("Guard"));
        await using var peerHub = await ConnectAsync(peerToken);
        using var peerInbox = new EventInbox<MessageDto>(peerHub, "ChatMessageCreated");

        // Membership check must reject this — the connection stays out of the group.
        await peerHub.InvokeAsync("JoinChannel", channelId);

        await SendMessageAsync(adminClient, channelId, "should not leak");

        var received = await peerInbox.WaitForFirstAsync(m => m.ChannelId == channelId, TimeSpan.FromSeconds(2));
        received.ShouldBeNull("a non-member must not be added to the channel group by JoinChannel");
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

    private static async Task SendMessageAsync(HttpClient client, Guid channelId, string body)
    {
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body, parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });
        response.EnsureSuccessStatusCode();
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
            catch (OperationCanceledException) { /* timeout — return default */ }
            return default;
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
