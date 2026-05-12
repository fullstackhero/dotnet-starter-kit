using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class MentionAndNotificationTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string NotificationsBasePath = "/api/v1/notifications";
    private const string HubPath = "/api/v1/realtime/hub";
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public MentionAndNotificationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task SendingMessage_With_AtMention_Should_Persist_Notification_For_Mentioned_User()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();

        // Register a second user the admin can mention. The IMentionResolver looks them up via
        // IUserService.GetListAsync, so all that matters is that the row is active.
        var (alice, _, _) = await RegisterUserAsync(adminClient, "alice");

        // Admin creates a channel, invites Alice, and posts a message that @-mentions her.
        var channelId = await CreateChannelAsync(adminClient, $"chan-{Guid.NewGuid().ToString("N")[..8]}");
        await AddMemberAsync(adminClient, channelId, alice.Id);

        await SendMessageAsync(adminClient, channelId, $"hey @{alice.UserName} take a look");

        // Verify a notification row was written for Alice via her own /notifications list endpoint.
        using var aliceClient = await _auth.CreateAuthenticatedClientAsync(alice.Email, alice.Password);
        var inbox = await ReadInboxAsync(aliceClient);
        var mention = inbox.FirstOrDefault(n => n.Type == "chat.mention");
        mention.ShouldNotBeNull("Expected a chat.mention notification to land in Alice's inbox");
        mention!.Body.ShouldNotBeNullOrEmpty();
        mention.Body!.ShouldContain("take a look");
        mention.Link.ShouldNotBeNull();
        mention.Link!.ShouldStartWith($"/chat/{channelId}");
        mention.ReadAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task SendingMessage_With_AtMention_Should_Push_NotificationCreated_Over_SignalR()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (bob, bobToken, _) = await RegisterUserAsync(adminClient, "bob");

        var channelId = await CreateChannelAsync(adminClient, $"chan-{Guid.NewGuid().ToString("N")[..8]}");
        await AddMemberAsync(adminClient, channelId, bob.Id);

        await using var bobHub = await ConnectAsync(bobToken);
        using var inbox = new EventInbox<NotificationPayload>(bobHub, "NotificationCreated");

        await SendMessageAsync(adminClient, channelId, $"@{bob.UserName} you up?");

        var received = await inbox.WaitForFirstAsync(p => p.Type == "chat.mention", EventTimeout);
        received.ShouldNotBeNull("Expected NotificationCreated to fire on Bob's hub connection");
        received!.Title.ShouldNotBeNullOrWhiteSpace();
        received.Link.ShouldStartWith($"/chat/{channelId}");
    }

    [Fact]
    public async Task SendingMessage_With_Self_Mention_Should_Not_Create_Notification()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();

        // Get admin's own username via /identity/profile so the @-token resolves to admin's userId.
        using var profileResponse = await adminClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var profile = await profileResponse.DeserializeAsync<UserDto>();

        var channelId = await CreateChannelAsync(adminClient, $"chan-{Guid.NewGuid().ToString("N")[..8]}");
        await SendMessageAsync(adminClient, channelId, $"talking to myself @{profile.UserName}");

        // Admin's own inbox should NOT pick up a chat.mention for this message.
        var inbox = await ReadInboxAsync(adminClient);
        // There can be unrelated mentions in the same test factory's shared DB — filter by link.
        inbox.ShouldNotContain(n => n.Type == "chat.mention" && n.Link != null && n.Link.StartsWith($"/chat/{channelId}"));
    }

    // ─── helpers ─────────────────────────────────────────────────────

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

    private static async Task AddMemberAsync(HttpClient adminClient, Guid channelId, string userId)
    {
        using var response = await adminClient.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/members", new
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
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    private static async Task<IReadOnlyList<NotificationDto>> ReadInboxAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{NotificationsBasePath}/?pageSize=100");
        return await response.DeserializeAsync<IReadOnlyList<NotificationDto>>();
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
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        var registered = await response.DeserializeAsync<RegisterResult>();

        // Registered users land with EmailConfirmed=false and `/token/issue` rejects them with
        // 401 until they confirm. Bypass the email link in-process so the test can sign in.
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

    private sealed record NotificationPayload(
        Guid Id,
        string Type,
        string Title,
        string? Body,
        string? Link,
        string Source,
        DateTime CreatedAtUtc);

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
            catch (OperationCanceledException)
            {
                // Fall through to return null on timeout.
            }
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
