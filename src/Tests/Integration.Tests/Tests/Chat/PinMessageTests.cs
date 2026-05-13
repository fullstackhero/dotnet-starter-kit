using System.Net;
using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class PinMessageTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string HubPath = "/api/v1/realtime/hub";
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PinMessageTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── Happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task PinMessage_Should_Set_IsPinned_With_PinnedBy_And_Timestamp()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Pin"));
        var messageId = await SendMessageAsync(client, channelId, "pin me");

        using var pin = await client.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);
        pin.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var message = await FindInChannelAsync(client, channelId, messageId);
        message.IsPinned.ShouldBeTrue();
        message.PinnedByUserId.ShouldNotBeNullOrWhiteSpace();
        message.PinnedAtUtc.ShouldNotBeNull();
        message.PinnedAtUtc.Value.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task UnpinMessage_Should_Clear_IsPinned_And_Stamps()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Unpin"));
        var messageId = await SendMessageAsync(client, channelId, "pin, then unpin");

        await client.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);

        using var unpin = await client.DeleteAsync($"{ChatBasePath}/messages/{messageId}/pin");
        unpin.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await FindInChannelAsync(client, channelId, messageId);
        after.IsPinned.ShouldBeFalse();
        after.PinnedByUserId.ShouldBeNull();
        after.PinnedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task RePin_By_Same_User_Should_Be_Idempotent_NoChange_To_Stamp()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Idem"));
        var messageId = await SendMessageAsync(client, channelId, "idem pin");

        await client.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);
        var first = await FindInChannelAsync(client, channelId, messageId);

        using var second = await client.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await FindInChannelAsync(client, channelId, messageId);
        after.IsPinned.ShouldBeTrue();
        after.PinnedByUserId.ShouldBe(first.PinnedByUserId);
        after.PinnedAtUtc.ShouldBe(first.PinnedAtUtc);
    }

    [Fact]
    public async Task Unpin_When_Not_Pinned_Should_Be_NoOp_NoContent()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("UnpinNoOp"));
        var messageId = await SendMessageAsync(client, channelId, "never pinned");

        using var unpin = await client.DeleteAsync($"{ChatBasePath}/messages/{messageId}/pin");
        unpin.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var after = await FindInChannelAsync(client, channelId, messageId);
        after.IsPinned.ShouldBeFalse();
    }

    // ─── GetPinnedMessages ───────────────────────────────────────────

    [Fact]
    public async Task GetPinnedMessages_Should_Return_Pinned_MostRecentFirst()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("List"));

        var m1 = await SendMessageAsync(client, channelId, "first");
        var m2 = await SendMessageAsync(client, channelId, "second");
        var m3 = await SendMessageAsync(client, channelId, "third");

        await client.PostAsync($"{ChatBasePath}/messages/{m1}/pin", null);
        await Task.Delay(15); // ensure PinnedAtUtc difference
        await client.PostAsync($"{ChatBasePath}/messages/{m3}/pin", null);

        using var listResponse = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/pinned");
        var pinned = await listResponse.DeserializeAsync<IReadOnlyList<MessageDto>>();

        pinned.Count.ShouldBe(2);
        pinned[0].Id.ShouldBe(m3); // most recently pinned first
        pinned[1].Id.ShouldBe(m1);
        pinned.ShouldNotContain(m => m.Id == m2);
    }

    [Fact]
    public async Task GetPinnedMessages_Should_Be_Empty_For_Channel_Without_Pins()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Empty"));
        await SendMessageAsync(client, channelId, "nothing pinned");

        using var listResponse = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/pinned");
        var pinned = await listResponse.DeserializeAsync<IReadOnlyList<MessageDto>>();

        pinned.ShouldBeEmpty();
    }

    // ─── Authorization ───────────────────────────────────────────────

    [Fact]
    public async Task Pin_Should_Return_404_For_Missing_Message()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        using var pin = await client.PostAsync($"{ChatBasePath}/messages/{Guid.NewGuid()}/pin", null);
        pin.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Pin_Should_Return_404_For_Non_Member()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (alice, _, _) = await RegisterUserAsync(adminClient, "alice");

        // Admin creates a channel + sends a message — Alice is NOT a member.
        var channelId = await CreateChannelAsync(adminClient, Unique("Private"));
        var messageId = await SendMessageAsync(adminClient, channelId, "members only");

        using var aliceClient = await _auth.CreateAuthenticatedClientAsync(alice.Email, alice.Password);
        using var pin = await aliceClient.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);

        // RequireMember surfaces 404 to avoid leaking channel existence.
        pin.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPinnedMessages_Should_Return_404_For_Non_Member()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (alice, _, _) = await RegisterUserAsync(adminClient, "alice");

        var channelId = await CreateChannelAsync(adminClient, Unique("PrivateList"));

        using var aliceClient = await _auth.CreateAuthenticatedClientAsync(alice.Email, alice.Password);
        using var listResponse = await aliceClient.GetAsync($"{ChatBasePath}/channels/{channelId}/pinned");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── Realtime ────────────────────────────────────────────────────

    [Fact]
    public async Task PinMessage_Should_Broadcast_ChatMessagePinned_On_Channel_Group()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (bob, bobToken, _) = await RegisterUserAsync(adminClient, "bob");

        var channelId = await CreateChannelAsync(adminClient, Unique("PinHub"));
        await AddMemberAsync(adminClient, channelId, bob.Id);
        var messageId = await SendMessageAsync(adminClient, channelId, "broadcast on pin");

        await using var bobHub = await ConnectHubAsync(bobToken);
        using var inbox = new EventInbox<MessageDto>(bobHub, "ChatMessagePinned");

        using var pin = await adminClient.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);
        pin.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var received = await inbox.WaitForFirstAsync(p => p.Id == messageId, EventTimeout);
        received.ShouldNotBeNull("Expected ChatMessagePinned to fire on Bob's hub connection");
        received!.IsPinned.ShouldBeTrue();
        received.PinnedByUserId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnpinMessage_Should_Broadcast_ChatMessageUnpinned_On_Channel_Group()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (carol, carolToken, _) = await RegisterUserAsync(adminClient, "carol");

        var channelId = await CreateChannelAsync(adminClient, Unique("UnpinHub"));
        await AddMemberAsync(adminClient, channelId, carol.Id);
        var messageId = await SendMessageAsync(adminClient, channelId, "broadcast on unpin");
        await adminClient.PostAsync($"{ChatBasePath}/messages/{messageId}/pin", null);

        await using var carolHub = await ConnectHubAsync(carolToken);
        using var inbox = new EventInbox<MessageDto>(carolHub, "ChatMessageUnpinned");

        using var unpin = await adminClient.DeleteAsync($"{ChatBasePath}/messages/{messageId}/pin");
        unpin.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var received = await inbox.WaitForFirstAsync(p => p.Id == messageId, EventTimeout);
        received.ShouldNotBeNull("Expected ChatMessageUnpinned to fire on Carol's hub connection");
        received!.IsPinned.ShouldBeFalse();
    }

    // ─── helpers (lifted from MentionAndNotificationTests pattern) ───

    private static string Unique(string prefix) => $"pin-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

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

    private static async Task AddMemberAsync(HttpClient adminClient, Guid channelId, string userId)
    {
        using var response = await adminClient.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/members", new
        {
            channelId,
            userIds = new[] { userId },
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task<Guid> SendMessageAsync(HttpClient client, Guid channelId, string body)
    {
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body, parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });
        var dto = await response.DeserializeAsync<MessageDto>();
        return dto.Id;
    }

    private static async Task<MessageDto> FindInChannelAsync(HttpClient client, Guid channelId, Guid messageId)
    {
        using var listResponse = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages?pageSize=200");
        var messages = await listResponse.DeserializeAsync<IReadOnlyList<MessageDto>>();
        return messages.First(m => m.Id == messageId);
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
